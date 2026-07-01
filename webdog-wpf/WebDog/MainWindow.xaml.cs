using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using WebDog.Models;
using WebDog.Services;
using WebDog.ViewModels;

namespace WebDog
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            UpdateWindowFrame();
            Loaded += MainWindow_Loaded;
            StateChanged += MainWindow_StateChanged;
        }

        private void MainWindow_StateChanged(object sender, EventArgs e)
        {
            if (MaximizeButton == null) return;
            var icon = MaximizeButton.Content as System.Windows.Shapes.Path;
            if (icon == null) return;
            if (WindowState == WindowState.Maximized)
            {
                icon.Data = Geometry.Parse("M0,0 L8,0 L8,8 L0,8 Z M0,2 L2,2 L2,0");
                MaximizeButton.ToolTip = "Restore";
            }
            else
            {
                icon.Data = Geometry.Parse("M0,0 L10,0 L10,10 L0,10 Z");
                MaximizeButton.ToolTip = "Maximize";
            }
            UpdateWindowFrame();
        }

        private void UpdateWindowFrame()
        {
            if (OuterWindowFrame == null) return;

            if (WindowState == WindowState.Maximized)
            {
                OuterWindowFrame.Margin = new Thickness(0);
                OuterWindowFrame.CornerRadius = new CornerRadius(0);
                OuterWindowFrame.BorderThickness = new Thickness(0);
                OuterWindowFrame.Effect = null;
            }
            else
            {
                OuterWindowFrame.Margin = new Thickness(18);
                OuterWindowFrame.CornerRadius = new CornerRadius(12);
                OuterWindowFrame.BorderThickness = new Thickness(1);
                OuterWindowFrame.Effect = TryFindResource("WindowShadowEffect") as System.Windows.Media.Effects.Effect;
            }
        }

        private void MinimizeWindow_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

        private void MaximizeWindow_Click(object sender, RoutedEventArgs e) =>
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

        private void CloseWindow_Click(object sender, RoutedEventArgs e) => Close();

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.WsMessages.CollectionChanged += (_, __) => ScrollWsToBottom();
                vm.PropertyChanged += Vm_PropertyChanged;
            }
            RenderHighlightedResponse();
        }

        private void Vm_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.DisplayedResponseBody) ||
                e.PropertyName == nameof(MainViewModel.ResponseContentType) ||
                e.PropertyName == nameof(MainViewModel.ResponseView) ||
                e.PropertyName == nameof(MainViewModel.IsDarkTheme))
            {
                try { RenderHighlightedResponse(); } catch { }
                try { RenderPreview(); } catch { }
            }
        }

        private void RenderHighlightedResponse()
        {
            if (ResponseRichTextBox == null) return;
            if (DataContext is not MainViewModel vm) return;

            // Only render highlighted doc for the Pretty view.
            if (vm.ResponseView != "pretty" || string.IsNullOrEmpty(vm.DisplayedResponseBody))
            {
                ResponseRichTextBox.Document = new FlowDocument();
                return;
            }

            var lang = SyntaxHighlighter.Detect(vm.ResponseContentType);
            var doc = SyntaxHighlighter.Build(vm.DisplayedResponseBody, lang);
            ResponseRichTextBox.Document = doc;
        }

        private void ScrollWsToBottom()
        {
            Dispatcher.BeginInvoke(new System.Action(() =>
            {
                if (WsMessagesScroll != null)
                {
                    WsMessagesScroll.ScrollToBottom();
                }
            }));
        }

        private void RenderPreview()
        {
            if (DataContext is not MainViewModel vm) return;
            if (vm.ResponseView != "preview") return;

            var body = vm.Response?.RawBody ?? "";
            var rawBytes = vm.Response?.RawBytes ?? Array.Empty<byte>();
            var contentType = vm.ResponseContentType ?? "";

            if (string.IsNullOrEmpty(body) && rawBytes.Length == 0)
            {
                if (PreviewBrowser != null)
                    PreviewBrowser.NavigateToString(BuildPreviewMessageHtml("Empty response body"));
                return;
            }

            if (contentType.Contains("html"))
            {
                if (PreviewBrowser != null)
                    PreviewBrowser.NavigateToString(body);
            }
            else if (vm.IsImageResponse)
            {
                try
                {
                    using var ms = new MemoryStream(rawBytes);
                    var bitmap = new System.Windows.Media.Imaging.BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                    bitmap.StreamSource = ms;
                    bitmap.EndInit();
                    bitmap.Freeze();
                    if (PreviewImage != null)
                        PreviewImage.Source = bitmap;
                }
                catch
                {
                    if (PreviewImage != null)
                        PreviewImage.Source = null;
                }
            }
            else if (contentType.Contains("json") || contentType.Contains("xml"))
            {
                var escaped = System.Net.WebUtility.HtmlEncode(body);
                var html = BuildPreformattedPreviewHtml(escaped);
                if (PreviewBrowser != null)
                    PreviewBrowser.NavigateToString(html);
            }
            else
            {
                if (PreviewBrowser != null)
                    PreviewBrowser.NavigateToString(BuildPreviewMessageHtml($"Preview not available for {System.Net.WebUtility.HtmlEncode(contentType)}"));
            }
        }

        private static string BuildPreviewMessageHtml(string message)
        {
            var bg = CssColor("AppSurfaceBrush", "#0B0E14");
            var fg = CssColor("TextMutedBrush", "#9BA6B2");
            return $"<html><body style='background:{bg};color:{fg};display:flex;align-items:center;justify-content:center;height:100vh;font-family:sans-serif'><p>{message}</p></body></html>";
        }

        private static string BuildPreformattedPreviewHtml(string escapedBody)
        {
            var bg = CssColor("AppSurfaceBrush", "#0B0E14");
            var fg = CssColor("TextPrimaryBrush", "#F0F6FC");
            return $"<html><head><style>body{{background:{bg};color:{fg};font-family:Consolas,monospace;padding:16px;white-space:pre-wrap;font-size:13px}}</style></head><body>{escapedBody}</body></html>";
        }

        private static string CssColor(string resourceKey, string fallback)
        {
            if (Application.Current?.TryFindResource(resourceKey) is SolidColorBrush brush)
            {
                return ToHex(brush.Color);
            }

            var colorKey = resourceKey.EndsWith("Brush", StringComparison.Ordinal)
                ? resourceKey[..^5]
                : resourceKey;
            if (Application.Current?.TryFindResource(colorKey) is Color color)
            {
                return ToHex(color);
            }

            return fallback;
        }

        private static string ToHex(Color color) => $"#{color.R:X2}{color.G:X2}{color.B:X2}";

        private void HistoryItem_Click(object sender, MouseButtonEventArgs e)
        {
            // Ignore clicks that originate from the delete button so deleting
            // a history item doesn't also trigger selection.
            var src = e.OriginalSource as DependencyObject;
            while (src != null)
            {
                if (src is Button) return;
                src = VisualTreeHelper.GetParent(src);
            }

            if (sender is Border border && border.DataContext is HistoryItem item)
            {
                if (DataContext is MainViewModel vm)
                {
                    vm.SelectHistoryCommand.Execute(item);
                }
            }
        }

        private void FormFilePicker_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is FormParamModel fp)
            {
                var dlg = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "Select File",
                    Filter = "All files (*.*)|*.*",
                };
                if (dlg.ShowDialog() == true)
                {
                    fp.Value = dlg.FileName;
                    fp.FileName = dlg.SafeFileName;
                    fp.FileSize = new System.IO.FileInfo(dlg.FileName).Length;
                }
            }
        }

        private void BinaryFilePicker_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Select Binary File",
                Filter = "All files (*.*)|*.*",
            };
            if (dlg.ShowDialog() == true && DataContext is MainViewModel vm)
            {
                vm.Body = dlg.FileName;
                vm.BodyFileName = dlg.SafeFileName;
                vm.BodyFileSize = new System.IO.FileInfo(dlg.FileName).Length;
            }
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (DataContext is not MainViewModel vm) return;

            // Ctrl+S: Save response
            if (e.Key == Key.S && Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (vm.SaveResponseCommand.CanExecute(null))
                    vm.SaveResponseCommand.Execute(null);
                e.Handled = true;
            }
            // Escape: Cancel request
            else if (e.Key == Key.Escape)
            {
                if (vm.CancelRequestCommand.CanExecute(null))
                    vm.CancelRequestCommand.Execute(null);
                e.Handled = true;
            }
            // Ctrl+L: Focus URL bar (handled implicitly by URL TextBox)
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.SaveEnvVars();
            }
            base.OnClosing(e);
        }
    }
}

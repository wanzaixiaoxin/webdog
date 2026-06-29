using System.Windows;

namespace WebDog.Views
{
    public partial class BulkEditDialog : Window
    {
        public string Result { get; private set; }

        public BulkEditDialog(string title, string initialText)
        {
            Title = title;
            Width = 600;
            Height = 480;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.CanResizeWithGrip;
            Background = System.Windows.Media.Brushes.Transparent;
            AllowsTransparency = true;
            WindowStyle = WindowStyle.None;

            var border = new System.Windows.Controls.Border
            {
                Background = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom("#161B22"),
                BorderBrush = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom("#30363D"),
                BorderThickness = new System.Windows.Thickness(1),
                CornerRadius = new System.Windows.CornerRadius(12),
                Margin = new System.Windows.Thickness(0),
            };

            var grid = new System.Windows.Controls.Grid();
            grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = new GridLength(48) });
            grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition());
            grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = new GridLength(52) });

            // Title bar
            var titleBar = new System.Windows.Controls.Grid { Margin = new Thickness(20, 0, 12, 0) };
            titleBar.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition());
            titleBar.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = GridLength.Auto });

            var titleText = new System.Windows.Controls.TextBlock
            {
                Text = title,
                Foreground = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom("#F0F6FC"),
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center,
            };
            System.Windows.Controls.Grid.SetColumn(titleText, 0);
            titleBar.Children.Add(titleText);

            var closeButton = new System.Windows.Controls.Button
            {
                Content = "\u2715",
                Style = (System.Windows.Style)Application.Current.TryFindResource("GhostButton"),
                Foreground = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom("#484F58"),
                FontSize = 14,
                Padding = new Thickness(8, 4, 8, 4),
                VerticalAlignment = VerticalAlignment.Center,
            };
            closeButton.Click += (_, __) => { Result = null; Close(); };
            System.Windows.Controls.Grid.SetColumn(closeButton, 1);
            titleBar.Children.Add(closeButton);

            System.Windows.Controls.Grid.SetRow(titleBar, 0);
            grid.Children.Add(titleBar);

            // Body
            var textBox = new System.Windows.Controls.TextBox
            {
                Text = initialText,
                AcceptsReturn = true,
                TextWrapping = TextWrapping.NoWrap,
                HorizontalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Auto,
                FontFamily = new System.Windows.Media.FontFamily("Cascadia Code, Consolas"),
                FontSize = 13,
                Padding = new Thickness(16, 12, 16, 12),
                Background = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom("#0D1117"),
                Foreground = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom("#F0F6FC"),
                BorderThickness = new Thickness(0),
                Margin = new Thickness(16, 0, 16, 0),
            };
            System.Windows.Controls.Grid.SetRow(textBox, 1);
            grid.Children.Add(textBox);

            // Footer
            var footer = new System.Windows.Controls.StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(16, 0, 16, 0),
            };

            var cancelBtn = new System.Windows.Controls.Button
            {
                Content = "Cancel",
                Style = (System.Windows.Style)Application.Current.TryFindResource("GhostButton"),
                Foreground = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom("#8B949E"),
                FontSize = 12,
                Padding = new Thickness(16, 8, 16, 8),
                Margin = new Thickness(0, 0, 8, 0),
            };
            cancelBtn.Click += (_, __) => { Result = null; Close(); };

            var okBtn = new System.Windows.Controls.Button
            {
                Content = "Apply",
                Style = (System.Windows.Style)Application.Current.TryFindResource("AccentButton"),
                FontSize = 12,
                Padding = new Thickness(20, 8, 20, 8),
            };
            okBtn.Click += (_, __) =>
            {
                Result = textBox.Text;
                DialogResult = true;
                Close();
            };

            footer.Children.Add(cancelBtn);
            footer.Children.Add(okBtn);

            System.Windows.Controls.Grid.SetRow(footer, 2);
            grid.Children.Add(footer);

            border.Child = grid;
            Content = border;

            // Allow dragging by title bar
            titleBar.MouseLeftButtonDown += (_, __) => DragMove();
        }
    }
}

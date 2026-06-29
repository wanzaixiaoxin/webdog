using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace WebDog.Controls
{
    public class LineNumberTextBox : UserControl
    {
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(nameof(Text), typeof(string), typeof(LineNumberTextBox),
                new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnTextChanged));

        public static readonly DependencyProperty WordWrapProperty =
            DependencyProperty.Register(nameof(WordWrap), typeof(bool), typeof(LineNumberTextBox),
                new PropertyMetadata(true, OnWordWrapChanged));

        private TextBox _textBox;
        private TextBlock _lineNumbers;

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public bool WordWrap
        {
            get => (bool)GetValue(WordWrapProperty);
            set => SetValue(WordWrapProperty, value);
        }

        public LineNumberTextBox()
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition());

            _lineNumbers = new TextBlock
            {
                FontFamily = new FontFamily("Cascadia Code, Consolas"),
                FontSize = 13,
                Foreground = (Brush)new BrushConverter().ConvertFrom("#484F58"),
                TextAlignment = TextAlignment.Right,
                Padding = new Thickness(8, 12, 8, 0),
                Background = (Brush)new BrushConverter().ConvertFrom("#0B0E14"),
                VerticalAlignment = VerticalAlignment.Top,
            };
            Grid.SetColumn(_lineNumbers, 0);
            grid.Children.Add(_lineNumbers);

            _textBox = new TextBox
            {
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                FontFamily = new FontFamily("Cascadia Code, Consolas"),
                FontSize = 13,
                Padding = new Thickness(12),
                Background = (Brush)new BrushConverter().ConvertFrom("#0D1117"),
                Foreground = (Brush)new BrushConverter().ConvertFrom("#F0F6FC"),
                BorderThickness = new Thickness(0),
                VerticalAlignment = VerticalAlignment.Stretch,
                ContextMenu = new ContextMenu
                {
                    Background = (Brush)new BrushConverter().ConvertFrom("#161B22"),
                    BorderBrush = (Brush)new BrushConverter().ConvertFrom("#30363D"),
                    Foreground = (Brush)new BrushConverter().ConvertFrom("#F0F6FC"),
                    Items =
                    {
                        CreateMenuItem("Undo", () => _textBox.Undo()),
                        CreateMenuItem("Redo", () => _textBox.Redo()),
                        new Separator(),
                        CreateMenuItem("Cut", () => _textBox.Cut()),
                        CreateMenuItem("Copy", () => _textBox.Copy()),
                        CreateMenuItem("Paste", () => _textBox.Paste()),
                        new Separator(),
                        CreateMenuItem("Select All", () => _textBox.SelectAll()),
                    }
                },
            };
            _textBox.TextChanged += (s, e) =>
            {
                Text = _textBox.Text;
                UpdateLineNumbers();
            };
            _textBox.LayoutUpdated += (s, e) => UpdateLineNumbers();
            Grid.SetColumn(_textBox, 1);
            grid.Children.Add(_textBox);

            Content = grid;
        }

        private void UpdateLineNumbers()
        {
            var lineCount = _textBox.LineCount;
            var oldLineCount = _lineNumbers.Inlines.Count;
            if (lineCount == oldLineCount) return;

            _lineNumbers.Inlines.Clear();
            for (int i = 1; i <= Math.Max(1, lineCount); i++)
            {
                _lineNumbers.Inlines.Add(new System.Windows.Documents.Run($"{i}\n"));
            }
        }

        private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is LineNumberTextBox tb && tb._textBox != null && tb._textBox.Text != (string)e.NewValue)
            {
                tb._textBox.Text = (string)e.NewValue ?? "";
            }
        }

        private static void OnWordWrapChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is LineNumberTextBox tb && tb._textBox != null)
            {
                var wrap = (bool)e.NewValue;
                tb._textBox.TextWrapping = wrap ? TextWrapping.Wrap : TextWrapping.NoWrap;
                tb._textBox.HorizontalScrollBarVisibility = wrap ? ScrollBarVisibility.Auto : ScrollBarVisibility.Auto;
                tb.UpdateLineNumbers();
            }
        }

        private static MenuItem CreateMenuItem(string header, Action action)
        {
            var item = new MenuItem
            {
                Header = header,
                FontSize = 12,
            };
            item.Click += (s, e) => action();
            return item;
        }
    }
}

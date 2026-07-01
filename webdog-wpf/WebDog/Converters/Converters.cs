using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace WebDog.Converters
{
    internal static class ThemeBrushes
    {
        public static Brush Get(string resourceKey, string fallbackHex)
        {
            if (Application.Current?.TryFindResource(resourceKey) is Brush brush)
            {
                return brush;
            }

            var colorKey = resourceKey.EndsWith("Brush", StringComparison.Ordinal)
                ? resourceKey[..^5]
                : resourceKey;
            if (Application.Current?.TryFindResource(colorKey) is Color color)
            {
                return new SolidColorBrush(color);
            }

            return Make(fallbackHex);
        }

        private static SolidColorBrush Make(string hex)
        {
            var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
            brush.Freeze();
            return brush;
        }
    }

    public class BoolToGridLengthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool visible = value is bool b && b;
            if (!visible) return new GridLength(0);
            var param = parameter?.ToString();
            if (param == "Auto") return GridLength.Auto;
            if (double.TryParse(param, out double width)) return new GridLength(width);
            return GridLength.Auto;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class BoolToVisConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => (value is bool b && b) ? Visibility.Visible : Visibility.Collapsed;
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => value is Visibility v && v == Visibility.Visible;
    }

    public class BoolToVisCollapsedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => (value is bool b && b) ? Visibility.Visible : Visibility.Collapsed;
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => value is Visibility v && v == Visibility.Visible;
    }

    public class ProtocolColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var protocol = value?.ToString();
            var expected = parameter?.ToString();
            return protocol == expected
                ? ThemeBrushes.Get("AccentBrush", "#2DD4BF")
                : ThemeBrushes.Get("TextMutedBrush", "#9BA6B2");
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class MethodColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var m = value?.ToString()?.ToUpperInvariant();
            var color = m switch
            {
                "GET" => "BlueBrush",
                "POST" => "GreenBrush",
                "PUT" => "AmberBrush",
                "DELETE" => "RedBrush",
                "PATCH" => "AccentBrush",
                "HEAD" => "TextMutedBrush",
                "OPTIONS" => "BlueBrush",
                _ => "TextMutedBrush",
            };
            return ThemeBrushes.Get(color, "#9BA6B2");
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class StatusColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return ThemeBrushes.Get("TextMutedBrush", "#9BA6B2");
            var status = System.Convert.ToInt32(value);
            var color = status switch
            {
                >= 200 and < 300 => "GreenBrush",
                >= 300 and < 400 => "AmberBrush",
                >= 400 and < 500 => "RedBrush",
                >= 500 => "RedBrush",
                _ => "TextMutedBrush",
            };
            return ThemeBrushes.Get(color, "#9BA6B2");
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class BodyTypeColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var current = value?.ToString();
            var expected = parameter?.ToString();
            return current == expected
                ? ThemeBrushes.Get("AccentBrush", "#2DD4BF")
                : ThemeBrushes.Get("TextMutedBrush", "#9BA6B2");
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class ViewToggleColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var current = value?.ToString();
            var expected = parameter?.ToString();
            return current == expected
                ? ThemeBrushes.Get("AccentBrush", "#2DD4BF")
                : ThemeBrushes.Get("TextMutedBrush", "#9BA6B2");
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class WsButtonBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => (value is bool b && b)
                ? ThemeBrushes.Get("RedBrush", "#F87171")
                : ThemeBrushes.Get("AccentBrush", "#2DD4BF");
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class WsButtonStyleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool connected = value is bool b && b;
            var key = connected ? "DangerButton" : "AccentButton";
            return Application.Current?.TryFindResource(key);
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class WsButtonTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => (value is bool b && b) ? "Disconnect" : "Connect";
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class WsMessageBgConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var t = value?.ToString();
            var color = t switch
            {
                "sent" => Color.FromArgb(30, 45, 212, 191),
                "received" => Color.FromArgb(30, 56, 189, 248),
                "error" => Color.FromArgb(30, 251, 113, 133),
                _ => Color.FromArgb(20, 148, 163, 184),
            };
            return new SolidColorBrush(color);
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class WsMessageBorderConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var t = value?.ToString();
            var color = t switch
            {
                "sent" => Color.FromArgb(60, 45, 212, 191),
                "received" => Color.FromArgb(60, 56, 189, 248),
                "error" => Color.FromArgb(60, 251, 113, 133),
                _ => Color.FromArgb(40, 148, 163, 184),
            };
            return new SolidColorBrush(color);
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class WsMessageFgConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var t = value?.ToString();
            var color = t switch
            {
                "sent" => "AccentBrush",
                "received" => "BlueBrush",
                "error" => "RedBrush",
                _ => "TextMutedBrush",
            };
            return ThemeBrushes.Get(color, "#9BA6B2");
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class StringNotEmptyToVis : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => !string.IsNullOrWhiteSpace(value?.ToString()) ? Visibility.Visible : Visibility.Collapsed;
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class JsonValidationConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var text = value?.ToString();
            var mode = parameter?.ToString();
            if (string.IsNullOrWhiteSpace(text))
            {
                if (mode == "Vis") return Visibility.Collapsed;
                if (mode == "IsValid") return false;
                return "";
            }
            try
            {
                JsonSerializer.Deserialize<object>(text);
                if (mode == "Vis") return Visibility.Collapsed;
                if (mode == "IsValid") return true;
                return "Valid JSON";
            }
            catch (Exception ex)
            {
                if (mode == "Vis") return Visibility.Visible;
                if (mode == "IsValid") return false;
                return ex.Message;
            }
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class InvertBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is bool b ? !b : value;
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => value is bool b ? !b : value;
    }

    public class FormatSizeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not long size || size == 0) return "0 B";
            string[] units = { "B", "KB", "MB", "GB" };
            int unitIndex = 0;
            double formatted = size;
            while (formatted >= 1024 && unitIndex < units.Length - 1)
            {
                formatted /= 1024;
                unitIndex++;
            }
            return $"{formatted:F1} {units[unitIndex]}";
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class NullToCollapsedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value == null ? Visibility.Collapsed : Visibility.Visible;
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class NullToVisConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value != null ? Visibility.Visible : Visibility.Collapsed;
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class ResponseViewVisConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var current = value?.ToString();
            var expected = parameter?.ToString();
            return current == expected ? Visibility.Visible : Visibility.Collapsed;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class ResponseViewSearchVisConverter : IValueConverter
    {
        // Search box only for text views (pretty/raw), not tree.
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var view = value?.ToString();
            return view == "tree" ? Visibility.Collapsed : Visibility.Visible;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class JsonNodeTypeColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var key = value?.ToString() switch
            {
                "string" => "GreenBrush",
                "number" => "BlueBrush",
                "boolean" => "RedBrush",
                "null" => "TextMutedBrush",
                _ => "TextSecondaryBrush",
            };
            return ThemeBrushes.Get(key, "#9BA6B2");
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    /// <summary>Visible for text-based body types (json, text, raw); Collapsed for form-data/urlencoded/none.</summary>
    public class BodyTypeVisConverter : IValueConverter
    {
        private static readonly HashSet<string> TextTypes = new() { "json", "text", "raw" };
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var t = value?.ToString();
            return TextTypes.Contains(t) ? Visibility.Visible : Visibility.Collapsed;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    /// <summary>Visible for key-value body types (form-data, urlencoded); Collapsed otherwise.</summary>
    public class FormBodyVisConverter : IValueConverter
    {
        private static readonly HashSet<string> FormTypes = new() { "formdata", "urlencoded" };
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var t = value?.ToString();
            return FormTypes.Contains(t) ? Visibility.Visible : Visibility.Collapsed;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class FileTypeVisConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => string.Equals(value?.ToString(), "file", StringComparison.OrdinalIgnoreCase) ? Visibility.Visible : Visibility.Collapsed;
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class TextTypeVisConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => !string.Equals(value?.ToString(), "file", StringComparison.OrdinalIgnoreCase) ? Visibility.Visible : Visibility.Collapsed;
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class BinaryVisConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value?.ToString() == "binary" ? Visibility.Visible : Visibility.Collapsed;
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class EqToVisConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                var v = value?.ToString();
                var p = parameter?.ToString();
                return v == p ? Visibility.Visible : Visibility.Collapsed;
            }
            catch { return Visibility.Visible; }
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class TabColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                var v = value?.ToString();
                var p = parameter?.ToString();
                return v == p
                    ? ThemeBrushes.Get("AccentBrush", "#2DD4BF")
                    : ThemeBrushes.Get("TextMutedBrush", "#9BA6B2");
            }
            catch { return ThemeBrushes.Get("TextMutedBrush", "#9BA6B2"); }
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class WrapTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => (value is bool b && b) ? "Wrap: ON" : "Wrap: OFF";
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}

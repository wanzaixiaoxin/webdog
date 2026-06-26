using System;
using System.Globalization;
using System.Text.Json;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace WebDog.Converters
{
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
            return protocol == expected ? new SolidColorBrush(Color.FromRgb(45, 212, 191)) : new SolidColorBrush(Color.FromRgb(100, 116, 139));
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
                "GET" => Color.FromRgb(56, 189, 248),
                "POST" => Color.FromRgb(52, 211, 153),
                "PUT" => Color.FromRgb(245, 158, 11),
                "DELETE" => Color.FromRgb(251, 113, 133),
                "PATCH" => Color.FromRgb(45, 212, 191),
                "HEAD" => Color.FromRgb(148, 163, 184),
                "OPTIONS" => Color.FromRgb(96, 165, 250),
                _ => Color.FromRgb(148, 163, 184),
            };
            return new SolidColorBrush(color);
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class StatusColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return new SolidColorBrush(Color.FromRgb(100, 116, 139));
            var status = System.Convert.ToInt32(value);
            var color = status switch
            {
                >= 200 and < 300 => Color.FromRgb(52, 211, 153),
                >= 300 and < 400 => Color.FromRgb(245, 158, 11),
                >= 400 and < 500 => Color.FromRgb(251, 113, 133),
                >= 500 => Color.FromRgb(239, 68, 68),
                _ => Color.FromRgb(100, 116, 139),
            };
            return new SolidColorBrush(color);
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
                ? new SolidColorBrush(Color.FromRgb(45, 212, 191))
                : new SolidColorBrush(Color.FromRgb(100, 116, 139));
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
                ? new SolidColorBrush(Color.FromRgb(45, 212, 191))
                : new SolidColorBrush(Color.FromRgb(100, 116, 139));
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class WsButtonBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => (value is bool b && b)
                ? new SolidColorBrush(Color.FromRgb(251, 113, 133))
                : new SolidColorBrush(Color.FromRgb(13, 148, 136));
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
                "sent" => Color.FromRgb(45, 212, 191),
                "received" => Color.FromRgb(56, 189, 248),
                "error" => Color.FromRgb(251, 113, 133),
                _ => Color.FromRgb(148, 163, 184),
            };
            return new SolidColorBrush(color);
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
            if (string.IsNullOrWhiteSpace(text)) return "";
            try
            {
                JsonSerializer.Deserialize<object>(text);
                return "Valid JSON";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}

using System;
using System.Windows;
using System.Windows.Threading;

namespace WebDog.Services
{
    public class ThemeService
    {
        private bool _switching;
        private DateTime _lastSwitch = DateTime.MinValue;
        public bool IsDark { get; private set; } = true;

        public void ApplyTheme(bool isDark)
        {
            // Debounce: prevent rapid clicks from stacking
            var now = DateTime.Now;
            if ((now - _lastSwitch).TotalMilliseconds < 300 || _switching) return;
            _lastSwitch = now;
            _switching = true;

            try
            {
                IsDark = isDark;
                var dicts = Application.Current.Resources.MergedDictionaries;
                dicts.Clear();
                dicts.Add(new ResourceDictionary { Source = new Uri("/Themes/Shadows.xaml", UriKind.Relative) });
                dicts.Add(new ResourceDictionary { Source = new Uri("/Themes/DarkTheme.xaml", UriKind.Relative) });
                if (!isDark)
                    dicts.Add(new ResourceDictionary { Source = new Uri("/Themes/LightTheme.xaml", UriKind.Relative) });

                // Force UI refresh
                var windows = Application.Current.Windows;
                foreach (Window w in windows)
                {
                    var oldBg = w.Background;
                    w.Background = null;
                    w.Background = oldBg;
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Theme switch failed", ex);
            }
            finally
            {
                _switching = false;
            }
        }
    }
}

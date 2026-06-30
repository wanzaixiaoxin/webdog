using System;
using System.Windows;

namespace WebDog.Services
{
    public class ThemeService
    {
        public bool IsDark { get; private set; } = true;

        public void ApplyTheme(bool isDark)
        {
            IsDark = isDark;
            var dicts = Application.Current.Resources.MergedDictionaries;
            dicts.Clear();
            dicts.Add(new ResourceDictionary { Source = new Uri("/Themes/DarkTheme.xaml", UriKind.Relative) });
            if (!isDark)
                dicts.Add(new ResourceDictionary { Source = new Uri("/Themes/LightTheme.xaml", UriKind.Relative) });
        }
    }
}

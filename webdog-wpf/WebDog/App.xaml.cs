using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace WebDog
{
    public partial class App : Application
    {
        public App()
        {
            // UI thread exceptions
            DispatcherUnhandledException += OnUnhandledUI;
            // Non-UI thread exceptions
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledDomain;
            // Task exceptions
            TaskScheduler.UnobservedTaskException += OnUnobservedTask;
        }

        private static void LogCrash(string label, Exception ex)
        {
            var log = Path.Combine(Path.GetTempPath(), "webdog_crash.txt");
            try
            {
                var msg = $"=== {label} ===\nTime: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\nType: {ex.GetType().FullName}\nMessage: {ex.Message}\nStack:\n{ex.StackTrace}";
                if (ex.InnerException != null)
                    msg += $"\n\nInner: {ex.InnerException.GetType().FullName}\n{ex.InnerException.Message}\n{ex.InnerException.StackTrace}";
                File.AppendAllText(log, msg + "\n\n");
            }
            catch { }
        }

        private void OnUnhandledUI(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            LogCrash("UI_THREAD", e.Exception);
            e.Handled = true;
            MessageBox.Show($"UI Error: {e.Exception.Message}\n\nDetails saved to %TEMP%\\webdog_crash.txt",
                "WebDog Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void OnUnhandledDomain(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                LogCrash("APP_DOMAIN", ex);
                MessageBox.Show($"Fatal Error: {ex.Message}\n\nDetails saved to %TEMP%\\webdog_crash.txt",
                    "WebDog Fatal Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnUnobservedTask(object sender, UnobservedTaskExceptionEventArgs e)
        {
            LogCrash("TASK_UNOBSERVED", e.Exception);
            e.SetObserved();
        }
    }
}

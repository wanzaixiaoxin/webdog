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
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Type: {e.Exception.GetType().FullName}");
            sb.AppendLine($"Message: {e.Exception.Message}");
            var inner = e.Exception.InnerException;
            if (inner != null)
            {
                sb.AppendLine($"InnerType: {inner.GetType().FullName}");
                sb.AppendLine($"InnerMsg: {inner.Message}");
                sb.AppendLine($"InnerStack: {inner.StackTrace}");
            }
            sb.AppendLine($"Stack:\n{e.Exception.StackTrace}");

            var log = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "webdog_crash.txt");
            try { File.WriteAllText(log, sb.ToString()); } catch { }

            e.Handled = true;
            var brief = $"Type: {e.Exception.GetType().Name}\nMsg: {e.Exception.Message}";
            if (inner != null) brief += $"\nInner: {inner.GetType().Name}: {inner.Message}";
            MessageBox.Show($"{brief}\n\nCrash log written to:\n{log}",
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

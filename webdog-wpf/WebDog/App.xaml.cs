using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using WebDog.Services;

namespace WebDog
{
    public partial class App : Application
    {
        public App()
        {
            Logger.Info("=== Application starting ===");

            DispatcherUnhandledException += OnUnhandledUI;
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledDomain;
            TaskScheduler.UnobservedTaskException += OnUnobservedTask;

            Startup += (_, _) => Logger.Info("Application started successfully");
            Exit += (_, _) => Logger.Info("=== Application exiting ===");
        }

        private void OnUnhandledUI(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            Logger.Error("UI_THREAD unhandled exception", e.Exception);
            e.Handled = true;
        }

        private void OnUnhandledDomain(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
                Logger.Error($"APP_DOMAIN unhandled (terminating={e.IsTerminating})", ex);
        }

        private void OnUnobservedTask(object sender, UnobservedTaskExceptionEventArgs e)
        {
            Logger.Error("TASK_UNOBSERVED exception", e.Exception);
            e.SetObserved();
        }
    }
}

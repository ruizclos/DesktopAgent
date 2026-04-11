using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace LocalAIAgent
{
    public partial class App : Application
    {
        public App()
        {
            DispatcherUnhandledException += (s, e) =>
            {
                File.AppendAllText("crash.log", $"[{DateTime.Now}] DISPATCHER: {e.Exception}\n\n");
                Console.Error.WriteLine("CRASH: " + e.Exception);
                e.Handled = true;
                MessageBox.Show(e.Exception.Message, "Unhandled Error", MessageBoxButton.OK, MessageBoxImage.Error);
            };

            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                File.AppendAllText("crash.log", $"[{DateTime.Now}] DOMAIN: {e.ExceptionObject}\n\n");
                Console.Error.WriteLine("FATAL: " + e.ExceptionObject);
            };
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            try
            {
                var win = new MainWindow();
                win.Show();
            }
            catch (Exception ex)
            {
                var msg = ex.InnerException?.Message ?? ex.Message;
                File.AppendAllText("crash.log", $"[{DateTime.Now}] STARTUP: {ex}\n\n");
                Console.Error.WriteLine("STARTUP CRASH: " + ex);
                MessageBox.Show(msg, "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown(1);
            }
        }
    }
}

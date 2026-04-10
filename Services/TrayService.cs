using System;
using System.Drawing;
using System.Windows;
using System.Windows.Forms;

namespace LocalAIAgent.Services
{
    public class TrayService : IDisposable
    {
        private readonly NotifyIcon _trayIcon;
        private readonly Window _mainWindow;

        public TrayService(Window mainWindow)
        {
            _mainWindow = mainWindow;

            _trayIcon = new NotifyIcon
            {
                Icon = SystemIcons.Application,
                Visible = true,
                Text = "Local AI Agent"
            };

            var menu = new ContextMenuStrip();
            menu.Items.Add("Open", null, (_, __) => ShowMainWindow());
            menu.Items.Add("Exit", null, (_, __) => ExitApp());

            _trayIcon.ContextMenuStrip = menu;
            _trayIcon.DoubleClick += (_, __) => ShowMainWindow();
        }

        public void MinimizeToTray()
        {
            _mainWindow.Hide();
            _trayIcon.Visible = true;
        }

        private void ShowMainWindow()
        {
            _mainWindow.Show();
            _mainWindow.WindowState = WindowState.Normal;
            _mainWindow.Activate();
        }

        private void ExitApp()
        {
            _trayIcon.Visible = false;
            Application.Current.Shutdown();
        }

        public void Dispose()
        {
            _trayIcon.Dispose();
        }
    }
}

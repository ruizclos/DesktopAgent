using System;
using System.ComponentModel;
using System.Windows;
using LocalAIAgent.ViewModels;
using LocalAIAgent.Services;

namespace LocalAIAgent
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;
        private readonly TrayService _tray;

        public MainWindow()
        {
            InitializeComponent();

            // Build service graph
            var config = new ConfigService();
            var log    = new LoggingService();
            var ai     = new AiService(config);
            var email  = new EmailService(config);
            var calendar   = new CalendarService(config);
            var pdf        = new PdfService(ai);
            var report     = new ReportService(ai, email, calendar, config);
            var appointment = new AppointmentService(ai, calendar);

            _viewModel = new MainViewModel(config, ai, email, pdf, report, appointment, calendar);
            DataContext = _viewModel;

            // Pre-fill masked fields from saved config
            ApiKeyBox.Password        = config.Config.ApiKey;
            EmailPasswordBox.Password = config.Config.EmailPassword;
            GoogleSecretBox.Password  = config.Config.GoogleClientSecret;

            // Initialize tray service
            _tray = new TrayService(this);

            // Honour StartMinimized preference
            if (config.Config.StartMinimized)
            {
                Loaded += (_, _) => Hide();
            }
        }

        // ─── PasswordBox change handlers ──────────────────────────

        private void ApiKeyBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm)
                vm.ApiKey = ApiKeyBox.Password;
        }

        private void EmailPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm)
                vm.EmailPassword = EmailPasswordBox.Password;
        }

        private void GoogleSecretBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm)
                vm.GoogleClientSecret = GoogleSecretBox.Password;
        }

        // ─── Minimize to tray ────────────────────────────────────

        protected override void OnClosing(CancelEventArgs e)
        {
            e.Cancel = true;
            Hide();
        }

        protected override void OnStateChanged(EventArgs e)
        {
            base.OnStateChanged(e);
            if (WindowState == WindowState.Minimized)
                Hide();
        }

        // ─── Cleanup ──────────────────────────────────────────────

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _tray.Dispose();
        }
    }
}

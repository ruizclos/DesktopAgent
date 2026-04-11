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
            var log = new LoggingService();
            var ai = new AiService(config);
            var email = new EmailService(config);
            var calendar = new CalendarService(config);
            var pdf = new PdfService(ai);
            var report = new ReportService(ai, email, calendar, config);
            var appointment = new AppointmentService(ai, calendar);

            _viewModel = new MainViewModel(
                config,
                email,
                pdf,
                report,
                appointment,
                calendar,
                log
            );

            DataContext = _viewModel;

            // Load API key into PasswordBox
            ApiKeyBox.Password = config.Config.ApiKey;

            // Initialize tray service
            _tray = new TrayService(this);
        }

        private void ApiKeyBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.ApiKey = ApiKeyBox.Password;
            }
        }

        // ---------------------------------------------------------
        // MINIMIZE TO TRAY BEHAVIOR
        // ---------------------------------------------------------
        protected override void OnClosing(CancelEventArgs e)
        {
            // Instead of closing, minimize to tray
            e.Cancel = true;
            Hide();
        }

        protected override void OnStateChanged(EventArgs e)
        {
            base.OnStateChanged(e);

            if (WindowState == WindowState.Minimized)
            {
                Hide();
            }
        }

        // ---------------------------------------------------------
        // CLEANUP
        // ---------------------------------------------------------
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _tray.Dispose();
        }
    }
}

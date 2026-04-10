using System.Windows;
using LocalAIAgent.ViewModels;
using LocalAIAgent.Services;

namespace LocalAIAgent
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();

            // Build service graph
            var config = new ConfigService();
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
                calendar
            );

            DataContext = _viewModel;

            // Load API key into PasswordBox
            ApiKeyBox.Password = config.Config.ApiKey;
        }

        private void ApiKeyBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.ApiKey = ApiKeyBox.Password;
            }
        }
    }
}

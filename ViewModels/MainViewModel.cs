using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using LocalAIAgent.Services;
using LocalAIAgent.Views;

namespace LocalAIAgent.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly ConfigService _config;
        private readonly EmailService _emailService;
        private readonly PdfService _pdfService;
        private readonly ReportService _reportService;
        private readonly AppointmentService _appointmentService;
        private readonly CalendarService _calendarService;

        public MainViewModel(ConfigService config,
                             EmailService emailService,
                             PdfService pdfService,
                             ReportService reportService,
                             AppointmentService appointmentService,
                             CalendarService calendarService)
        {
            _config = config;
            _emailService = emailService;
            _pdfService = pdfService;
            _reportService = reportService;
            _appointmentService = appointmentService;
            _calendarService = calendarService;

            SaveSettingsCommand = new RelayCommand(SaveSettings);
            ConnectEmailCommand = new RelayCommand(async () => await ConnectEmailAsync());
            SummarizePdfCommand = new RelayCommand(async () => await SummarizePdfAsync());
            GenerateReplyCommand = new RelayCommand(async () => await GenerateReplyAsync());
            GenerateDailyReportCommand = new RelayCommand(async () => await GenerateDailyReportAsync());
            ScheduleAppointmentCommand = new RelayCommand(async () => await ScheduleAppointmentAsync());
            ViewAvailabilityCommand = new RelayCommand(async () => await ViewAvailabilityAsync());
        }

        // -------------------------
        // Bindable Properties
        // -------------------------

        public string Email
        {
            get => _config.Config.Email;
            set { _config.Config.Email = value; OnPropertyChanged(); }
        }

        public string ImapServer
        {
            get => _config.Config.ImapServer;
            set { _config.Config.ImapServer = value; OnPropertyChanged(); }
        }

        public int ImapPort
        {
            get => _config.Config.ImapPort;
            set { _config.Config.ImapPort = value; OnPropertyChanged(); }
        }

        public bool UseSsl
        {
            get => _config.Config.UseSsl;
            set { _config.Config.UseSsl = value; OnPropertyChanged(); }
        }

        public string ApiKey
        {
            get => _config.Config.ApiKey;
            set { _config.Config.ApiKey = value; OnPropertyChanged(); }
        }

        public string ReportsFolder
        {
            get => _config.Config.ReportsFolder;
            set { _config.Config.ReportsFolder = value; OnPropertyChanged(); }
        }

        public string GoogleClientId
        {
            get => _config.Config.GoogleClientId;
            set { _config.Config.GoogleClientId = value; OnPropertyChanged(); }
        }

        public string GoogleClientSecret
        {
            get => _config.Config.GoogleClientSecret;
            set { _config.Config.GoogleClientSecret = value; OnPropertyChanged(); }
        }

        public string MicrosoftClientId
        {
            get => _config.Config.MicrosoftClientId;
            set { _config.Config.MicrosoftClientId = value; OnPropertyChanged(); }
        }

        public string MicrosoftTenantId
        {
            get => _config.Config.MicrosoftTenantId;
            set { _config.Config.MicrosoftTenantId = value; OnPropertyChanged(); }
        }

        public bool UseGoogleCalendar
        {
            get => _config.Config.UseGoogleCalendar;
            set
            {
                _config.Config.UseGoogleCalendar = value;
                if (value) _config.Config.UseOutlookCalendar = false;
                OnPropertyChanged();
                OnPropertyChanged(nameof(UseOutlookCalendar));
            }
        }

        public bool UseOutlookCalendar
        {
            get => _config.Config.UseOutlookCalendar;
            set
            {
                _config.Config.UseOutlookCalendar = value;
                if (value) _config.Config.UseGoogleCalendar = false;
                OnPropertyChanged();
                OnPropertyChanged(nameof(UseGoogleCalendar));
            }
        }

        private string _statusText = "Idle";
        public string StatusText
        {
            get => _statusText;
            set { _statusText = value; OnPropertyChanged(); }
        }

        private string _logText = "";
        public string LogText
        {
            get => _logText;
            set { _logText = value; OnPropertyChanged(); }
        }

        // -------------------------
        // Commands
        // -------------------------

        public RelayCommand SaveSettingsCommand { get; }
        public RelayCommand ConnectEmailCommand { get; }
        public RelayCommand SummarizePdfCommand { get; }
        public RelayCommand GenerateReplyCommand { get; }
        public RelayCommand GenerateDailyReportCommand { get; }
        public RelayCommand ScheduleAppointmentCommand { get; }
        public RelayCommand ViewAvailabilityCommand { get; }

        // -------------------------
        // Methods
        // -------------------------

        private void SaveSettings()
        {
            _config.Save();
            AppendLog("Settings saved.");
        }

        private async Task ConnectEmailAsync()
        {
            StatusText = "Connecting...";
            AppendLog("Connecting to email...");

            try
            {
                var emails = await _emailService.GetRecentEmailsAsync(5);
                AppendLog($"Fetched {emails.Count} recent emails.");
                StatusText = "Connected";
            }
            catch (Exception ex)
            {
                AppendLog($"Error: {ex.Message}");
                StatusText = "Error";
            }
        }

        private async Task SummarizePdfAsync()
        {
            var dlg = new OpenFileDialog { Filter = "PDF files|*.pdf" };
            if (dlg.ShowDialog() == true)
            {
                StatusText = "Summarizing PDF...";
                AppendLog($"Summarizing {dlg.FileName}...");

                var summary = await _pdfService.SummarizePdfAsync(dlg.FileName);
                AppendLog("Summary:\n" + summary);

                StatusText = "Idle";
            }
        }

        private async Task GenerateReplyAsync()
        {
            var note = Microsoft.VisualBasic.Interaction.InputBox(
                "Enter a short note for the reply:",
                "Generate Reply",
                "Thank you for reaching out...");

            if (string.IsNullOrWhiteSpace(note))
                return;

            StatusText = "Generating reply...";
            AppendLog("Generating reply...");

            var reply = await _reportService.GenerateDailyReportAsync(); // placeholder
            AppendLog("Reply (placeholder):\n" + note);

            StatusText = "Idle";
        }

        private async Task GenerateDailyReportAsync()
        {
            StatusText = "Generating report...";
            AppendLog("Generating daily report...");

            var path = await _reportService.GenerateDailyReportAsync();
            AppendLog($"Report saved to: {path}");

            StatusText = "Idle";
        }

        private async Task ScheduleAppointmentAsync()
        {
            var dialog = new AppointmentDialog();
            if (dialog.ShowDialog() == true)
            {
                var vm = dialog.ViewModel;
                var start = vm.Date.Date + vm.Time;
                var end = start.AddMinutes(vm.DurationMinutes);

                StatusText = "Scheduling appointment...";
                AppendLog($"Scheduling appointment with {vm.ClientName}...");

                var email = await _appointmentService.ScheduleAppointmentAsync(
                    vm.ClientName, start, end, vm.Purpose);

                AppendLog("Generated Appointment Email:\n" + email);

                StatusText = "Idle";
            }
        }

        private async Task ViewAvailabilityAsync()
        {
            StatusText = "Checking availability...";
            AppendLog("Checking availability for next 7 days...");

            var start = DateTime.Now;
            var end = DateTime.Now.AddDays(7);

            var description = await _appointmentService.DescribeAvailabilityAsync(start, end);
            AppendLog("Availability:\n" + description);

            StatusText = "Idle";
        }

        private void AppendLog(string message)
        {
            LogText += message + "\n";
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

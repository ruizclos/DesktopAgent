using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;
using LocalAIAgent.Services;
using LocalAIAgent.Views;

namespace LocalAIAgent.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly ConfigService _config;
        private readonly AiService _aiService;
        private readonly EmailService _emailService;
        private readonly PdfService _pdfService;
        private readonly ReportService _reportService;
        private readonly AppointmentService _appointmentService;
        private readonly CalendarService _calendarService;

        private DispatcherTimer? _emailTimer;
        private DispatcherTimer? _reportTimer;
        private DateTime _lastReportTriggered = DateTime.MinValue;

        public MainViewModel(ConfigService config,
                             AiService aiService,
                             EmailService emailService,
                             PdfService pdfService,
                             ReportService reportService,
                             AppointmentService appointmentService,
                             CalendarService calendarService)
        {
            _config = config;
            _aiService = aiService;
            _emailService = emailService;
            _pdfService = pdfService;
            _reportService = reportService;
            _appointmentService = appointmentService;
            _calendarService = calendarService;

            SaveSettingsCommand       = new RelayCommand(SaveSettings);
            ConnectEmailCommand       = new RelayCommand(async () => await ConnectEmailAsync());
            SummarizePdfCommand       = new RelayCommand(async () => await SummarizePdfAsync());
            GenerateReplyCommand      = new RelayCommand(async () => await GenerateReplyAsync());
            GenerateDailyReportCommand = new RelayCommand(async () => await GenerateDailyReportAsync());
            ScheduleAppointmentCommand = new RelayCommand(async () => await ScheduleAppointmentAsync());
            ViewAvailabilityCommand   = new RelayCommand(async () => await ViewAvailabilityAsync());
            AskAiCommand              = new RelayCommand(async () => await AskAiAsync(), () => !string.IsNullOrWhiteSpace(AiQuery));
            ClearLogCommand           = new RelayCommand(() => LogText = "");
            CopyLogCommand            = new RelayCommand(() => Clipboard.SetText(LogText));
            BrowseReportsFolderCommand = new RelayCommand(BrowseReportsFolder);
            RevokeGmailOAuthCommand    = new RelayCommand(async () => await RevokeGmailOAuthAsync());

            StartTimers();

            if (_config.Config.AutoCheckEmailOnStart)
            {
                Application.Current.Dispatcher.BeginInvoke(async () =>
                {
                    await Task.Delay(800);
                    await ConnectEmailAsync();
                });
            }
        }

        // ── Busy / Status ─────────────────────────────────────────

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(); }
        }

        private string _statusText = "Idle";
        public string StatusText
        {
            get => _statusText;
            set
            {
                _statusText = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(StatusBrush));
            }
        }

        public Brush StatusBrush
        {
            get
            {
                if (StatusText.Contains("Error", StringComparison.OrdinalIgnoreCase))
                    return Brushes.OrangeRed;
                if (StatusText == "Idle")
                    return new SolidColorBrush(Color.FromRgb(0x9E, 0x9E, 0x9E));
                if (StatusText.Contains("Connected", StringComparison.OrdinalIgnoreCase))
                    return new SolidColorBrush(Color.FromRgb(0x43, 0xA0, 0x47));
                return new SolidColorBrush(Color.FromRgb(0xFF, 0x98, 0x00));
            }
        }

        private string _logText = "";
        public string LogText
        {
            get => _logText;
            set { _logText = value; OnPropertyChanged(); }
        }

        // ── AI Chat ───────────────────────────────────────────────

        private string _aiQuery = "";
        public string AiQuery
        {
            get => _aiQuery;
            set
            {
                _aiQuery = value;
                OnPropertyChanged();
                AskAiCommand.RaiseCanExecuteChanged();
            }
        }

        // ── AI Model picker ───────────────────────────────────────

        public List<string> AiModelOptions { get; } = new()
        {
            "gpt-4o",
            "gpt-4o-mini",
            "gpt-4-turbo",
            "gpt-3.5-turbo",
            "o1-mini",
            "o3-mini",
            "custom"
        };

        // ── Email / IMAP ──────────────────────────────────────────

        public string Email
        {
            get => _config.Config.Email;
            set { _config.Config.Email = value; OnPropertyChanged(); }
        }

        public string EmailPassword
        {
            get => _config.Config.EmailPassword;
            set { _config.Config.EmailPassword = value; OnPropertyChanged(); }
        }

        // ── Gmail OAuth2 ──────────────────────────────────────────

        public bool UseGmailOAuth
        {
            get => _config.Config.UseGmailOAuth;
            set { _config.Config.UseGmailOAuth = value; OnPropertyChanged(); }
        }

        public string GmailOAuthClientId
        {
            get => _config.Config.GmailOAuthClientId;
            set { _config.Config.GmailOAuthClientId = value; OnPropertyChanged(); }
        }

        public string GmailOAuthClientSecret
        {
            get => _config.Config.GmailOAuthClientSecret;
            set { _config.Config.GmailOAuthClientSecret = value; OnPropertyChanged(); }
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

        // ── Email / SMTP ──────────────────────────────────────────

        public string SmtpServer
        {
            get => _config.Config.SmtpServer;
            set { _config.Config.SmtpServer = value; OnPropertyChanged(); }
        }

        public int SmtpPort
        {
            get => _config.Config.SmtpPort;
            set { _config.Config.SmtpPort = value; OnPropertyChanged(); }
        }

        public bool UseSmtpSsl
        {
            get => _config.Config.UseSmtpSsl;
            set { _config.Config.UseSmtpSsl = value; OnPropertyChanged(); }
        }

        // ── Email Behavior ────────────────────────────────────────

        public int MaxEmailCount
        {
            get => _config.Config.MaxEmailCount;
            set { _config.Config.MaxEmailCount = value; OnPropertyChanged(); }
        }

        public bool AutoCheckEmailOnStart
        {
            get => _config.Config.AutoCheckEmailOnStart;
            set { _config.Config.AutoCheckEmailOnStart = value; OnPropertyChanged(); }
        }

        public int EmailRefreshIntervalMinutes
        {
            get => _config.Config.EmailRefreshIntervalMinutes;
            set { _config.Config.EmailRefreshIntervalMinutes = value; OnPropertyChanged(); }
        }

        public string EmailSignature
        {
            get => _config.Config.EmailSignature;
            set { _config.Config.EmailSignature = value; OnPropertyChanged(); }
        }

        // ── AI Settings ───────────────────────────────────────────

        public string ApiKey
        {
            get => _config.Config.ApiKey;
            set { _config.Config.ApiKey = value; OnPropertyChanged(); }
        }

        public string AiModel
        {
            get => _config.Config.AiModel;
            set { _config.Config.AiModel = value; OnPropertyChanged(); }
        }

        public string AiEndpoint
        {
            get => _config.Config.AiEndpoint;
            set { _config.Config.AiEndpoint = value; OnPropertyChanged(); }
        }

        public string AiSystemPrompt
        {
            get => _config.Config.AiSystemPrompt;
            set { _config.Config.AiSystemPrompt = value; OnPropertyChanged(); }
        }

        public int AiMaxTokens
        {
            get => _config.Config.AiMaxTokens;
            set { _config.Config.AiMaxTokens = value; OnPropertyChanged(); }
        }

        // ── Calendar Settings ─────────────────────────────────────

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

        public int CalendarLookAheadDays
        {
            get => _config.Config.CalendarLookAheadDays;
            set { _config.Config.CalendarLookAheadDays = value; OnPropertyChanged(); }
        }

        // ── App Behavior ──────────────────────────────────────────

        public string ReportsFolder
        {
            get => _config.Config.ReportsFolder;
            set { _config.Config.ReportsFolder = value; OnPropertyChanged(); }
        }

        public bool ReportScheduleEnabled
        {
            get => _config.Config.ReportScheduleEnabled;
            set { _config.Config.ReportScheduleEnabled = value; OnPropertyChanged(); StartTimers(); }
        }

        public string ReportScheduleTime
        {
            get => _config.Config.ReportScheduleTime;
            set { _config.Config.ReportScheduleTime = value; OnPropertyChanged(); }
        }

        public bool StartMinimized
        {
            get => _config.Config.StartMinimized;
            set { _config.Config.StartMinimized = value; OnPropertyChanged(); }
        }

        public bool EnableNotifications
        {
            get => _config.Config.EnableNotifications;
            set { _config.Config.EnableNotifications = value; OnPropertyChanged(); }
        }

        public int LogRetentionDays
        {
            get => _config.Config.LogRetentionDays;
            set { _config.Config.LogRetentionDays = value; OnPropertyChanged(); }
        }

        // ── Commands ──────────────────────────────────────────────

        public RelayCommand SaveSettingsCommand { get; }
        public RelayCommand ConnectEmailCommand { get; }
        public RelayCommand SummarizePdfCommand { get; }
        public RelayCommand GenerateReplyCommand { get; }
        public RelayCommand GenerateDailyReportCommand { get; }
        public RelayCommand ScheduleAppointmentCommand { get; }
        public RelayCommand ViewAvailabilityCommand { get; }
        public RelayCommand AskAiCommand { get; }
        public RelayCommand ClearLogCommand { get; }
        public RelayCommand CopyLogCommand { get; }
        public RelayCommand BrowseReportsFolderCommand { get; }
        public RelayCommand RevokeGmailOAuthCommand { get; }

        // ── Command Implementations ───────────────────────────────

        private void SaveSettings()
        {
            _config.Save();
            StartTimers();
            AppendLog($"[{DateTime.Now:HH:mm}] Settings saved.");
            StatusText = "Idle";
        }

        private void BrowseReportsFolder()
        {
            var dlg = new OpenFolderDialog { Title = "Select Reports Folder" };
            if (dlg.ShowDialog() == true)
                ReportsFolder = dlg.FolderName;
        }

        private async Task RevokeGmailOAuthAsync()
        {
            try
            {
                await _emailService.RevokeOAuthTokenAsync();
                AppendLog($"[{DateTime.Now:HH:mm}] Gmail OAuth token cleared. Next connection will open Google sign-in.");
            }
            catch (Exception ex)
            {
                AppendLog($"[{DateTime.Now:HH:mm}] Revoke error: {ex.Message}");
            }
        }

        private async Task ConnectEmailAsync()
        {
            IsBusy = true;
            StatusText = "Connecting to email...";
            AppendLog($"[{DateTime.Now:HH:mm}] Connecting to {_config.Config.ImapServer}...");

            try
            {
                var emails = await _emailService.GetRecentEmailsAsync(_config.Config.MaxEmailCount);
                AppendLog($"[{DateTime.Now:HH:mm}] Fetched {emails.Count} emails.");

                foreach (var msg in emails)
                    AppendLog($"  • [{msg.Date:MMM d}] {msg.From.ToString().Split('<')[0].Trim()} — {msg.Subject}");

                StatusText = "Connected";
            }
            catch (Exception ex)
            {
                AppendLog($"[{DateTime.Now:HH:mm}] Email error: {ex.Message}");
                StatusText = "Error";
            }
            finally { IsBusy = false; }
        }

        private async Task SummarizePdfAsync()
        {
            var dlg = new OpenFileDialog { Filter = "PDF files|*.pdf" };
            if (dlg.ShowDialog() != true) return;

            IsBusy = true;
            StatusText = "Summarizing PDF...";
            AppendLog($"[{DateTime.Now:HH:mm}] Summarizing: {System.IO.Path.GetFileName(dlg.FileName)}");

            try
            {
                var summary = await _pdfService.SummarizePdfAsync(dlg.FileName);
                AppendLog("── PDF Summary ──────────────────────────────");
                AppendLog(summary);
                AppendLog("─────────────────────────────────────────────");
                StatusText = "Idle";
            }
            catch (Exception ex)
            {
                AppendLog($"[{DateTime.Now:HH:mm}] PDF error: {ex.Message}");
                StatusText = "Error";
            }
            finally { IsBusy = false; }
        }

        private async Task GenerateReplyAsync()
        {
            var note = Microsoft.VisualBasic.Interaction.InputBox(
                "Enter a short note or context for the reply:",
                "Generate Reply",
                "Thank you for reaching out...");

            if (string.IsNullOrWhiteSpace(note)) return;

            IsBusy = true;
            StatusText = "Generating reply...";
            AppendLog($"[{DateTime.Now:HH:mm}] Generating email reply...");

            try
            {
                var reply = await _aiService.GenerateEmailReplyAsync("(selected email)", note);
                AppendLog("── Generated Reply ───────────────────────────");
                AppendLog(reply);
                AppendLog("─────────────────────────────────────────────");
                StatusText = "Idle";
            }
            catch (Exception ex)
            {
                AppendLog($"[{DateTime.Now:HH:mm}] AI error: {ex.Message}");
                StatusText = "Error";
            }
            finally { IsBusy = false; }
        }

        private async Task GenerateDailyReportAsync()
        {
            IsBusy = true;
            StatusText = "Generating daily report...";
            AppendLog($"[{DateTime.Now:HH:mm}] Generating daily report...");

            try
            {
                var path = await _reportService.GenerateDailyReportAsync();
                AppendLog($"[{DateTime.Now:HH:mm}] Report saved: {path}");
                StatusText = "Idle";
            }
            catch (Exception ex)
            {
                AppendLog($"[{DateTime.Now:HH:mm}] Report error: {ex.Message}");
                StatusText = "Error";
            }
            finally { IsBusy = false; }
        }

        private async Task ScheduleAppointmentAsync()
        {
            var dialog = new AppointmentDialog();
            if (dialog.ShowDialog() != true) return;

            var vm = dialog.ViewModel;
            var start = vm.Date.Date + vm.Time;
            var end = start.AddMinutes(vm.DurationMinutes);

            IsBusy = true;
            StatusText = "Scheduling appointment...";
            AppendLog($"[{DateTime.Now:HH:mm}] Scheduling appointment with {vm.ClientName}...");

            try
            {
                var email = await _appointmentService.ScheduleAppointmentAsync(
                    vm.ClientName, start, end, vm.Purpose);

                AppendLog("── Appointment Email ─────────────────────────");
                AppendLog(email);
                AppendLog("─────────────────────────────────────────────");
                StatusText = "Idle";
            }
            catch (Exception ex)
            {
                AppendLog($"[{DateTime.Now:HH:mm}] Appointment error: {ex.Message}");
                StatusText = "Error";
            }
            finally { IsBusy = false; }
        }

        private async Task ViewAvailabilityAsync()
        {
            IsBusy = true;
            StatusText = "Checking availability...";
            var days = _config.Config.CalendarLookAheadDays;
            AppendLog($"[{DateTime.Now:HH:mm}] Checking availability for next {days} days...");

            try
            {
                var start = DateTime.Now;
                var end = DateTime.Now.AddDays(days);
                var description = await _appointmentService.DescribeAvailabilityAsync(start, end);

                AppendLog("── Availability ──────────────────────────────");
                AppendLog(description);
                AppendLog("─────────────────────────────────────────────");
                StatusText = "Idle";
            }
            catch (Exception ex)
            {
                AppendLog($"[{DateTime.Now:HH:mm}] Calendar error: {ex.Message}");
                StatusText = "Error";
            }
            finally { IsBusy = false; }
        }

        private async Task AskAiAsync()
        {
            if (string.IsNullOrWhiteSpace(AiQuery)) return;

            var query = AiQuery;
            AiQuery = "";

            IsBusy = true;
            StatusText = "Asking AI...";
            AppendLog($"[{DateTime.Now:HH:mm}] You: {query}");

            try
            {
                var answer = await _aiService.SummarizeTextAsync(query);
                AppendLog($"[{DateTime.Now:HH:mm}] AI: {answer}");
                StatusText = "Idle";
            }
            catch (Exception ex)
            {
                AppendLog($"[{DateTime.Now:HH:mm}] AI error: {ex.Message}");
                StatusText = "Error";
            }
            finally { IsBusy = false; }
        }

        // ── Timers ────────────────────────────────────────────────

        private void StartTimers()
        {
            // Email refresh timer
            _emailTimer?.Stop();
            if (_config.Config.EmailRefreshIntervalMinutes > 0)
            {
                _emailTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMinutes(_config.Config.EmailRefreshIntervalMinutes)
                };
                _emailTimer.Tick += async (s, e) => await ConnectEmailAsync();
                _emailTimer.Start();
            }

            // Scheduled daily report timer
            _reportTimer?.Stop();
            if (_config.Config.ReportScheduleEnabled)
            {
                _reportTimer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(1) };
                _reportTimer.Tick += async (s, e) => await CheckReportScheduleAsync();
                _reportTimer.Start();
            }
        }

        private async Task CheckReportScheduleAsync()
        {
            if (!_config.Config.ReportScheduleEnabled) return;
            var now = DateTime.Now;
            if (TimeSpan.TryParse(_config.Config.ReportScheduleTime, out var scheduled) &&
                now.Hour == scheduled.Hours && now.Minute == scheduled.Minutes &&
                (now - _lastReportTriggered).TotalMinutes > 2)
            {
                _lastReportTriggered = now;
                await GenerateDailyReportAsync();
            }
        }

        // ── Helpers ───────────────────────────────────────────────

        private void AppendLog(string message)
        {
            LogText += message + "\n";
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

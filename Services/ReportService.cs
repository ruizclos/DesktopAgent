using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace LocalAIAgent.Services
{
    public class ReportService
    {
        private readonly AiService _ai;
        private readonly EmailService _emailService;
        private readonly CalendarService _calendarService;
        private readonly ConfigService _config;

        public ReportService(
            AiService ai,
            EmailService emailService,
            CalendarService calendarService,
            ConfigService config)
        {
            _ai = ai;
            _emailService = emailService;
            _calendarService = calendarService;
            _config = config;
        }

        public async Task<string> GenerateDailyReportAsync()
        {
            // -------------------------
            // 1. Fetch recent emails
            // -------------------------
            var emails = await _emailService.GetRecentEmailsAsync(5);

            var sbEmails = new StringBuilder();
            foreach (var email in emails)
            {
                sbEmails.AppendLine($"From: {email.From}");
                sbEmails.AppendLine($"Subject: {email.Subject}");
                sbEmails.AppendLine($"Date: {email.Date}");
                sbEmails.AppendLine();
            }

            var emailSummary = await _ai.SummarizeTextAsync(sbEmails.ToString());

            // -------------------------
            // 2. Fetch calendar events
            // -------------------------
            var calendarSummary = await _calendarService.GetUpcomingEventsSummaryAsync();

            // -------------------------
            // 3. Generate AI report
            // -------------------------
            var report = await _ai.GenerateDailyReportAsync(emailSummary, calendarSummary);

            // -------------------------
            // 4. Save to file
            // -------------------------
            var folder = _config.Config.ReportsFolder;
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            var filePath = Path.Combine(folder,
                $"DailyReport_{DateTime.Now:yyyyMMdd_HHmm}.txt");

            File.WriteAllText(filePath, report);

            return filePath;
        }
    }
}

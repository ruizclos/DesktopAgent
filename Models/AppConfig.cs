namespace LocalAIAgent.Models
{
    public class AppConfig
    {
        // ── Email / IMAP ──────────────────────────────────────────
        public string Email { get; set; } = "";
        public string EmailPassword { get; set; } = "";
        public string ImapServer { get; set; } = "";
        public int ImapPort { get; set; } = 993;
        public bool UseSsl { get; set; } = true;

        // ── Email / SMTP ──────────────────────────────────────────
        public string SmtpServer { get; set; } = "";
        public int SmtpPort { get; set; } = 587;
        public bool UseSmtpSsl { get; set; } = true;

        // ── Email Behavior ────────────────────────────────────────
        public int MaxEmailCount { get; set; } = 10;
        public bool AutoCheckEmailOnStart { get; set; } = false;
        public int EmailRefreshIntervalMinutes { get; set; } = 0;
        public string EmailSignature { get; set; } = "";

        // ── AI Settings ───────────────────────────────────────────
        public string ApiKey { get; set; } = "";
        public string AiModel { get; set; } = "gpt-4o-mini";
        public string AiEndpoint { get; set; } = "https://api.openai.com/v1/chat/completions";
        public string AiSystemPrompt { get; set; } = "You are a helpful assistant.";
        public int AiMaxTokens { get; set; } = 2000;

        // ── Reports ───────────────────────────────────────────────
        public string ReportsFolder { get; set; } = "Reports";
        public bool ReportScheduleEnabled { get; set; } = false;
        public string ReportScheduleTime { get; set; } = "08:00";

        // ── Google Calendar ───────────────────────────────────────
        public string GoogleClientId { get; set; } = "";
        public string GoogleClientSecret { get; set; } = "";

        // ── Microsoft Outlook Calendar ────────────────────────────
        public string MicrosoftClientId { get; set; } = "";
        public string MicrosoftTenantId { get; set; } = "";

        // ── Calendar Provider ─────────────────────────────────────
        public bool UseGoogleCalendar { get; set; } = false;
        public bool UseOutlookCalendar { get; set; } = false;
        public int CalendarLookAheadDays { get; set; } = 7;

        // ── App Behavior ──────────────────────────────────────────
        public bool StartMinimized { get; set; } = false;
        public bool EnableNotifications { get; set; } = true;
        public int LogRetentionDays { get; set; } = 7;
    }
}

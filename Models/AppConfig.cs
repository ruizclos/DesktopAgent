namespace LocalAIAgent.Models
{
    public class AppConfig
    {
        // Email Settings
        public string Email { get; set; } = "";
        public string ImapServer { get; set; } = "";
        public int ImapPort { get; set; } = 993;
        public bool UseSsl { get; set; } = true;

        // AI Settings
        public string ApiKey { get; set; } = "";

        // Reports
        public string ReportsFolder { get; set; } = "Reports";

        // Google Calendar
        public string GoogleClientId { get; set; } = "";
        public string GoogleClientSecret { get; set; } = "";

        // Microsoft Outlook Calendar
        public string MicrosoftClientId { get; set; } = "";
        public string MicrosoftTenantId { get; set; } = "";

        // Calendar Provider Selection
        public bool UseGoogleCalendar { get; set; } = false;
        public bool UseOutlookCalendar { get; set; } = false;
    }
}

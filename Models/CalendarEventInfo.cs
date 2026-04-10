namespace LocalAIAgent.Models
{
    public class CalendarEventInfo
    {
        public string Title { get; set; } = "";
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public string Source { get; set; } = ""; // Google or Outlook
    }
}

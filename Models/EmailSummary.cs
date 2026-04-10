namespace LocalAIAgent.Models
{
    public class EmailSummary
    {
        public string From { get; set; } = "";
        public string Subject { get; set; } = "";
        public string Body { get; set; } = "";
        public DateTime Date { get; set; }
    }
}

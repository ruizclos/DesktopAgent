namespace LocalAIAgent.Models
{
    public class AvailabilityBlock
    {
        public DateTime Start { get; set; }
        public DateTime End { get; set; }

        public override string ToString()
        {
            return $"{Start:MMM dd, yyyy — hh:mm tt} to {End:hh:mm tt}";
        }
    }
}

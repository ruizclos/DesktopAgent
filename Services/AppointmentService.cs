using System;
using System.Text;
using System.Threading.Tasks;

namespace LocalAIAgent.Services
{
    public class AppointmentService
    {
        private readonly AiService _ai;
        private readonly CalendarService _calendar;

        public AppointmentService(AiService ai, CalendarService calendar)
        {
            _ai = ai;
            _calendar = calendar;
        }

        // ---------------------------------------------------------
        // SCHEDULE APPOINTMENT
        // ---------------------------------------------------------
        public async Task<string> ScheduleAppointmentAsync(
            string clientName,
            DateTime start,
            DateTime end,
            string purpose)
        {
            // Generate a professional confirmation email
            var email = await _ai.GenerateAppointmentEmailAsync(
                clientName,
                start,
                end,
                purpose
            );

            return email;
        }

        // ---------------------------------------------------------
        // DESCRIBE AVAILABILITY (Used in MainViewModel)
        // ---------------------------------------------------------
        public async Task<string> DescribeAvailabilityAsync(DateTime start, DateTime end)
        {
            var blocks = await _calendar.GetAvailabilityAsync(start, end);

            var sb = new StringBuilder();
            foreach (var block in blocks)
            {
                sb.AppendLine($"{block.start:MMM dd, yyyy} — {block.start:hh:mm tt} to {block.end:hh:mm tt}");
            }

            // Summarize availability using AI
            var summary = await _ai.DescribeAvailabilityAsync(sb.ToString());
            return summary;
        }
    }
}

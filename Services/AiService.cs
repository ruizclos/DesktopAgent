using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using LocalAIAgent.Models;

namespace LocalAIAgent.Services
{
    public class AiService
    {
        private readonly HttpClient _http;
        private readonly ConfigService _config;

        public AiService(ConfigService config)
        {
            _config = config;
            _http = new HttpClient();
        }

        private record ChatRequest(string model, ChatMessage[] messages, int max_tokens);
        private record ChatMessage(string role, string content);
        private record ChatResponse(ChatChoice[] choices);
        private record ChatChoice(ChatMessage message);

        private async Task<string> SendAsync(string prompt)
        {
            if (string.IsNullOrWhiteSpace(_config.Config.ApiKey))
                throw new Exception("API key is missing.");

            var request = new ChatRequest(
                model: _config.Config.AiModel,
                messages: new[]
                {
                    new ChatMessage("system", _config.Config.AiSystemPrompt),
                    new ChatMessage("user", prompt)
                },
                max_tokens: _config.Config.AiMaxTokens
            );

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _http.DefaultRequestHeaders.Clear();
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _config.Config.ApiKey);

            var response = await _http.PostAsync(_config.Config.AiEndpoint, content);
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"AI Error: {body}");

            var parsed = JsonSerializer.Deserialize<ChatResponse>(body);
            return parsed?.choices?[0]?.message?.content ?? "";
        }

        // -------------------------
        // Public AI Methods
        // -------------------------

        public Task<string> SummarizeTextAsync(string text)
        {
            var prompt = $"Summarize the following text:\n\n{text}";
            return SendAsync(prompt);
        }

        public Task<string> GenerateEmailReplyAsync(string emailBody, string note)
        {
            var prompt =
                $"Write a professional email reply. Original email:\n\n{emailBody}\n\n" +
                $"User notes: {note}\n\nReply:";
            return SendAsync(prompt);
        }

        public Task<string> SummarizePdfContentAsync(string extractedText)
        {
            var prompt = $"Summarize this PDF content:\n\n{extractedText}";
            return SendAsync(prompt);
        }

        public Task<string> GenerateDailyReportAsync(string emailSummary, string calendarSummary)
        {
            var prompt =
                $"Create a daily report using:\n\nEmails:\n{emailSummary}\n\nCalendar:\n{calendarSummary}";
            return SendAsync(prompt);
        }

        public Task<string> DescribeAvailabilityAsync(string availabilityText)
        {
            var prompt = $"Summarize this availability:\n\n{availabilityText}";
            return SendAsync(prompt);
        }

        public Task<string> GenerateAppointmentEmailAsync(string client, DateTime start, DateTime end, string purpose)
        {
            var prompt =
                $"Write a professional appointment confirmation email.\n" +
                $"Client: {client}\nStart: {start}\nEnd: {end}\nPurpose: {purpose}";
            return SendAsync(prompt);
        }
    }
}

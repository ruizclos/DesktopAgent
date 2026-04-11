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

        private static bool IsGeminiEndpoint(string url) =>
            url.Contains("generativelanguage.googleapis.com", StringComparison.OrdinalIgnoreCase);

        private async Task<string> SendAsync(string prompt)
        {
            if (string.IsNullOrWhiteSpace(_config.Config.ApiKey))
                throw new Exception("API key is missing. Enter it in the AI tab.");

            var endpoint = _config.Config.AiEndpoint;

            _http.DefaultRequestHeaders.Clear();

            string json;
            string url;

            if (IsGeminiEndpoint(endpoint))
            {
                // Gemini REST format — key goes as query param, different body schema
                url = endpoint.Contains("?")
                    ? $"{endpoint}&key={_config.Config.ApiKey}"
                    : $"{endpoint}?key={_config.Config.ApiKey}";

                var geminiRequest = new
                {
                    contents = new[]
                    {
                        new { parts = new[] { new { text = _config.Config.AiSystemPrompt + "\n\n" + prompt } } }
                    },
                    generationConfig = new { maxOutputTokens = _config.Config.AiMaxTokens }
                };
                json = JsonSerializer.Serialize(geminiRequest);
            }
            else
            {
                // OpenAI-compatible format (OpenAI, Azure, local Ollama, etc.)
                url = endpoint;
                _http.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", _config.Config.ApiKey);

                var openAiRequest = new ChatRequest(
                    model: _config.Config.AiModel,
                    messages: new[]
                    {
                        new ChatMessage("system", _config.Config.AiSystemPrompt),
                        new ChatMessage("user", prompt)
                    },
                    max_tokens: _config.Config.AiMaxTokens
                );
                json = JsonSerializer.Serialize(openAiRequest);
            }

            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _http.PostAsync(url, content);
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"AI Error ({(int)response.StatusCode}): {body}");

            if (IsGeminiEndpoint(endpoint))
            {
                // Parse Gemini response: candidates[0].content.parts[0].text
                using var doc = JsonDocument.Parse(body);
                var text = doc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();
                return text ?? "";
            }
            else
            {
                var parsed = JsonSerializer.Deserialize<ChatResponse>(body);
                return parsed?.choices?[0]?.message?.content ?? "";
            }
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

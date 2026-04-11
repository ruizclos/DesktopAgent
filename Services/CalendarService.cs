using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LocalAIAgent.Models;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Services;

namespace LocalAIAgent.Services
{
    public class CalendarService
    {
        private readonly ConfigService _config;
        private readonly GoogleAuthService _googleAuth;

        public CalendarService(ConfigService config, GoogleAuthService googleAuth)
        {
            _config     = config;
            _googleAuth = googleAuth;
        }

        // ---------------------------------------------------------
        // PUBLIC: Get upcoming events summary (used in Daily Report)
        // ---------------------------------------------------------
        public async Task<string> GetUpcomingEventsSummaryAsync()
        {
            if (_config.Config.UseGoogleCalendar)
                return await GetGoogleCalendarSummaryAsync();

            if (_config.Config.UseOutlookCalendar)
                return await GetOutlookCalendarSummaryAsync();

            return "No calendar provider selected.";
        }

        // ---------------------------------------------------------
        // GOOGLE CALENDAR
        // ---------------------------------------------------------
        private async Task<string> GetGoogleCalendarSummaryAsync()
        {
            var cfg = _config.Config;

            UserCredential credential;
            try
            {
                credential = await _googleAuth.GetCredentialAsync();
            }
            catch (Exception ex)
            {
                return $"Google auth error: {ex.Message}";
            }

            var service = new Google.Apis.Calendar.v3.CalendarService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "Local AI Agent"
            });


            var request = service.Events.List("primary");
            request.TimeMinDateTimeOffset = DateTimeOffset.Now;
            request.TimeMaxDateTimeOffset = DateTimeOffset.Now.AddDays(7);
            request.SingleEvents = true;
            request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;


            var events = await request.ExecuteAsync();

            if (events.Items == null || events.Items.Count == 0)
                return "No upcoming Google Calendar events.";

            var sb = new StringBuilder();

            foreach (var ev in events.Items)
            {
                sb.AppendLine($"{ev.Summary} — {ev.Start.DateTimeDateTimeOffset} to {ev.End.DateTimeDateTimeOffset}");
            }

            return sb.ToString();
        }

        // ---------------------------------------------------------
        // OUTLOOK / MICROSOFT GRAPH CALENDAR
        // ---------------------------------------------------------
        private async Task<string> GetOutlookCalendarSummaryAsync()
        {
            var cfg = _config.Config;

            if (string.IsNullOrWhiteSpace(cfg.MicrosoftClientId) ||
                string.IsNullOrWhiteSpace(cfg.MicrosoftTenantId))
                return "Outlook Calendar credentials missing.";

            var app = PublicClientApplicationBuilder
                .Create(cfg.MicrosoftClientId)
                .WithTenantId(cfg.MicrosoftTenantId)
                .WithRedirectUri("http://localhost")
                .Build();

            var scopes = new[] { "Calendars.Read" };

            AuthenticationResult result;
            try
            {
                result = await app.AcquireTokenInteractive(scopes).ExecuteAsync();
            }
            catch (Exception ex)
            {
                return $"Outlook auth error: {ex.Message}";
            }



            // Use direct HTTP request for access token authentication (workaround for SDK v5+)
            var httpClient = new System.Net.Http.HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", result.AccessToken);
            var url = $"https://graph.microsoft.com/v1.0/me/events?$filter=start/dateTime ge '{DateTime.Now:O}' and start/dateTime le '{DateTime.Now.AddDays(7):O}'&$orderby=start/dateTime";
            var response = await httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                return $"Outlook API error: {response.StatusCode}";
            var json = await response.Content.ReadAsStringAsync();
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (!root.TryGetProperty("value", out var eventsArray) || eventsArray.ValueKind != System.Text.Json.JsonValueKind.Array || eventsArray.GetArrayLength() == 0)
                return "No upcoming Outlook events.";
            var sb = new StringBuilder();
            foreach (var ev in eventsArray.EnumerateArray())
            {
                string? subject = null;
                string? start = null;
                string? end = null;
                if (ev.TryGetProperty("subject", out var subjectProp) && subjectProp.ValueKind == System.Text.Json.JsonValueKind.String)
                {
                    subject = subjectProp.GetString();
                }
                if (ev.TryGetProperty("start", out var startProp) && startProp.ValueKind == System.Text.Json.JsonValueKind.Object)
                {
                    if (startProp.TryGetProperty("dateTime", out var sdp) && sdp.ValueKind == System.Text.Json.JsonValueKind.String)
                    {
                        start = sdp.GetString();
                    }
                }
                if (ev.TryGetProperty("end", out var endProp) && endProp.ValueKind == System.Text.Json.JsonValueKind.Object)
                {
                    if (endProp.TryGetProperty("dateTime", out var edp) && edp.ValueKind == System.Text.Json.JsonValueKind.String)
                    {
                        end = edp.GetString();
                    }
                }
                sb.AppendLine($"{subject ?? "(No Subject)"} — {start ?? "?"} to {end ?? "?"}");
            }
            return sb.ToString();
        }

        // ---------------------------------------------------------
        // PUBLIC: Availability description (used by AppointmentService)
        // ---------------------------------------------------------
        public async Task<List<(DateTime start, DateTime end)>> GetAvailabilityAsync(DateTime start, DateTime end)
        {
            // For now, we return simple availability blocks.
            // Later, this can be expanded to merge with real calendar data.

            var blocks = new List<(DateTime, DateTime)>();

            var current = start;
            while (current < end)
            {
                var blockStart = new DateTime(current.Year, current.Month, current.Day, 9, 0, 0);
                var blockEnd = blockStart.AddHours(8);

                blocks.Add((blockStart, blockEnd));
                current = current.AddDays(1);
            }

            return blocks;
        }
    }
}

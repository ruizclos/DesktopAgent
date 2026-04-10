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

        public CalendarService(ConfigService config)
        {
            _config = config;
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

            if (string.IsNullOrWhiteSpace(cfg.GoogleClientId) ||
                string.IsNullOrWhiteSpace(cfg.GoogleClientSecret))
                return "Google Calendar credentials missing.";

            var credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                new ClientSecrets
                {
                    ClientId = cfg.GoogleClientId,
                    ClientSecret = cfg.GoogleClientSecret
                },
                new[] { CalendarService.Scope.CalendarReadonly },
                "user",
                CancellationToken.None
            ).Result;

            var service = new Google.Apis.Calendar.v3.CalendarService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "Local AI Agent"
            });

            var request = service.Events.List("primary");
            request.TimeMin = DateTime.Now;
            request.TimeMax = DateTime.Now.AddDays(7);
            request.SingleEvents = true;
            request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;

            var events = await request.ExecuteAsync();

            if (events.Items == null || events.Items.Count == 0)
                return "No upcoming Google Calendar events.";

            var sb = new StringBuilder();
            foreach (var ev in events.Items)
            {
                sb.AppendLine($"{ev.Summary} — {ev.Start.DateTime} to {ev.End.DateTime}");
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

            var graphClient = new GraphServiceClient(
                new DelegateAuthenticationProvider(request =>
                {
                    request.Headers.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", result.AccessToken);
                    return Task.CompletedTask;
                })
            );

            var events = await graphClient.Me.Events
                .Request()
                .Filter($"start/dateTime ge '{DateTime.Now:O}' and start/dateTime le '{DateTime.Now.AddDays(7):O}'")
                .OrderBy("start/dateTime")
                .GetAsync();

            if (events.Count == 0)
                return "No upcoming Outlook events.";

            var sb = new StringBuilder();
            foreach (var ev in events)
            {
                sb.AppendLine($"{ev.Subject} — {ev.Start.DateTime} to {ev.End.DateTime}");
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

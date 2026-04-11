using System;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Util.Store;
using LocalAIAgent.Models;

namespace LocalAIAgent.Services
{
    /// <summary>
    /// Centralised Google OAuth2 service.
    /// A single browser sign-in grants access to both Gmail and Google Calendar.
    /// Tokens are cached in %APPDATA%\LocalAIAgent\Google.Auth.
    /// </summary>
    public class GoogleAuthService
    {
        // Combined scopes — user only logs in once for both services
        private static readonly string[] Scopes =
        {
            "https://mail.google.com/",
            Google.Apis.Calendar.v3.CalendarService.Scope.CalendarReadonly
        };

        private readonly ConfigService _config;

        public GoogleAuthService(ConfigService config)
        {
            _config = config;
        }

        private static string TokenStorePath =>
            System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "LocalAIAgent", "Google.Auth");

        /// <summary>
        /// Returns a valid UserCredential, opening a browser login if needed.
        /// </summary>
        public async Task<UserCredential> GetCredentialAsync()
        {
            var cfg = _config.Config;

            if (string.IsNullOrWhiteSpace(cfg.GmailOAuthClientId) ||
                string.IsNullOrWhiteSpace(cfg.GmailOAuthClientSecret))
                throw new Exception(
                    "Google OAuth2 Client ID or Secret is missing.\n" +
                    "Enter them in the Email tab → 'Google OAuth2 (MFA)' section.");

            var dataStore = new FileDataStore(TokenStorePath, fullPath: true);

            var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                new ClientSecrets
                {
                    ClientId     = cfg.GmailOAuthClientId,
                    ClientSecret = cfg.GmailOAuthClientSecret
                },
                Scopes,
                cfg.Email,
                CancellationToken.None,
                dataStore);

            // Silently refresh if the access token has expired
            if (credential.Token.IsStale)
                await credential.RefreshTokenAsync(CancellationToken.None);

            return credential;
        }

        /// <summary>
        /// Deletes the cached token so the next call to GetCredentialAsync opens a fresh login.
        /// </summary>
        public async Task RevokeAsync()
        {
            var cfg = _config.Config;
            var dataStore = new FileDataStore(TokenStorePath, fullPath: true);
            await dataStore.DeleteAsync<TokenResponse>(cfg.Email);
        }
    }
}

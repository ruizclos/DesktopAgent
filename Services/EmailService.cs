using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Util.Store;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit.Security;
using MimeKit;
using LocalAIAgent.Models;

namespace LocalAIAgent.Services
{
    public class EmailService
    {
        private readonly ConfigService _config;

        public EmailService(ConfigService config)
        {
            _config = config;
        }

        public async Task<List<MimeMessage>> GetRecentEmailsAsync(int count)
        {
            var cfg = _config.Config;

            if (string.IsNullOrWhiteSpace(cfg.Email) ||
                string.IsNullOrWhiteSpace(cfg.ImapServer))
                throw new Exception("Email or IMAP settings are missing.");

            using var client = new ImapClient();

            await client.ConnectAsync(cfg.ImapServer, cfg.ImapPort,
                cfg.UseSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls);

            if (cfg.UseGmailOAuth)
            {
                await AuthenticateWithOAuthAsync(client, cfg);
            }
            else
            {
                try
                {
                    await client.AuthenticateAsync(cfg.Email, cfg.EmailPassword);
                }
                catch (Exception authEx) when (
                    authEx.Message.Contains("Application-specific password", StringComparison.OrdinalIgnoreCase) ||
                    authEx.Message.Contains("app password", StringComparison.OrdinalIgnoreCase))
                {
                    throw new Exception(
                        "Gmail requires an App Password for IMAP access.\n\n" +
                        "Steps to fix:\n" +
                        "1. Go to myaccount.google.com/security\n" +
                        "2. Under \"How you sign in to Google\", enable 2-Step Verification\n" +
                        "3. Search for \"App passwords\" in your Google Account settings\n" +
                        "4. Create an App Password for \"Mail\"\n" +
                        "5. Paste the generated 16-character code into the Password field here\n\n" +
                        "Or switch to Google OAuth2 in the Email tab for browser-based MFA sign-in.");
                }
            }

            var inbox = client.Inbox;
            await inbox.OpenAsync(MailKit.FolderAccess.ReadOnly);

            var uids = await inbox.SearchAsync(SearchQuery.All);
            var messages = new List<MimeMessage>();

            for (int i = uids.Count - 1; i >= 0 && messages.Count < count; i--)
            {
                var msg = await inbox.GetMessageAsync(uids[i]);
                messages.Add(msg);
            }

            await client.DisconnectAsync(true);
            return messages;
        }

        private static async Task AuthenticateWithOAuthAsync(ImapClient client, AppConfig cfg)
        {
            if (string.IsNullOrWhiteSpace(cfg.GmailOAuthClientId) ||
                string.IsNullOrWhiteSpace(cfg.GmailOAuthClientSecret))
                throw new Exception(
                    "Gmail OAuth2 is enabled but Client ID or Client Secret is missing.\n" +
                    "Enter them in the Email tab under 'Google OAuth2 (MFA)'.");

            // Token cache stored in %APPDATA%\LocalAIAgent\Gmail.Auth
            var dataStore = new FileDataStore(
                System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "LocalAIAgent", "Gmail.Auth"),
                fullPath: true);

            var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                new ClientSecrets
                {
                    ClientId     = cfg.GmailOAuthClientId,
                    ClientSecret = cfg.GmailOAuthClientSecret
                },
                new[] { "https://mail.google.com/" },
                cfg.Email,
                CancellationToken.None,
                dataStore);

            // Refresh the token if it has expired
            if (credential.Token.IsStale)
                await credential.RefreshTokenAsync(CancellationToken.None);

            await client.AuthenticateAsync(
                new SaslMechanismOAuth2(cfg.Email, credential.Token.AccessToken));
        }

        /// <summary>Clears the locally cached OAuth2 token, forcing a fresh Google sign-in.</summary>
        public async Task RevokeOAuthTokenAsync()
        {
            var cfg = _config.Config;
            var dataStore = new FileDataStore(
                System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "LocalAIAgent", "Gmail.Auth"),
                fullPath: true);

            await dataStore.DeleteAsync<Google.Apis.Auth.OAuth2.Responses.TokenResponse>(cfg.Email);
        }

        public string ExtractBody(MimeMessage message)
        {
            if (message.HtmlBody != null)
                return message.HtmlBody;

            if (message.TextBody != null)
                return message.TextBody;

            return "(No readable content)";
        }
    }
}

namespace LocalAIAgent.Services
{
    public class EmailService
    {
        private readonly ConfigService _config;

        public EmailService(ConfigService config)
        {
            _config = config;
        }

        public async Task<List<MimeMessage>> GetRecentEmailsAsync(int count)
        {
            var cfg = _config.Config;

            if (string.IsNullOrWhiteSpace(cfg.Email) ||
                string.IsNullOrWhiteSpace(cfg.ImapServer))
                throw new Exception("Email or IMAP settings are missing.");

            using var client = new ImapClient();

            await client.ConnectAsync(cfg.ImapServer, cfg.ImapPort,
                cfg.UseSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls);

            try
            {
                await client.AuthenticateAsync(cfg.Email, cfg.EmailPassword);
            }
            catch (Exception authEx) when (
                authEx.Message.Contains("Application-specific password", StringComparison.OrdinalIgnoreCase) ||
                authEx.Message.Contains("app password", StringComparison.OrdinalIgnoreCase))
            {
                throw new Exception(
                    "Gmail requires an App Password for IMAP access.\n\n" +
                    "Steps to fix:\n" +
                    "1. Go to myaccount.google.com/security\n" +
                    "2. Under \"How you sign in to Google\", enable 2-Step Verification\n" +
                    "3. Search for \"App passwords\" in your Google Account settings\n" +
                    "4. Create an App Password for \"Mail\"\n" +
                    "5. Paste the generated 16-character code into the Password field here");
            }

            var inbox = client.Inbox;
            await inbox.OpenAsync(MailKit.FolderAccess.ReadOnly);

            var uids = await inbox.SearchAsync(SearchQuery.All);
            var messages = new List<MimeMessage>();

            for (int i = uids.Count - 1; i >= 0 && messages.Count < count; i--)
            {
                var msg = await inbox.GetMessageAsync(uids[i]);
                messages.Add(msg);
            }

            await client.DisconnectAsync(true);
            return messages;
        }

        public string ExtractBody(MimeMessage message)
        {
            if (message.HtmlBody != null)
                return message.HtmlBody;

            if (message.TextBody != null)
                return message.TextBody;

            return "(No readable content)";
        }
    }
}

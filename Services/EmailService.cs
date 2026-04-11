using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
        private readonly GoogleAuthService _googleAuth;

        public EmailService(ConfigService config, GoogleAuthService googleAuth)
        {
            _config     = config;
            _googleAuth = googleAuth;
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
                var credential = await _googleAuth.GetCredentialAsync();
                await client.AuthenticateAsync(
                    new MailKit.Security.SaslMechanismOAuth2(cfg.Email, credential.Token.AccessToken));
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

        /// <summary>Delegates token revocation to the shared GoogleAuthService.</summary>
        public Task RevokeOAuthTokenAsync() => _googleAuth.RevokeAsync();

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

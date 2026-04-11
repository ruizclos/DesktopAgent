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

            await client.AuthenticateAsync(cfg.Email, cfg.EmailPassword);

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

using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace IT15.Services
{
    public class EmailSender : IEmailSender
    {
        private readonly IConfiguration _configuration;

        public EmailSender(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            // Try Supabase first, then fall back to default SMTP settings (e.g., Gmail).
            var providers = new List<SmtpConfig>
            {
                ReadConfig("SupabaseSmtp"),
                ReadConfig("SmtpSettings")
            };

            using var message = new MailMessage
            {
                Subject = subject,
                Body = htmlMessage,
                IsBodyHtml = true
            };
            message.To.Add(new MailAddress(email));

            var errors = new List<string>();

            foreach (var provider in providers)
            {
                if (provider == null)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(provider.Host) ||
                    string.IsNullOrWhiteSpace(provider.Username) ||
                    string.IsNullOrWhiteSpace(provider.Password) ||
                    string.IsNullOrWhiteSpace(provider.FromEmail))
                {
                    errors.Add($"{provider.Name}: missing required settings");
                    continue;
                }

                message.From = new MailAddress(provider.FromEmail, provider.FromName ?? "WorkSync");

                using var client = new SmtpClient(provider.Host, provider.Port)
                {
                    EnableSsl = true,
                    Credentials = new NetworkCredential(provider.Username, provider.Password),
                    DeliveryMethod = SmtpDeliveryMethod.Network
                };

                try
                {
                    await client.SendMailAsync(message);
                    return; // Success, stop attempting other providers
                }
                catch (SmtpException ex)
                {
                    errors.Add($"{provider.Name}: {ex.Message}");
                }
                catch (System.Net.Sockets.SocketException ex)
                {
                    errors.Add($"{provider.Name}: {ex.Message}");
                }
            }

            throw new System.Exception("All SMTP providers failed: " + string.Join("; ", errors));
        }

        private SmtpConfig? ReadConfig(string sectionName)
        {
            var host = _configuration[$"{sectionName}:Host"];
            var portValue = _configuration[$"{sectionName}:Port"];
            var username = _configuration[$"{sectionName}:Username"];
            var password = _configuration[$"{sectionName}:Password"];
            var fromEmail = _configuration[$"{sectionName}:FromEmail"];
            var fromName = _configuration[$"{sectionName}:FromName"];

            if (string.IsNullOrWhiteSpace(host) &&
                string.IsNullOrWhiteSpace(username) &&
                string.IsNullOrWhiteSpace(password) &&
                string.IsNullOrWhiteSpace(fromEmail))
            {
                return null; // No config for this provider
            }

            var port = 587;
            if (!string.IsNullOrWhiteSpace(portValue) && int.TryParse(portValue, out var parsedPort))
            {
                port = parsedPort;
            }

            return new SmtpConfig
            {
                Name = sectionName,
                Host = host,
                Port = port,
                Username = username,
                Password = password,
                FromEmail = fromEmail,
                FromName = fromName
            };
        }

        private class SmtpConfig
        {
            public string Name { get; set; }
            public string Host { get; set; }
            public int Port { get; set; }
            public string Username { get; set; }
            public string Password { get; set; }
            public string FromEmail { get; set; }
            public string FromName { get; set; }
        }
    }
}

using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Configuration;
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
            // Supabase SMTP settings
            var host = _configuration["SupabaseSmtp:Host"];
            var portValue = _configuration["SupabaseSmtp:Port"];
            var username = _configuration["SupabaseSmtp:Username"];
            var password = _configuration["SupabaseSmtp:Password"];
            var fromEmail = _configuration["SupabaseSmtp:FromEmail"];
            var fromName = _configuration["SupabaseSmtp:FromName"] ?? "WorkSync";

            if (string.IsNullOrWhiteSpace(host) ||
                string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(fromEmail))
            {
                throw new System.Exception("Supabase SMTP settings are missing. Set SupabaseSmtp:Host, Port, Username, Password, FromEmail, and FromName.");
            }

            var port = 587;
            if (!string.IsNullOrWhiteSpace(portValue) && int.TryParse(portValue, out var parsedPort))
            {
                port = parsedPort;
            }

            using var client = new SmtpClient(host, port)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(username, password)
            };

            using var message = new MailMessage
            {
                From = new MailAddress(fromEmail, fromName),
                Subject = subject,
                Body = htmlMessage,
                IsBodyHtml = true
            };

            message.To.Add(new MailAddress(email));

            await client.SendMailAsync(message);
        }
    }
}

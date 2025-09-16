using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Configuration;
using SendGrid;
using SendGrid.Helpers.Mail;
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
            // Get SendGrid settings from appsettings.json
            var apiKey = _configuration["SendGrid:ApiKey"];
            var fromEmail = _configuration["SendGrid:FromEmail"];
            var fromName = _configuration["SendGrid:FromName"];

            if (string.IsNullOrEmpty(apiKey))
            {
                throw new System.Exception("SendGrid API Key is not configured.");
            }

            var client = new SendGridClient(apiKey);
            var from = new EmailAddress(fromEmail, fromName);
            var to = new EmailAddress(email);

            // Per SendGrid's recommendation, you should provide both plain text and HTML content.
            // For simplicity, we'll use the htmlMessage for both, as most modern clients will render the HTML.
            var plainTextContent = htmlMessage;
            var htmlContent = htmlMessage;

            var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
            var response = await client.SendEmailAsync(msg);

            if (!response.IsSuccessStatusCode)
            {
                // Log the error or handle it as needed
                var error = await response.Body.ReadAsStringAsync();
                throw new System.Exception($"SendGrid failed to send email: {response.StatusCode} - {error}");
            }
        }
    }
}
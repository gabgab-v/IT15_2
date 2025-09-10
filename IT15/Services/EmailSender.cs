using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text; // Required for StringContent and Encoding
using System.Text.Json; // Required for JsonSerializer
using System.Threading.Tasks;

namespace IT15.Services
{
    public class EmailSender : IEmailSender
    {
        // This service no longer needs IConfiguration injected.
        public EmailSender()
        {
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            // THE FIX: Hardcode your API key here for the final test.
            // Replace "re_YourApiKeyFromResend" with your actual key.
            var apiKey = "re_YourApiKeyFromResend";

            if (string.IsNullOrEmpty(apiKey) || apiKey == "re_DXsdeabQ_KHxgBA5J91xdEvoLgepvXC1K")
            {
                throw new System.Exception("API Key is not set in EmailSender.cs. Please replace the placeholder.");
            }

            // 1. Manually create and configure the HttpClient.
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            // 2. Create the JSON payload for the Resend API.
            var payload = new
            {
                from = "onboarding@resend.dev",
                to = email,
                subject = subject,
                html = htmlMessage
            };
            var jsonPayload = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            // 3. Send the POST request directly to the Resend API endpoint.
            var response = await httpClient.PostAsync("https://api.resend.com/emails", content);

            // 4. Check the response. If it's not successful, throw an exception with the error details.
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new System.Exception($"Resend API call failed with status code {response.StatusCode}: {errorContent}");
            }
        }
    }
}


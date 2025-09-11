using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace IT15.Services
{
    public class SmsSender : ISmsSender
    {
        private readonly IConfiguration _configuration;

        public SmsSender(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public Task SendSmsAsync(string number, string message)
        {
            var twilioSettings = _configuration.GetSection("TwilioSettings");
            var accountSid = twilioSettings["AccountSID"];
            var authToken = twilioSettings["AuthToken"];
            var twilioPhoneNumber = twilioSettings["PhoneNumber"];

            TwilioClient.Init(accountSid, authToken);

            return MessageResource.CreateAsync(
                to: new PhoneNumber(number),
                from: new PhoneNumber(twilioPhoneNumber),
                body: message);
        }
    }
}


using IT15.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace IT15.Services
{
    public class HolidayApiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public HolidayApiService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiKey = configuration["HolidayApiSettings:ApiKey"];
        }

        // THE FIX: This method now queries the API day-by-day to stay within the free plan.
        public async Task<List<Holiday>> GetUpcomingHolidaysAsync(string countryCode, int daysToLookAhead = 90)
        {
            if (string.IsNullOrEmpty(_apiKey))
            {
                return new List<Holiday>();
            }

            var upcomingHolidays = new List<Holiday>();
            var today = DateTime.Today;

            // Loop through each day for the next 90 days.
            for (int i = 0; i < daysToLookAhead; i++)
            {
                var dateToCheck = today.AddDays(i);

                // Construct the API URL for a single, specific day.
                var apiUrl = $"https://holidays.abstractapi.com/v1/?api_key={_apiKey}&country={countryCode}&year={dateToCheck.Year}&month={dateToCheck.Month}&day={dateToCheck.Day}";

                try
                {
                    // The API returns a list, even for one day. It will be empty if there's no holiday.
                    var holidaysForDay = await _httpClient.GetFromJsonAsync<List<Holiday>>(apiUrl);

                    if (holidaysForDay != null && holidaysForDay.Any())
                    {
                        // If a holiday is found, add it to our results.
                        upcomingHolidays.AddRange(holidaysForDay);
                    }
                }
                catch (HttpRequestException)
                {
                    // If a single day's API call fails, just continue to the next day.
                    // This makes the service more resilient.
                }
            }

            return upcomingHolidays;
        }
    }
}


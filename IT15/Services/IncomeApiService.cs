using IT15.Models;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace IT15.Services
{
    public class IncomeApiService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<IncomeApiService> _logger; 

        public IncomeApiService(HttpClient httpClient, ILogger<IncomeApiService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        // This method fetches all "sales" from the API and calculates the total income.
        public async Task<decimal> GetTotalIncomeAsync()
        {
            try
            {
                // Call the Fake Store API's products endpoint.
                var products = await _httpClient.GetFromJsonAsync<List<StoreProduct>>("products");

                if (products != null)
                {
                    // For our simulation, we'll assume the total income is the sum of all product prices.
                    return products.Sum(p => p.Price);
                }
            }
            catch (HttpRequestException ex)
            {
                // If the API fails, we'll assume zero income to be safe.
                // In a real app, you would log this error.
                _logger.LogError(ex, "No income, error");
            }

            return 0;
        }

        public async Task<List<StoreProduct>> GetProductsAsync()
        {
            try
            {
                var products = await _httpClient.GetFromJsonAsync<List<StoreProduct>>("products");
                return products ?? new List<StoreProduct>();
            }
            catch (HttpRequestException)
            {
                _logger.LogError(ex, "Failed to fetch products from Fake Store API.");
                return new List<StoreProduct>();
            }
        }
    }
}

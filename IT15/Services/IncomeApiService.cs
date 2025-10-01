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
        private const string ApiUrl = "https://fakestoreapi.com/products";

        public IncomeApiService(HttpClient httpClient, ILogger<IncomeApiService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;

            // Add headers if not already set (some APIs reject "bare" clients)
            if (!_httpClient.DefaultRequestHeaders.Contains("User-Agent"))
                _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");

            if (!_httpClient.DefaultRequestHeaders.Contains("Accept"))
                _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        }

        public async Task<decimal> GetTotalIncomeAsync()
        {
            var products = await GetProductsAsync();
            return products.Sum(p => p.Price);
        }

        public async Task<List<StoreProduct>> GetProductsAsync()
        {
            int retries = 3;
            for (int i = 0; i < retries; i++)
            {
                try
                {
                    _logger.LogInformation("Fetching products from Fake Store API (attempt {Attempt})", i + 1);

                    var products = await _httpClient.GetFromJsonAsync<List<StoreProduct>>(ApiUrl);

                    if (products != null)
                        return products;
                }
                catch (HttpRequestException ex)
                {
                    _logger.LogWarning(ex, "Attempt {Attempt} failed to fetch products.", i + 1);
                    await Task.Delay(2000 * (i + 1)); // backoff: 2s, 4s, 6s
                }
            }

            _logger.LogError("All retries failed. Returning empty product list.");
            return new List<StoreProduct>();
        }
    }
}

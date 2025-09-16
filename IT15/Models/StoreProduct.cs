using System.Text.Json.Serialization;

namespace IT15.Models
{
    // This model represents a product from the Fake Store API.
    // We only care about the price for our income calculation.
    public class StoreProduct
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("price")]
        public decimal Price { get; set; }

        [JsonPropertyName("image")]
        public string Image { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DotNetApiStarterKit.Models
{
    public class OrderItem
    {
        [JsonPropertyName("order_id")]
        public int OrderId { get; set; }

        [JsonPropertyName("part_id")]
        [Required(ErrorMessage = "Part ID is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Part ID must be a positive number")]
        public int PartId { get; set; }

        [JsonPropertyName("quantity")]
        [Required(ErrorMessage = "Quantity is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be a positive number")]
        public int Quantity { get; set; }

        [JsonPropertyName("price")]
        [Required(ErrorMessage = "Price is required")]
        [Range(1, double.MaxValue, ErrorMessage = "Price must be a positive number")]
        public decimal Price { get; set; }

        [JsonPropertyName("totalprice")]
        [Required(ErrorMessage = "TotalPrice is required")]
        [Range(1, double.MaxValue, ErrorMessage = "TotalPrice must be a positive number")]
        public decimal TotalPrice { get; set; }
    }
}

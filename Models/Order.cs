using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace DotNetApiStarterKit.Models
{
    public class Order
    {
        [JsonPropertyName("order_id")]
        public int OrderId { get; set; }

        [JsonPropertyName("customer_id")]
        [Required(ErrorMessage = "Customer ID is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Customer ID must be a positive number")]
        public int CustomerId { get; set; }

        [JsonPropertyName("order_date")]
        [Required(ErrorMessage = "Order date is required")]
        [RegularExpression(@"^\d{4}-\d{2}-\d{2}$", ErrorMessage = "Order date must be in YYYY-MM-DD format")]
        public string OrderDate { get; set; } = string.Empty;

        [JsonPropertyName("orderitems")]
        [Required(ErrorMessage = "Order items are required")]
        [MinLength(1, ErrorMessage = "At least one order item is required")]
        public List<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

        public bool IsValidDate()
        {
            return DateTime.TryParseExact(this.OrderDate, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out _);
        }
    }
}

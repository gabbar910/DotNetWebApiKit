using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace DotNetApiStarterKit.Models
{
    public class Order
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)] // Preserve original IDs during migration
        [JsonPropertyName("order_id")]
        public int OrderId { get; set; }

        [Required(ErrorMessage = "Customer ID is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Customer ID must be a positive number")]
        [JsonPropertyName("customer_id")]
        public int CustomerId { get; set; }

        [ForeignKey("CustomerId")]
        public Customer? Customer { get; set; }

        [Required(ErrorMessage = "Order date is required")]
        [JsonPropertyName("order_date")]
        public DateTime OrderDate { get; set; }

        [Precision(18, 2)]
        public decimal TotalAmount { get; set; }

        [JsonPropertyName("orderitems")]
        public List<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

        public bool IsValidDate()
        {
            return DateTime.TryParseExact(this.OrderDate.ToString("yyyy-MM-dd"), "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out _);
        }
    }
}

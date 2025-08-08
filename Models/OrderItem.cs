using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace DotNetApiStarterKit.Models
{
    public class OrderItem
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ItemId { get; set; }

        [JsonPropertyName("order_id")]
        [Required]
        public int OrderId { get; set; }

        [ForeignKey("OrderId")]
        public Order? Order { get; set; }

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
        [Range(typeof(decimal), "0.01", "79228162514264337593543950335", ErrorMessage = "Price must be a positive number")]
        [Precision(18, 2)]
        public decimal Price { get; set; }

        [JsonPropertyName("totalprice")]
        [Required(ErrorMessage = "TotalPrice is required")]
        [Range(typeof(decimal), "0.01", "79228162514264337593543950335", ErrorMessage = "TotalPrice must be a positive number")]
        [Precision(18, 2)]
        public decimal TotalPrice { get; set; }
    }
}

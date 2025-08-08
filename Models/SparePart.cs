namespace DotNetApiStarterKit.Models
{
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    public class SparePart
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)] // Use existing IDs from JSON
        public int PartId { get; set; }

        [Required]
        [StringLength(100)]
        public string PartName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Category { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Manufacturer { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string CompatibleMake { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string CompatibleModel { get; set; } = string.Empty;

        public int StockQuantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [StringLength(100)]
        public string Location { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property for order items
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}

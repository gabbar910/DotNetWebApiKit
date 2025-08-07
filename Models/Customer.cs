namespace DotNetApiStarterKit.Models
{
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    public class Customer
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CustomerId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string Address { get; set; } = string.Empty;

        [Required]
        [MaxLength(10)]
        public string Pincode { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string State { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string City { get; set; } = string.Empty;
    }
}

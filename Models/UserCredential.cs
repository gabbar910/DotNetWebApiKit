namespace DotNetApiStarterKit.Models
{
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    public class UserCredential
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int UserId { get; set; }

        [Required]
        [MaxLength(50)]
        [Column(TypeName = "TEXT")]
        public string Username { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        [Column(TypeName = "TEXT")]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        [Column(TypeName = "TEXT")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "TEXT")]
        public DateTime CreatedAt { get; set; }

        [Column(TypeName = "TEXT")]
        public DateTime LastLoginAt { get; set; }

        [Required]
        [Column(TypeName = "INTEGER")]
        public bool IsActive { get; set; } = true;
    }
}

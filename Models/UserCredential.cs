namespace DotNetApiStarterKit.Models
{
    public class UserCredential
    {
        public int UserId { get; set; }

        public string Username { get; set; } = string.Empty;

        public string PasswordHash { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public DateTime LastLoginAt { get; set; }

        public bool IsActive { get; set; } = true;
    }
}

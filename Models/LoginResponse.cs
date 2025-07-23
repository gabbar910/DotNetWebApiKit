namespace DotNetApiStarterKit.Models
{
    public class LoginResponse
    {
        public string Token { get; set; } = string.Empty;

        public DateTime Expires { get; set; }

        public string Username { get; set; } = string.Empty;
    }
}

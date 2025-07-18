namespace DotNetApiStarterKit.Models
{
    public class Customer
    {
        public int CustomerId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Address { get; set; } = string.Empty;

        public string Pincode { get; set; } = string.Empty;

        public string State { get; set; } = string.Empty;

        public string City { get; set; } = string.Empty;
    }
}

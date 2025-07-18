namespace DotNetApiStarterKit.Models
{
    public class SparePart
    {
        public int PartId { get; set; }

        public string PartName { get; set; } = string.Empty;

        public string Category { get; set; } = string.Empty;

        public string Manufacturer { get; set; } = string.Empty;

        public string CompatibleMake { get; set; } = string.Empty;

        public string CompatibleModel { get; set; } = string.Empty;

        public int StockQuantity { get; set; }

        public decimal Price { get; set; }

        public string Location { get; set; } = string.Empty;
    }
}

namespace DotNetApiStarterKit.Services
{
    using System.Text.Json;
    using DotNetApiStarterKit.Models;

    public interface ISparePartsService
    {
        Task<IEnumerable<SparePart>> GetAllSparePartsAsync();

        Task<SparePart?> GetSparePartByIdAsync(int id);

        Task<IEnumerable<SparePart>> GetSparePartsByCategoryAsync(string category);

        Task<IEnumerable<SparePart>> GetSparePartsByManufacturerAsync(string manufacturer);

        Task<IEnumerable<SparePart>> GetSparePartsByMakeAsync(string make);

        Task<IEnumerable<SparePart>> GetLowStockPartsAsync(int threshold = 10);

        Task<SparePart> CreateSparePartAsync(SparePart sparePart);

        Task<IEnumerable<SparePart>> SearchSparePartsAsync(string? category = null, string? manufacturer = null, string? make = null);
    }

    public class SparePartsService : ISparePartsService
    {
        private readonly string dataFilePath;
        private readonly ILogger<SparePartsService> logger;
        private List<SparePart>? cachedParts;

        public SparePartsService(IWebHostEnvironment environment, ILogger<SparePartsService> logger)
        {
            this.dataFilePath = Path.Combine(environment.ContentRootPath, "data", "spareparts_inventory.json");
            this.logger = logger;
        }

        public async Task<IEnumerable<SparePart>> GetAllSparePartsAsync()
        {
            var parts = await this.LoadSparePartsAsync();
            return parts;
        }

        public async Task<SparePart?> GetSparePartByIdAsync(int id)
        {
            var parts = await this.LoadSparePartsAsync();
            return parts.FirstOrDefault(p => p.PartId == id);
        }

        public async Task<IEnumerable<SparePart>> GetSparePartsByCategoryAsync(string category)
        {
            var parts = await this.LoadSparePartsAsync();
            return parts.Where(p => p.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
        }

        public async Task<IEnumerable<SparePart>> GetSparePartsByManufacturerAsync(string manufacturer)
        {
            var parts = await this.LoadSparePartsAsync();
            return parts.Where(p => p.Manufacturer.Equals(manufacturer, StringComparison.OrdinalIgnoreCase));
        }

        public async Task<IEnumerable<SparePart>> GetSparePartsByMakeAsync(string make)
        {
            var parts = await this.LoadSparePartsAsync();
            return parts.Where(p => p.CompatibleMake.Equals(make, StringComparison.OrdinalIgnoreCase));
        }

        public async Task<IEnumerable<SparePart>> GetLowStockPartsAsync(int threshold = 10)
        {
            var parts = await this.LoadSparePartsAsync();
            return parts.Where(p => p.StockQuantity <= threshold).OrderBy(p => p.StockQuantity);
        }

        public async Task<IEnumerable<SparePart>> SearchSparePartsAsync(string? category = null, string? manufacturer = null, string? make = null)
        {
            var parts = await this.LoadSparePartsAsync();
            var query = parts.AsEnumerable();

            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(p => p.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrEmpty(manufacturer))
            {
                query = query.Where(p => p.Manufacturer.Equals(manufacturer, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrEmpty(make))
            {
                query = query.Where(p => p.CompatibleMake.Equals(make, StringComparison.OrdinalIgnoreCase));
            }

            return query;
        }

        public async Task<SparePart> CreateSparePartAsync(SparePart sparePart)
        {
            var spareparts = await this.LoadSparePartsAsync();

            // Generate new ID
            var maxId = spareparts.Any() ? spareparts.Max(p => p.PartId) : 0;
            sparePart.PartId = maxId + 1;

            spareparts.Add(sparePart);
            await this.SavePartsAsync(spareparts);

            this.logger.LogInformation("Created new spare part with ID {PartId}", sparePart.PartId);
            return sparePart;
        }

        private async Task<List<SparePart>> LoadSparePartsAsync()
        {
            if (this.cachedParts != null)
            {
                return this.cachedParts;
            }

            try
            {
                if (!File.Exists(this.dataFilePath))
                {
                    this.logger.LogError("Spare parts data file not found at: {FilePath}", this.dataFilePath);
                    return new List<SparePart>();
                }

                var jsonContent = await File.ReadAllTextAsync(this.dataFilePath);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                };

                this.cachedParts = JsonSerializer.Deserialize<List<SparePart>>(jsonContent, options) ?? new List<SparePart>();
                this.logger.LogInformation("Loaded {Count} spare parts from data file", this.cachedParts.Count);

                return this.cachedParts;
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error loading spare parts data from file: {FilePath}", this.dataFilePath);
                return new List<SparePart>();
            }
        }

        private async Task SavePartsAsync(List<SparePart> parts)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                    WriteIndented = true,
                };

                var jsonContent = JsonSerializer.Serialize(parts, options);
                await File.WriteAllTextAsync(this.dataFilePath, jsonContent);
                this.cachedParts = parts;
                this.logger.LogInformation("Saved {Count} parts to data file", parts.Count);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error saving parts data to file: {FilePath}", this.dataFilePath);
                throw;
            }
        }
    }
}

namespace DotNetApiStarterKit.Services
{
    using DotNetApiStarterKit.Data;
    using DotNetApiStarterKit.Models;
    using Microsoft.EntityFrameworkCore;

    public interface ISparePartsService
    {
        Task<IEnumerable<SparePart>> GetAllSparePartsAsync();

        Task<SparePart?> GetSparePartByIdAsync(int id);

        Task<IEnumerable<SparePart>> GetSparePartsByCategoryAsync(string category);

        Task<IEnumerable<SparePart>> GetSparePartsByManufacturerAsync(string manufacturer);

        Task<IEnumerable<SparePart>> GetSparePartsByMakeAsync(string make);

        Task<IEnumerable<SparePart>> GetLowStockPartsAsync(int threshold = 10);

        Task<SparePart> CreateSparePartAsync(SparePart sparePart);

        Task<SparePart> UpdateSparePartAsync(SparePart sparePart);

        Task<bool> DeleteSparePartAsync(int id);

        Task<IEnumerable<SparePart>> SearchSparePartsAsync(string? category = null, string? manufacturer = null, string? make = null);
    }

    public class SparePartsService : ISparePartsService
    {
        private readonly AppDbContext context;
        private readonly ILogger<SparePartsService> logger;

        public SparePartsService(AppDbContext context, ILogger<SparePartsService> logger)
        {
            this.context = context;
            this.logger = logger;
        }

        public async Task<IEnumerable<SparePart>> GetAllSparePartsAsync()
        {
            try
            {
                var parts = await this.context.SpareParts.ToListAsync();
                this.logger.LogInformation("Retrieved {Count} spare parts from database", parts.Count);
                return parts;
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error retrieving all spare parts from database");
                throw;
            }
        }

        public async Task<SparePart?> GetSparePartByIdAsync(int id)
        {
            try
            {
                var part = await this.context.SpareParts.FirstOrDefaultAsync(p => p.PartId == id);
                if (part == null)
                {
                    this.logger.LogWarning("Spare part with ID {PartId} not found", id);
                }
                return part;
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error retrieving spare part with ID {PartId} from database", id);
                throw;
            }
        }

        public async Task<IEnumerable<SparePart>> GetSparePartsByCategoryAsync(string category)
        {
            try
            {
                var parts = await this.context.SpareParts
                    .Where(p => p.Category.ToLower() == category.ToLower())
                    .ToListAsync();
                this.logger.LogInformation("Retrieved {Count} spare parts for category {Category}", parts.Count, category);
                return parts;
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error retrieving spare parts for category {Category} from database", category);
                throw;
            }
        }

        public async Task<IEnumerable<SparePart>> GetSparePartsByManufacturerAsync(string manufacturer)
        {
            try
            {
                var parts = await this.context.SpareParts
                    .Where(p => p.Manufacturer.ToLower() == manufacturer.ToLower())
                    .ToListAsync();
                this.logger.LogInformation("Retrieved {Count} spare parts for manufacturer {Manufacturer}", parts.Count, manufacturer);
                return parts;
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error retrieving spare parts for manufacturer {Manufacturer} from database", manufacturer);
                throw;
            }
        }

        public async Task<IEnumerable<SparePart>> GetSparePartsByMakeAsync(string make)
        {
            try
            {
                var parts = await this.context.SpareParts
                    .Where(p => p.CompatibleMake.ToLower() == make.ToLower())
                    .ToListAsync();
                this.logger.LogInformation("Retrieved {Count} spare parts for make {Make}", parts.Count, make);
                return parts;
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error retrieving spare parts for make {Make} from database", make);
                throw;
            }
        }

        public async Task<IEnumerable<SparePart>> GetLowStockPartsAsync(int threshold = 10)
        {
            try
            {
                var parts = await this.context.SpareParts
                    .Where(p => p.StockQuantity <= threshold)
                    .OrderBy(p => p.StockQuantity)
                    .ToListAsync();
                this.logger.LogInformation("Retrieved {Count} low stock spare parts with threshold {Threshold}", parts.Count, threshold);
                return parts;
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error retrieving low stock spare parts from database");
                throw;
            }
        }

        public async Task<IEnumerable<SparePart>> SearchSparePartsAsync(string? category = null, string? manufacturer = null, string? make = null)
        {
            try
            {
                var query = this.context.SpareParts.AsQueryable();

                if (!string.IsNullOrEmpty(category))
                {
                    query = query.Where(p => p.Category.ToLower() == category.ToLower());
                }

                if (!string.IsNullOrEmpty(manufacturer))
                {
                    query = query.Where(p => p.Manufacturer.ToLower() == manufacturer.ToLower());
                }

                if (!string.IsNullOrEmpty(make))
                {
                    query = query.Where(p => p.CompatibleMake.ToLower() == make.ToLower());
                }

                var parts = await query.ToListAsync();
                this.logger.LogInformation("Search returned {Count} spare parts for category: {Category}, manufacturer: {Manufacturer}, make: {Make}", 
                    parts.Count, category ?? "any", manufacturer ?? "any", make ?? "any");
                return parts;
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error searching spare parts in database");
                throw;
            }
        }

        public async Task<SparePart> CreateSparePartAsync(SparePart sparePart)
        {
            try
            {
                // Generate new ID if not provided
                if (sparePart.PartId <= 0)
                {
                    var maxId = await this.context.SpareParts.AnyAsync() 
                        ? await this.context.SpareParts.MaxAsync(p => p.PartId) 
                        : 0;
                    sparePart.PartId = maxId + 1;
                }

                sparePart.CreatedAt = DateTime.UtcNow;
                sparePart.UpdatedAt = DateTime.UtcNow;

                this.context.SpareParts.Add(sparePart);
                await this.context.SaveChangesAsync();

                this.logger.LogInformation("Created new spare part with ID {PartId}", sparePart.PartId);
                return sparePart;
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error creating spare part in database");
                throw;
            }
        }

        public async Task<SparePart> UpdateSparePartAsync(SparePart sparePart)
        {
            try
            {
                var existingPart = await this.context.SpareParts.FirstOrDefaultAsync(p => p.PartId == sparePart.PartId);
                if (existingPart == null)
                {
                    throw new InvalidOperationException($"Spare part with ID {sparePart.PartId} not found");
                }

                // Update properties
                existingPart.PartName = sparePart.PartName;
                existingPart.Category = sparePart.Category;
                existingPart.Manufacturer = sparePart.Manufacturer;
                existingPart.CompatibleMake = sparePart.CompatibleMake;
                existingPart.CompatibleModel = sparePart.CompatibleModel;
                existingPart.StockQuantity = sparePart.StockQuantity;
                existingPart.Price = sparePart.Price;
                existingPart.Location = sparePart.Location;
                existingPart.UpdatedAt = DateTime.UtcNow;

                await this.context.SaveChangesAsync();

                this.logger.LogInformation("Updated spare part with ID {PartId}", sparePart.PartId);
                return existingPart;
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error updating spare part with ID {PartId} in database", sparePart.PartId);
                throw;
            }
        }

        public async Task<bool> DeleteSparePartAsync(int id)
        {
            try
            {
                var part = await this.context.SpareParts.FirstOrDefaultAsync(p => p.PartId == id);
                if (part == null)
                {
                    this.logger.LogWarning("Spare part with ID {PartId} not found for deletion", id);
                    return false;
                }

                // Check if part is referenced in any order items
                var hasOrderItems = await this.context.OrderItems.AnyAsync(oi => oi.PartId == id);
                if (hasOrderItems)
                {
                    throw new InvalidOperationException($"Cannot delete spare part with ID {id} because it is referenced in existing orders");
                }

                this.context.SpareParts.Remove(part);
                await this.context.SaveChangesAsync();

                this.logger.LogInformation("Deleted spare part with ID {PartId}", id);
                return true;
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error deleting spare part with ID {PartId} from database", id);
                throw;
            }
        }
    }
}

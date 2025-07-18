namespace DotNetApiStarterKit.Controllers
{
    using DotNetApiStarterKit.Models;
    using DotNetApiStarterKit.Services;
    using Microsoft.AspNetCore.Mvc;

    [ApiController]
    [Route("api/[controller]")]
    public class SparePartsController : ControllerBase
    {
        private readonly ISparePartsService sparePartsService;
        private readonly ILogger<SparePartsController> logger;

        public SparePartsController(ISparePartsService sparePartsService, ILogger<SparePartsController> logger)
        {
            this.sparePartsService = sparePartsService;
            this.logger = logger;
        }

        /// <summary>
        /// Get all spare parts with optional filtering
        /// </summary>
        /// <param name="category">Filter by category (optional)</param>
        /// <param name="manufacturer">Filter by manufacturer (optional)</param>
        /// <param name="make">Filter by compatible make (optional)</param>
        /// <returns>List of spare parts</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SparePart>>> GetSpareParts(
            [FromQuery] string? category = null,
            [FromQuery] string? manufacturer = null,
            [FromQuery] string? make = null)
        {
            try
            {
                IEnumerable<SparePart> parts;

                if (!string.IsNullOrEmpty(category) || !string.IsNullOrEmpty(manufacturer) || !string.IsNullOrEmpty(make))
                {
                    parts = await this.sparePartsService.SearchSparePartsAsync(category, manufacturer, make);
                }
                else
                {
                    parts = await this.sparePartsService.GetAllSparePartsAsync();
                }

                return this.Ok(parts);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error retrieving spare parts");
                return this.StatusCode(500, "An error occurred while retrieving spare parts");
            }
        }

        /// <summary>
        /// Get a specific spare part by ID
        /// </summary>
        /// <param name="id">Part ID</param>
        /// <returns>Spare part details</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<SparePart>> GetSparePart(int id)
        {
            try
            {
                var part = await this.sparePartsService.GetSparePartByIdAsync(id);

                if (part == null)
                {
                    return this.NotFound($"Spare part with ID {id} not found");
                }

                return this.Ok(part);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error retrieving spare part with ID {PartId}", id);
                return this.StatusCode(500, "An error occurred while retrieving the spare part");
            }
        }

        /// <summary>
        /// Get spare parts by category
        /// </summary>
        /// <param name="category">Category name</param>
        /// <returns>List of spare parts in the specified category</returns>
        [HttpGet("category/{category}")]
        public async Task<ActionResult<IEnumerable<SparePart>>> GetSparePartsByCategory(string category)
        {
            try
            {
                var parts = await this.sparePartsService.GetSparePartsByCategoryAsync(category);
                return this.Ok(parts);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error retrieving spare parts for category {Category}", category);
                return this.StatusCode(500, "An error occurred while retrieving spare parts by category");
            }
        }

        /// <summary>
        /// Get spare parts by manufacturer
        /// </summary>
        /// <param name="manufacturer">Manufacturer name</param>
        /// <returns>List of spare parts from the specified manufacturer</returns>
        [HttpGet("manufacturer/{manufacturer}")]
        public async Task<ActionResult<IEnumerable<SparePart>>> GetSparePartsByManufacturer(string manufacturer)
        {
            try
            {
                var parts = await this.sparePartsService.GetSparePartsByManufacturerAsync(manufacturer);
                return this.Ok(parts);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error retrieving spare parts for manufacturer {Manufacturer}", manufacturer);
                return this.StatusCode(500, "An error occurred while retrieving spare parts by manufacturer");
            }
        }

        /// <summary>
        /// Get spare parts by compatible vehicle make
        /// </summary>
        /// <param name="make">Vehicle make</param>
        /// <returns>List of spare parts compatible with the specified make</returns>
        [HttpGet("make/{make}")]
        public async Task<ActionResult<IEnumerable<SparePart>>> GetSparePartsByMake(string make)
        {
            try
            {
                var parts = await this.sparePartsService.GetSparePartsByMakeAsync(make);
                return this.Ok(parts);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error retrieving spare parts for make {Make}", make);
                return this.StatusCode(500, "An error occurred while retrieving spare parts by make");
            }
        }

        /// <summary>
        /// Get spare parts with low stock
        /// </summary>
        /// <param name="threshold">Stock threshold (default: 10)</param>
        /// <returns>List of spare parts with stock below the threshold</returns>
        [HttpGet("stock/low")]
        public async Task<ActionResult<IEnumerable<SparePart>>> GetLowStockParts([FromQuery] int threshold = 10)
        {
            try
            {
                var parts = await this.sparePartsService.GetLowStockPartsAsync(threshold);
                return this.Ok(parts);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error retrieving low stock parts with threshold {Threshold}", threshold);
                return this.StatusCode(500, "An error occurred while retrieving low stock parts");
            }
        }

        /// <summary>
        /// Get available categories
        /// </summary>
        /// <returns>List of unique categories</returns>
        [HttpGet("categories")]
        public async Task<ActionResult<IEnumerable<string>>> GetCategories()
        {
            try
            {
                var parts = await this.sparePartsService.GetAllSparePartsAsync();
                var categories = parts.Select(p => p.Category).Distinct().OrderBy(c => c);
                return this.Ok(categories);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error retrieving categories");
                return this.StatusCode(500, "An error occurred while retrieving categories");
            }
        }

        /// <summary>
        /// Get available manufacturers
        /// </summary>
        /// <returns>List of unique manufacturers</returns>
        [HttpGet("manufacturers")]
        public async Task<ActionResult<IEnumerable<string>>> GetManufacturers()
        {
            try
            {
                var parts = await this.sparePartsService.GetAllSparePartsAsync();
                var manufacturers = parts.Select(p => p.Manufacturer).Distinct().OrderBy(m => m);
                return this.Ok(manufacturers);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error retrieving manufacturers");
                return this.StatusCode(500, "An error occurred while retrieving manufacturers");
            }
        }

        /// <summary>
        /// Get available vehicle makes
        /// </summary>
        /// <returns>List of unique vehicle makes</returns>
        [HttpGet("makes")]
        public async Task<ActionResult<IEnumerable<string>>> GetMakes()
        {
            try
            {
                var parts = await this.sparePartsService.GetAllSparePartsAsync();
                var makes = parts.Select(p => p.CompatibleMake).Distinct().OrderBy(m => m);
                return this.Ok(makes);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error retrieving makes");
                return this.StatusCode(500, "An error occurred while retrieving makes");
            }
        }
    }
}

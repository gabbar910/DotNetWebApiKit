namespace DotNetApiStarterKit.Controllers
{
    using DotNetApiStarterKit.Models;
    using DotNetApiStarterKit.Services;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;

    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
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
        /// Get all spare parts
        /// </summary>
        /// <returns>List of all spare parts</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SparePart>>> GetAllSpareParts()
        {
            try
            {
                var spareParts = await this.sparePartsService.GetAllSparePartsAsync();
                return Ok(spareParts);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error retrieving all spare parts");
                return StatusCode(500, "Internal server error while retrieving spare parts");
            }
        }

        /// <summary>
        /// Get spare part by ID
        /// </summary>
        /// <param name="id">Spare part ID</param>
        /// <returns>Spare part details</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<SparePart>> GetSparePartById(int id)
        {
            try
            {
                var sparePart = await this.sparePartsService.GetSparePartByIdAsync(id);
                if (sparePart == null)
                {
                    return NotFound($"Spare part with ID {id} not found");
                }
                return Ok(sparePart);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error retrieving spare part with ID {PartId}", id);
                return StatusCode(500, "Internal server error while retrieving spare part");
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
                var spareParts = await this.sparePartsService.GetSparePartsByCategoryAsync(category);
                return Ok(spareParts);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error retrieving spare parts for category {Category}", category);
                return StatusCode(500, "Internal server error while retrieving spare parts by category");
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
                var spareParts = await this.sparePartsService.GetSparePartsByManufacturerAsync(manufacturer);
                return Ok(spareParts);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error retrieving spare parts for manufacturer {Manufacturer}", manufacturer);
                return StatusCode(500, "Internal server error while retrieving spare parts by manufacturer");
            }
        }

        /// <summary>
        /// Get spare parts by compatible make
        /// </summary>
        /// <param name="make">Vehicle make</param>
        /// <returns>List of spare parts compatible with the specified make</returns>
        [HttpGet("make/{make}")]
        public async Task<ActionResult<IEnumerable<SparePart>>> GetSparePartsByMake(string make)
        {
            try
            {
                var spareParts = await this.sparePartsService.GetSparePartsByMakeAsync(make);
                return Ok(spareParts);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error retrieving spare parts for make {Make}", make);
                return StatusCode(500, "Internal server error while retrieving spare parts by make");
            }
        }

        /// <summary>
        /// Get low stock spare parts
        /// </summary>
        /// <param name="threshold">Stock threshold (default: 10)</param>
        /// <returns>List of spare parts with stock below the threshold</returns>
        [HttpGet("low-stock")]
        public async Task<ActionResult<IEnumerable<SparePart>>> GetLowStockParts([FromQuery] int threshold = 10)
        {
            try
            {
                var spareParts = await this.sparePartsService.GetLowStockPartsAsync(threshold);
                return Ok(spareParts);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error retrieving low stock spare parts");
                return StatusCode(500, "Internal server error while retrieving low stock spare parts");
            }
        }

        /// <summary>
        /// Search spare parts with optional filters
        /// </summary>
        /// <param name="category">Optional category filter</param>
        /// <param name="manufacturer">Optional manufacturer filter</param>
        /// <param name="make">Optional make filter</param>
        /// <returns>List of spare parts matching the search criteria</returns>
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<SparePart>>> SearchSpareParts(
            [FromQuery] string? category = null,
            [FromQuery] string? manufacturer = null,
            [FromQuery] string? make = null)
        {
            try
            {
                var spareParts = await this.sparePartsService.SearchSparePartsAsync(category, manufacturer, make);
                return Ok(spareParts);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error searching spare parts");
                return StatusCode(500, "Internal server error while searching spare parts");
            }
        }

        /// <summary>
        /// Create a new spare part
        /// </summary>
        /// <param name="sparePart">Spare part details</param>
        /// <returns>Created spare part</returns>
        [HttpPost]
        public async Task<ActionResult<SparePart>> CreateSparePart([FromBody] SparePart sparePart)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var createdSparePart = await this.sparePartsService.CreateSparePartAsync(sparePart);
                return CreatedAtAction(nameof(GetSparePartById), new { id = createdSparePart.PartId }, createdSparePart);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error creating spare part");
                return StatusCode(500, "Internal server error while creating spare part");
            }
        }

        /// <summary>
        /// Update an existing spare part
        /// </summary>
        /// <param name="id">Spare part ID</param>
        /// <param name="sparePart">Updated spare part details</param>
        /// <returns>Updated spare part</returns>
        [HttpPut("{id}")]
        public async Task<ActionResult<SparePart>> UpdateSparePart(int id, [FromBody] SparePart sparePart)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                if (id != sparePart.PartId)
                {
                    return BadRequest("ID in URL does not match ID in request body");
                }

                var updatedSparePart = await this.sparePartsService.UpdateSparePartAsync(sparePart);
                return Ok(updatedSparePart);
            }
            catch (InvalidOperationException ex)
            {
                this.logger.LogWarning(ex, "Spare part with ID {PartId} not found for update", id);
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error updating spare part with ID {PartId}", id);
                return StatusCode(500, "Internal server error while updating spare part");
            }
        }

        /// <summary>
        /// Delete a spare part
        /// </summary>
        /// <param name="id">Spare part ID</param>
        /// <returns>Success status</returns>
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteSparePart(int id)
        {
            try
            {
                var deleted = await this.sparePartsService.DeleteSparePartAsync(id);
                if (!deleted)
                {
                    return NotFound($"Spare part with ID {id} not found");
                }
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                this.logger.LogWarning(ex, "Cannot delete spare part with ID {PartId}", id);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error deleting spare part with ID {PartId}", id);
                return StatusCode(500, "Internal server error while deleting spare part");
            }
        }
    }
}

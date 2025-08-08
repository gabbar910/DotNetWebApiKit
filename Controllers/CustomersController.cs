namespace DotNetApiStarterKit.Controllers
{
    using DotNetApiStarterKit.Models;
    using DotNetApiStarterKit.Services;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;

    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CustomersController : ControllerBase
    {
        private readonly ICustomerService customerService;
        private readonly ILogger<CustomersController> logger;

        public CustomersController(ICustomerService customerService, ILogger<CustomersController> logger)
        {
            this.customerService = customerService;
            this.logger = logger;
        }

        /// <summary>
        /// Get all customers
        /// </summary>
        /// <returns>List of all customers</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Customer>>> GetCustomers()
        {
            try
            {
                var customers = await this.customerService.GetAllCustomersAsync();
                return this.Ok(customers);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error retrieving customers");
                return this.StatusCode(500, "An error occurred while retrieving customers");
            }
        }

        /// <summary>
        /// Get a specific customer by ID
        /// </summary>
        /// <param name="id">Customer ID</param>
        /// <returns>Customer details</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<Customer>> GetCustomer(int id)
        {
            try
            {
                var customer = await this.customerService.GetCustomerByIdAsync(id);

                if (customer == null)
                {
                    return this.NotFound($"Customer with ID {id} not found");
                }

                return this.Ok(customer);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error retrieving customer with ID {CustomerId}", id);
                return this.StatusCode(500, "An error occurred while retrieving the customer");
            }
        }

        /// <summary>
        /// Create a new customer
        /// </summary>
        /// <param name="customer">Customer data</param>
        /// <returns>Created customer with assigned ID</returns>
        [HttpPost]
        public async Task<ActionResult<Customer>> CreateCustomer([FromBody] Customer customer)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(customer.Name))
                {
                    return this.BadRequest("Customer name is required");
                }

                if (string.IsNullOrWhiteSpace(customer.Address))
                {
                    return this.BadRequest("Customer address is required");
                }

                var createdCustomer = await this.customerService.CreateCustomerAsync(customer);
                return this.CreatedAtAction(
                    nameof(this.GetCustomer),
                    new { id = createdCustomer.CustomerId },
                    createdCustomer);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error creating customer");
                return this.StatusCode(500, "An error occurred while creating the customer");
            }
        }

        /// <summary>
        /// Update an existing customer
        /// </summary>
        /// <param name="id">Customer ID</param>
        /// <param name="customer">Updated customer data</param>
        /// <returns>Updated customer</returns>
        [HttpPut("{id}")]
        public async Task<ActionResult<Customer>> UpdateCustomer(int id, [FromBody] Customer customer)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(customer.Name))
                {
                    return this.BadRequest("Customer name is required");
                }

                if (string.IsNullOrWhiteSpace(customer.Address))
                {
                    return this.BadRequest("Customer address is required");
                }

                var updatedCustomer = await this.customerService.UpdateCustomerAsync(id, customer);

                if (updatedCustomer == null)
                {
                    return this.NotFound($"Customer with ID {id} not found");
                }

                return this.Ok(updatedCustomer);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error updating customer with ID {CustomerId}", id);
                return this.StatusCode(500, "An error occurred while updating the customer");
            }
        }

        /// <summary>
        /// Delete a customer
        /// </summary>
        /// <param name="id">Customer ID</param>
        /// <returns>Success status</returns>
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteCustomer(int id)
        {
            try
            {
                var deleted = await this.customerService.DeleteCustomerAsync(id);

                if (!deleted)
                {
                    return this.NotFound($"Customer with ID {id} not found");
                }

                return this.NoContent();
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error deleting customer with ID {CustomerId}", id);
                return this.StatusCode(500, "An error occurred while deleting the customer");
            }
        }

        /// <summary>
        /// Get customers by state
        /// </summary>
        /// <param name="state">State name</param>
        /// <returns>List of customers in the specified state</returns>
        [HttpGet("state/{state}")]
        public async Task<ActionResult<IEnumerable<Customer>>> GetCustomersByState(string state)
        {
            try
            {
                var customers = await this.customerService.GetAllCustomersAsync();
                var filteredCustomers = customers.Where(c => c.State.Equals(state, StringComparison.OrdinalIgnoreCase));
                return this.Ok(filteredCustomers);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error retrieving customers for state {State}", state);
                return this.StatusCode(500, "An error occurred while retrieving customers by state");
            }
        }

        /// <summary>
        /// Get customers by city
        /// </summary>
        /// <param name="city">City name</param>
        /// <returns>List of customers in the specified city</returns>
        [HttpGet("city/{city}")]
        public async Task<ActionResult<IEnumerable<Customer>>> GetCustomersByCity(string city)
        {
            try
            {
                var customers = await this.customerService.GetAllCustomersAsync();
                var filteredCustomers = customers.Where(c => c.City.Equals(city, StringComparison.OrdinalIgnoreCase));
                return this.Ok(filteredCustomers);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error retrieving customers for city {City}", city);
                return this.StatusCode(500, "An error occurred while retrieving customers by city");
            }
        }

        /// <summary>
        /// Get available states
        /// </summary>
        /// <returns>List of unique states</returns>
        [HttpGet("states")]
        public async Task<ActionResult<IEnumerable<string>>> GetStates()
        {
            try
            {
                var customers = await this.customerService.GetAllCustomersAsync();
                var states = customers.Select(c => c.State).Distinct().OrderBy(s => s);
                return this.Ok(states);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error retrieving states");
                return this.StatusCode(500, "An error occurred while retrieving states");
            }
        }

        /// <summary>
        /// Get available cities
        /// </summary>
        /// <returns>List of unique cities</returns>
        [HttpGet("cities")]
        public async Task<ActionResult<IEnumerable<string>>> GetCities()
        {
            try
            {
                var customers = await this.customerService.GetAllCustomersAsync();
                var cities = customers.Select(c => c.City).Distinct().OrderBy(c => c);
                return this.Ok(cities);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error retrieving cities");
                return this.StatusCode(500, "An error occurred while retrieving cities");
            }
        }
    }
}

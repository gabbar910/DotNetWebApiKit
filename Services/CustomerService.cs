namespace DotNetApiStarterKit.Services
{
    using DotNetApiStarterKit.Data;
    using DotNetApiStarterKit.Models;
    using Microsoft.EntityFrameworkCore;

    public interface ICustomerService
    {
        Task<IEnumerable<Customer>> GetAllCustomersAsync();

        Task<Customer?> GetCustomerByIdAsync(int id);

        Task<Customer> CreateCustomerAsync(Customer customer);

        Task<Customer?> UpdateCustomerAsync(int id, Customer customer);

        Task<bool> DeleteCustomerAsync(int id);
    }

    public class CustomerService : ICustomerService
    {
        private readonly AppDbContext context;
        private readonly ILogger<CustomerService> logger;

        public CustomerService(AppDbContext context, ILogger<CustomerService> logger)
        {
            this.context = context;
            this.logger = logger;
        }

        public async Task<IEnumerable<Customer>> GetAllCustomersAsync()
        {
            try
            {
                var customers = await this.context.Customers
                    .OrderBy(c => c.CustomerId)
                    .ToListAsync();

                this.logger.LogInformation("Retrieved {Count} customers from database", customers.Count);
                return customers;
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error retrieving all customers from database");
                throw;
            }
        }

        public async Task<Customer?> GetCustomerByIdAsync(int id)
        {
            try
            {
                var customer = await this.context.Customers
                    .FirstOrDefaultAsync(c => c.CustomerId == id);

                if (customer != null)
                {
                    this.logger.LogInformation("Retrieved customer with ID {CustomerId}", id);
                }
                else
                {
                    this.logger.LogWarning("Customer with ID {CustomerId} not found", id);
                }

                return customer;
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error retrieving customer with ID {CustomerId}", id);
                throw;
            }
        }

        public async Task<Customer> CreateCustomerAsync(Customer customer)
        {
            try
            {
                // Validate required fields
                if (string.IsNullOrWhiteSpace(customer.Name))
                {
                    throw new ArgumentException("Customer name is required");
                }

                if (string.IsNullOrWhiteSpace(customer.Address))
                {
                    throw new ArgumentException("Customer address is required");
                }

                if (string.IsNullOrWhiteSpace(customer.Pincode))
                {
                    throw new ArgumentException("Customer pincode is required");
                }

                if (string.IsNullOrWhiteSpace(customer.State))
                {
                    throw new ArgumentException("Customer state is required");
                }

                if (string.IsNullOrWhiteSpace(customer.City))
                {
                    throw new ArgumentException("Customer city is required");
                }

                // Reset ID to let database generate it
                customer.CustomerId = 0;

                this.context.Customers.Add(customer);
                await this.context.SaveChangesAsync();

                this.logger.LogInformation("Created new customer with ID {CustomerId}", customer.CustomerId);
                return customer;
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error creating new customer");
                throw;
            }
        }

        public async Task<Customer?> UpdateCustomerAsync(int id, Customer customer)
        {
            try
            {
                var existingCustomer = await this.context.Customers
                    .FirstOrDefaultAsync(c => c.CustomerId == id);

                if (existingCustomer == null)
                {
                    this.logger.LogWarning("Customer with ID {CustomerId} not found for update", id);
                    return null;
                }

                // Validate required fields
                if (string.IsNullOrWhiteSpace(customer.Name))
                {
                    throw new ArgumentException("Customer name is required");
                }

                if (string.IsNullOrWhiteSpace(customer.Address))
                {
                    throw new ArgumentException("Customer address is required");
                }

                if (string.IsNullOrWhiteSpace(customer.Pincode))
                {
                    throw new ArgumentException("Customer pincode is required");
                }

                if (string.IsNullOrWhiteSpace(customer.State))
                {
                    throw new ArgumentException("Customer state is required");
                }

                if (string.IsNullOrWhiteSpace(customer.City))
                {
                    throw new ArgumentException("Customer city is required");
                }

                // Update properties
                existingCustomer.Name = customer.Name;
                existingCustomer.Address = customer.Address;
                existingCustomer.Pincode = customer.Pincode;
                existingCustomer.State = customer.State;
                existingCustomer.City = customer.City;

                await this.context.SaveChangesAsync();

                this.logger.LogInformation("Updated customer with ID {CustomerId}", id);
                return existingCustomer;
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error updating customer with ID {CustomerId}", id);
                throw;
            }
        }

        public async Task<bool> DeleteCustomerAsync(int id)
        {
            try
            {
                var customer = await this.context.Customers
                    .FirstOrDefaultAsync(c => c.CustomerId == id);

                if (customer == null)
                {
                    this.logger.LogWarning("Customer with ID {CustomerId} not found for deletion", id);
                    return false;
                }

                this.context.Customers.Remove(customer);
                await this.context.SaveChangesAsync();

                this.logger.LogInformation("Deleted customer with ID {CustomerId}", id);
                return true;
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error deleting customer with ID {CustomerId}", id);
                throw;
            }
        }
    }
}

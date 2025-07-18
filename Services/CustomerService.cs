namespace DotNetApiStarterKit.Services
{
    using System.Text.Json;
    using DotNetApiStarterKit.Models;

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
        private readonly string dataFilePath;
        private readonly ILogger<CustomerService> logger;
        private List<Customer>? cachedCustomers;

        public CustomerService(IWebHostEnvironment environment, ILogger<CustomerService> logger)
        {
            this.dataFilePath = Path.Combine(environment.ContentRootPath, "data", "customer.json");
            this.logger = logger;
        }

        public async Task<IEnumerable<Customer>> GetAllCustomersAsync()
        {
            var customers = await this.LoadCustomersAsync();
            return customers;
        }

        public async Task<Customer?> GetCustomerByIdAsync(int id)
        {
            var customers = await this.LoadCustomersAsync();
            return customers.FirstOrDefault(c => c.CustomerId == id);
        }

        public async Task<Customer> CreateCustomerAsync(Customer customer)
        {
            var customers = await this.LoadCustomersAsync();

            // Generate new ID
            var maxId = customers.Any() ? customers.Max(c => c.CustomerId) : 0;
            customer.CustomerId = maxId + 1;

            customers.Add(customer);
            await this.SaveCustomersAsync(customers);

            this.logger.LogInformation("Created new customer with ID {CustomerId}", customer.CustomerId);
            return customer;
        }

        public async Task<Customer?> UpdateCustomerAsync(int id, Customer customer)
        {
            var customers = await this.LoadCustomersAsync();
            var existingCustomer = customers.FirstOrDefault(c => c.CustomerId == id);

            if (existingCustomer == null)
            {
                return null;
            }

            // Update properties
            existingCustomer.Name = customer.Name;
            existingCustomer.Address = customer.Address;
            existingCustomer.Pincode = customer.Pincode;
            existingCustomer.State = customer.State;
            existingCustomer.City = customer.City;

            await this.SaveCustomersAsync(customers);

            this.logger.LogInformation("Updated customer with ID {CustomerId}", id);
            return existingCustomer;
        }

        public async Task<bool> DeleteCustomerAsync(int id)
        {
            var customers = await this.LoadCustomersAsync();
            var customerToDelete = customers.FirstOrDefault(c => c.CustomerId == id);

            if (customerToDelete == null)
            {
                return false;
            }

            customers.Remove(customerToDelete);
            await this.SaveCustomersAsync(customers);

            this.logger.LogInformation("Deleted customer with ID {CustomerId}", id);
            return true;
        }

        private async Task<List<Customer>> LoadCustomersAsync()
        {
            try
            {
                if (!File.Exists(this.dataFilePath))
                {
                    this.logger.LogError("Customer data file not found at: {FilePath}", this.dataFilePath);
                    return new List<Customer>();
                }

                var jsonContent = await File.ReadAllTextAsync(this.dataFilePath);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                };

                var customers = JsonSerializer.Deserialize<List<Customer>>(jsonContent, options) ?? new List<Customer>();
                this.logger.LogInformation("Loaded {Count} customers from data file", customers.Count);

                return customers;
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error loading customer data from file: {FilePath}", this.dataFilePath);
                return new List<Customer>();
            }
        }

        private async Task SaveCustomersAsync(List<Customer> customers)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                    WriteIndented = true,
                };

                var jsonContent = JsonSerializer.Serialize(customers, options);
                await File.WriteAllTextAsync(this.dataFilePath, jsonContent);
                this.cachedCustomers = customers;
                this.logger.LogInformation("Saved {Count} customers to data file", customers.Count);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error saving customer data to file: {FilePath}", this.dataFilePath);
                throw;
            }
        }
    }
}

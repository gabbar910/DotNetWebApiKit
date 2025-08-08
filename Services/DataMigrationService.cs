namespace DotNetApiStarterKit.Services
{
    using System.Text.Json;
    using DotNetApiStarterKit.Data;
    using DotNetApiStarterKit.Models;
    using Microsoft.EntityFrameworkCore;

    public interface IDataMigrationService
    {
        Task MigrateUsersFromJsonAsync();

        Task MigrateCustomersFromJsonAsync();

        Task MigrateOrdersFromJsonAsync();

        Task MigrateOrderDetailsFromJsonAsync();
    }

    public class DataMigrationService : IDataMigrationService
    {
        private readonly AppDbContext context;
        private readonly ILogger<DataMigrationService> logger;
        private readonly IWebHostEnvironment environment;

        public DataMigrationService(AppDbContext context, ILogger<DataMigrationService> logger, IWebHostEnvironment environment)
        {
            this.context = context;
            this.logger = logger;
            this.environment = environment;
        }

        public async Task MigrateUsersFromJsonAsync()
        {
            try
            {
                var jsonFilePath = Path.Combine(this.environment.ContentRootPath, "data", "usercreds.json");

                if (!File.Exists(jsonFilePath))
                {
                    this.logger.LogInformation("No JSON user credentials file found at {FilePath}. Skipping migration.", jsonFilePath);
                    return;
                }

                // Check if users already exist in database
                var existingUsersCount = await this.context.Users.CountAsync();
                if (existingUsersCount > 0)
                {
                    this.logger.LogInformation("Database already contains {Count} users. Skipping migration.", existingUsersCount);
                    return;
                }

                this.logger.LogInformation("Starting migration of users from JSON file to SQLite database...");

                var jsonContent = await File.ReadAllTextAsync(jsonFilePath);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                };

                var jsonUsers = JsonSerializer.Deserialize<List<JsonUserCredential>>(jsonContent, options);

                if (jsonUsers == null || !jsonUsers.Any())
                {
                    this.logger.LogWarning("No users found in JSON file or file is empty.");
                    return;
                }

                var migratedUsers = new List<UserCredential>();

                foreach (var jsonUser in jsonUsers)
                {
                    var user = new UserCredential
                    {
                        Username = jsonUser.Username,
                        PasswordHash = jsonUser.PasswordHash,
                        Email = jsonUser.Email,
                        CreatedAt = jsonUser.CreatedAt,
                        LastLoginAt = jsonUser.LastLoginAt,
                        IsActive = jsonUser.IsActive,
                    };

                    migratedUsers.Add(user);
                }

                this.context.Users.AddRange(migratedUsers);
                await this.context.SaveChangesAsync();

                this.logger.LogInformation("Successfully migrated {Count} users from JSON to SQLite database.", migratedUsers.Count);

                // Create backup of JSON file
                var backupPath = jsonFilePath + ".backup";
                File.Copy(jsonFilePath, backupPath, true);
                this.logger.LogInformation("Created backup of JSON file at {BackupPath}", backupPath);

                // Delete original JSON file
                File.Delete(jsonFilePath);
                this.logger.LogInformation("Deleted original JSON file at {FilePath}", jsonFilePath);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error during user data migration from JSON to SQLite");
                throw;
            }
        }

        public async Task MigrateCustomersFromJsonAsync()
        {
            try
            {
                var jsonFilePath = Path.Combine(this.environment.ContentRootPath, "data", "customer.json");

                if (!File.Exists(jsonFilePath))
                {
                    this.logger.LogInformation("No JSON customer file found at {FilePath}. Skipping migration.", jsonFilePath);
                    return;
                }

                // Check if customers already exist in database
                var existingCustomersCount = await this.context.Customers.CountAsync();
                if (existingCustomersCount > 0)
                {
                    this.logger.LogInformation("Database already contains {Count} customers. Skipping migration.", existingCustomersCount);
                    return;
                }

                this.logger.LogInformation("Starting migration of customers from JSON file to SQLite database...");

                var jsonContent = await File.ReadAllTextAsync(jsonFilePath);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                };

                var jsonCustomers = JsonSerializer.Deserialize<List<JsonCustomer>>(jsonContent, options);

                if (jsonCustomers == null || !jsonCustomers.Any())
                {
                    this.logger.LogWarning("No customers found in JSON file or file is empty.");
                    return;
                }

                var migratedCustomers = new List<Customer>();

                foreach (var jsonCustomer in jsonCustomers)
                {
                    var customer = new Customer
                    {
                        CustomerId = jsonCustomer.CustomerId,
                        Name = jsonCustomer.Name,
                        Address = jsonCustomer.Address,
                        Pincode = jsonCustomer.Pincode,
                        State = jsonCustomer.State,
                        City = jsonCustomer.City,
                    };

                    migratedCustomers.Add(customer);
                }

                this.context.Customers.AddRange(migratedCustomers);
                await this.context.SaveChangesAsync();

                this.logger.LogInformation("Successfully migrated {Count} customers from JSON to SQLite database.", migratedCustomers.Count);

                // Create backup of JSON file
                var backupPath = jsonFilePath + ".backup";
                File.Copy(jsonFilePath, backupPath, true);
                this.logger.LogInformation("Created backup of JSON file at {BackupPath}", backupPath);

                // Delete original JSON file
                File.Delete(jsonFilePath);
                this.logger.LogInformation("Deleted original JSON file at {FilePath}", jsonFilePath);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error during customer data migration from JSON to SQLite");
                throw;
            }
        }

        public async Task MigrateOrdersFromJsonAsync()
        {
            try
            {
                var jsonFilePath = Path.Combine(this.environment.ContentRootPath, "data", "orders.json");

                if (!File.Exists(jsonFilePath))
                {
                    this.logger.LogInformation("No JSON orders file found at {FilePath}. Skipping migration.", jsonFilePath);
                    return;
                }

                // Check if orders already exist in database
                var existingOrdersCount = await this.context.Orders.CountAsync();
                if (existingOrdersCount > 0)
                {
                    this.logger.LogInformation("Database already contains {Count} orders. Skipping migration.", existingOrdersCount);
                    return;
                }

                this.logger.LogInformation("Starting migration of orders from JSON file to SQLite database...");

                var jsonContent = await File.ReadAllTextAsync(jsonFilePath);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                };

                var jsonOrders = JsonSerializer.Deserialize<List<JsonOrder>>(jsonContent, options);

                if (jsonOrders == null || !jsonOrders.Any())
                {
                    this.logger.LogWarning("No orders found in JSON file or file is empty.");
                    return;
                }

                var migratedOrders = new List<Order>();

                foreach (var jsonOrder in jsonOrders)
                {
                    var order = new Order
                    {
                        OrderId = jsonOrder.OrderId,
                        CustomerId = jsonOrder.CustomerId,
                        OrderDate = DateTime.ParseExact(jsonOrder.OrderDate, "yyyy-MM-dd", null),
                        TotalAmount = 0, // Will be calculated after order items are migrated
                        OrderItems = new List<OrderItem>(), // Will be populated by order details migration
                    };

                    migratedOrders.Add(order);
                }

                this.context.Orders.AddRange(migratedOrders);
                await this.context.SaveChangesAsync();

                this.logger.LogInformation("Successfully migrated {Count} orders from JSON to SQLite database.", migratedOrders.Count);

                // Create backup of JSON file
                var backupPath = jsonFilePath + ".backup";
                File.Copy(jsonFilePath, backupPath, true);
                this.logger.LogInformation("Created backup of JSON file at {BackupPath}", backupPath);

                // Delete original JSON file
                File.Delete(jsonFilePath);
                this.logger.LogInformation("Deleted original JSON file at {FilePath}", jsonFilePath);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error during order data migration from JSON to SQLite");
                throw;
            }
        }

        public async Task MigrateOrderDetailsFromJsonAsync()
        {
            try
            {
                var jsonFilePath = Path.Combine(this.environment.ContentRootPath, "data", "orderdetails.json");

                if (!File.Exists(jsonFilePath))
                {
                    this.logger.LogInformation("No JSON order details file found at {FilePath}. Skipping migration.", jsonFilePath);
                    return;
                }

                // Check if order items already exist in database
                var existingOrderItemsCount = await this.context.OrderItems.CountAsync();
                if (existingOrderItemsCount > 0)
                {
                    this.logger.LogInformation("Database already contains {Count} order items. Skipping migration.", existingOrderItemsCount);
                    return;
                }

                this.logger.LogInformation("Starting migration of order details from JSON file to SQLite database...");

                var jsonContent = await File.ReadAllTextAsync(jsonFilePath);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                };

                var jsonOrderItems = JsonSerializer.Deserialize<List<JsonOrderItem>>(jsonContent, options);

                if (jsonOrderItems == null || !jsonOrderItems.Any())
                {
                    this.logger.LogWarning("No order details found in JSON file or file is empty.");
                    return;
                }

                var migratedOrderItems = new List<OrderItem>();

                foreach (var jsonOrderItem in jsonOrderItems)
                {
                    var orderItem = new OrderItem
                    {
                        OrderId = jsonOrderItem.OrderId,
                        PartId = jsonOrderItem.PartId,
                        Quantity = jsonOrderItem.Quantity,
                        Price = jsonOrderItem.Price,
                        TotalPrice = jsonOrderItem.TotalPrice,
                    };

                    migratedOrderItems.Add(orderItem);
                }

                this.context.OrderItems.AddRange(migratedOrderItems);
                await this.context.SaveChangesAsync();

                this.logger.LogInformation("Successfully migrated {Count} order items from JSON to SQLite database.", migratedOrderItems.Count);

                // Update order total amounts
                await this.UpdateOrderTotalAmountsAsync();

                // Create backup of JSON file
                var backupPath = jsonFilePath + ".backup";
                File.Copy(jsonFilePath, backupPath, true);
                this.logger.LogInformation("Created backup of JSON file at {BackupPath}", backupPath);

                // Delete original JSON file
                File.Delete(jsonFilePath);
                this.logger.LogInformation("Deleted original JSON file at {FilePath}", jsonFilePath);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error during order details data migration from JSON to SQLite");
                throw;
            }
        }

        private async Task UpdateOrderTotalAmountsAsync()
        {
            try
            {
                this.logger.LogInformation("Updating order total amounts...");

                var orders = await this.context.Orders.Include(o => o.OrderItems).ToListAsync();

                foreach (var order in orders)
                {
                    order.TotalAmount = order.OrderItems.Sum(item => item.TotalPrice);
                }

                await this.context.SaveChangesAsync();

                this.logger.LogInformation("Successfully updated total amounts for {Count} orders.", orders.Count);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error updating order total amounts");
                throw;
            }
        }

        // Helper classes for JSON deserialization
        private class JsonUserCredential
        {
            public int UserId { get; set; }

            public string Username { get; set; } = string.Empty;

            public string PasswordHash { get; set; } = string.Empty;

            public string Email { get; set; } = string.Empty;

            public DateTime CreatedAt { get; set; }

            public DateTime LastLoginAt { get; set; }

            public bool IsActive { get; set; } = true;
        }

        private class JsonCustomer
        {
            public int CustomerId { get; set; }

            public string Name { get; set; } = string.Empty;

            public string Address { get; set; } = string.Empty;

            public string Pincode { get; set; } = string.Empty;

            public string State { get; set; } = string.Empty;

            public string City { get; set; } = string.Empty;
        }

        private class JsonOrder
        {
            public int OrderId { get; set; }

            public int CustomerId { get; set; }

            public string OrderDate { get; set; } = string.Empty;

            public List<JsonOrderItem> OrderItems { get; set; } = new List<JsonOrderItem>();
        }

        private class JsonOrderItem
        {
            public int OrderId { get; set; }

            public int PartId { get; set; }

            public int Quantity { get; set; }

            public decimal Price { get; set; }

            public decimal TotalPrice { get; set; }
        }
    }
}

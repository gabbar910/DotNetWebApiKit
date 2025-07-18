using System.Text.Json;
using DotNetApiStarterKit.Models;

namespace DotNetApiStarterKit.Services
{
    public interface IOrderService
    {
        Task<Order> CreateOrderAsync(Order order);

        Task<IEnumerable<Order>> GetAllOrdersAsync();

        Task<Order?> GetOrderByIdAsync(int id);
    }

    public class OrderService : IOrderService
    {
        private readonly string ordersFilePath;
        private readonly string orderDetailsFilePath;
        private readonly ILogger<OrderService> logger;
        private readonly ICustomerService customerService;
        private readonly ISparePartsService sparePartsService;

        public OrderService(
            IWebHostEnvironment environment,
            ILogger<OrderService> logger,
            ICustomerService customerService,
            ISparePartsService sparePartsService)
        {
            this.ordersFilePath = Path.Combine(environment.ContentRootPath, "data", "orders.json");
            this.orderDetailsFilePath = Path.Combine(environment.ContentRootPath, "data", "orderdetails.json");
            this.logger = logger;
            this.customerService = customerService;
            this.sparePartsService = sparePartsService;
        }

        public async Task<Order> CreateOrderAsync(Order order)
        {
            // Validate customer exists
            var customer = await this.customerService.GetCustomerByIdAsync(order.CustomerId);
            if (customer == null)
            {
                throw new ArgumentException($"Customer with ID {order.CustomerId} not found");
            }

            // Validate date format
            if (!order.IsValidDate())
            {
                throw new ArgumentException($"Invalid date format: {order.OrderDate}. Expected format: YYYY-MM-DD");
            }

            // Validate all part IDs exist
            foreach (var item in order.OrderItems)
            {
                var sparePart = await this.sparePartsService.GetSparePartByIdAsync(item.PartId);
                if (sparePart == null)
                {
                    throw new ArgumentException($"Part with ID {item.PartId} not found");
                }

                // Optional: Check if sufficient stock is available
                if (sparePart.StockQuantity < item.Quantity)
                {
                    throw new InvalidOperationException($"Insufficient stock for part {item.PartId}. Available: {sparePart.StockQuantity}, Requested: {item.Quantity}");
                }
            }

            // Generate new order ID
            var existingOrders = await this.LoadOrdersAsync();
            var maxOrderId = existingOrders.Any() ? existingOrders.Max(o => o.OrderId) : 0;
            order.OrderId = maxOrderId + 1;

            // Set order ID for all order items
            foreach (var item in order.OrderItems)
            {
                item.OrderId = order.OrderId;
            }

            // Save order and order details atomically
            await this.SaveOrderAsync(order);

            this.logger.LogInformation("Created new order with ID {OrderId} for customer {CustomerId}", order.OrderId, order.CustomerId);
            return order;
        }

        public async Task<IEnumerable<Order>> GetAllOrdersAsync()
        {
            var orders = await this.LoadOrdersAsync();
            var orderDetails = await this.LoadOrderDetailsAsync();

            // Group order details by order ID and attach to orders
            var orderDetailsGrouped = orderDetails.GroupBy(od => od.OrderId).ToDictionary(g => g.Key, g => g.ToList());

            foreach (var order in orders)
            {
                if (orderDetailsGrouped.TryGetValue(order.OrderId, out var items))
                {
                    order.OrderItems = items;
                }
            }

            return orders;
        }

        public async Task<Order?> GetOrderByIdAsync(int id)
        {
            var orders = await this.LoadOrdersAsync();
            var order = orders.FirstOrDefault(o => o.OrderId == id);

            if (order != null)
            {
                var orderDetails = await this.LoadOrderDetailsAsync();
                order.OrderItems = orderDetails.Where(od => od.OrderId == id).ToList();
            }

            return order;
        }

        private async Task<List<Order>> LoadOrdersAsync()
        {
            try
            {
                if (!File.Exists(this.ordersFilePath))
                {
                    this.logger.LogWarning("Orders file not found at: {FilePath}. Creating empty list.", this.ordersFilePath);
                    return new List<Order>();
                }

                var jsonContent = await File.ReadAllTextAsync(this.ordersFilePath);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                };

                var orders = JsonSerializer.Deserialize<List<Order>>(jsonContent, options) ?? new List<Order>();
                this.logger.LogInformation("Loaded {Count} orders from data file", orders.Count);

                return orders;
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error loading orders data from file: {FilePath}", this.ordersFilePath);
                throw;
            }
        }

        private async Task<List<OrderItem>> LoadOrderDetailsAsync()
        {
            try
            {
                if (!File.Exists(this.orderDetailsFilePath))
                {
                    this.logger.LogWarning("Order details file not found at: {FilePath}. Creating empty list.", this.orderDetailsFilePath);
                    return new List<OrderItem>();
                }

                var jsonContent = await File.ReadAllTextAsync(this.orderDetailsFilePath);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                };

                var orderDetails = JsonSerializer.Deserialize<List<OrderItem>>(jsonContent, options) ?? new List<OrderItem>();
                this.logger.LogInformation("Loaded {Count} order details from data file", orderDetails.Count);

                return orderDetails;
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error loading order details data from file: {FilePath}", this.orderDetailsFilePath);
                throw;
            }
        }

        private async Task SaveOrderAsync(Order order)
        {
            const int maxRetries = 3;
            const int delayMs = 100;

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    // Load existing data
                    var existingOrders = await this.LoadOrdersAsync();
                    var existingOrderDetails = await this.LoadOrderDetailsAsync();

                    // Add new order
                    var orderToSave = new Order
                    {
                        OrderId = order.OrderId,
                        CustomerId = order.CustomerId,
                        OrderDate = order.OrderDate,
                    };
                    existingOrders.Add(orderToSave);

                    // Add new order details
                    existingOrderDetails.AddRange(order.OrderItems);

                    // Save both files atomically using temporary files
                    var tempOrdersFile = this.ordersFilePath + ".tmp";
                    var tempOrderDetailsFile = this.orderDetailsFilePath + ".tmp";

                    var options = new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                        WriteIndented = true,
                    };

                    // Write to temporary files
                    var ordersJson = JsonSerializer.Serialize(existingOrders, options);
                    var orderDetailsJson = JsonSerializer.Serialize(existingOrderDetails, options);

                    await File.WriteAllTextAsync(tempOrdersFile, ordersJson);
                    await File.WriteAllTextAsync(tempOrderDetailsFile, orderDetailsJson);

                    // Atomic move to final files
                    File.Move(tempOrdersFile, this.ordersFilePath, true);
                    File.Move(tempOrderDetailsFile, this.orderDetailsFilePath, true);

                    this.logger.LogInformation("Successfully saved order {OrderId} with {ItemCount} items", order.OrderId, order.OrderItems.Count);
                    return;
                }
                catch (IOException ex) when (attempt < maxRetries)
                {
                    this.logger.LogWarning(ex, "File operation failed on attempt {Attempt}/{MaxRetries}. Retrying in {DelayMs}ms", attempt, maxRetries, delayMs);
                    await Task.Delay(delayMs);
                }
                catch (Exception ex)
                {
                    this.logger.LogError(ex, "Error saving order data on attempt {Attempt}", attempt);

                    // Clean up temporary files if they exist
                    var tempOrdersFile = this.ordersFilePath + ".tmp";
                    var tempOrderDetailsFile = this.orderDetailsFilePath + ".tmp";

                    if (File.Exists(tempOrdersFile))
                    {
                        File.Delete(tempOrdersFile);
                    }

                    if (File.Exists(tempOrderDetailsFile))
                    {
                        File.Delete(tempOrderDetailsFile);
                    }

                    if (attempt == maxRetries)
                    {
                        throw;
                    }

                    await Task.Delay(delayMs);
                }
            }
        }
    }
}

using DotNetApiStarterKit.Data;
using DotNetApiStarterKit.Models;
using Microsoft.EntityFrameworkCore;

namespace DotNetApiStarterKit.Services
{
    public interface IOrderService
    {
        Task<Order> CreateOrderAsync(Order order);

        Task<IEnumerable<Order>> GetAllOrdersAsync();

        Task<Order?> GetOrderByIdAsync(int id);

        Task<Order?> UpdateOrderAsync(int id, Order order);

        Task<bool> DeleteOrderAsync(int id);
    }

    public class OrderService : IOrderService
    {
        private readonly AppDbContext context;
        private readonly ILogger<OrderService> logger;
        private readonly ICustomerService customerService;
        private readonly ISparePartsService sparePartsService;

        public OrderService(
            AppDbContext context,
            ILogger<OrderService> logger,
            ICustomerService customerService,
            ISparePartsService sparePartsService)
        {
            this.context = context;
            this.logger = logger;
            this.customerService = customerService;
            this.sparePartsService = sparePartsService;
        }

        public async Task<Order> CreateOrderAsync(Order order)
        {
            using var transaction = await this.context.Database.BeginTransactionAsync();
            try
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

                // Validate all part IDs exist and check stock
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

                // Generate new order ID if not provided
                if (order.OrderId == 0)
                {
                    var maxOrderId = await this.context.Orders.MaxAsync(o => (int?)o.OrderId) ?? 0;
                    order.OrderId = maxOrderId + 1;
                }

                // Calculate total amount
                order.TotalAmount = order.OrderItems.Sum(item => item.TotalPrice);

                // Set order ID for all order items
                foreach (var item in order.OrderItems)
                {
                    item.OrderId = order.OrderId;
                }

                // Add order to context
                this.context.Orders.Add(order);
                await this.context.SaveChangesAsync();

                await transaction.CommitAsync();

                this.logger.LogInformation("Created new order with ID {OrderId} for customer {CustomerId} with total amount {TotalAmount}", 
                    order.OrderId, order.CustomerId, order.TotalAmount);

                return order;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                this.logger.LogError(ex, "Error creating order for customer {CustomerId}", order.CustomerId);
                throw;
            }
        }

        public async Task<IEnumerable<Order>> GetAllOrdersAsync()
        {
            try
            {
                var orders = await this.context.Orders
                    .Include(o => o.Customer)
                    .Include(o => o.OrderItems)
                    .OrderByDescending(o => o.OrderDate)
                    .ToListAsync();

                this.logger.LogInformation("Retrieved {Count} orders from database", orders.Count);
                return orders;
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error retrieving all orders from database");
                throw;
            }
        }

        public async Task<Order?> GetOrderByIdAsync(int id)
        {
            try
            {
                var order = await this.context.Orders
                    .Include(o => o.Customer)
                    .Include(o => o.OrderItems)
                    .FirstOrDefaultAsync(o => o.OrderId == id);

                if (order != null)
                {
                    this.logger.LogInformation("Retrieved order {OrderId} from database", id);
                }
                else
                {
                    this.logger.LogWarning("Order with ID {OrderId} not found", id);
                }

                return order;
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error retrieving order {OrderId} from database", id);
                throw;
            }
        }

        public async Task<Order?> UpdateOrderAsync(int id, Order order)
        {
            using var transaction = await this.context.Database.BeginTransactionAsync();
            try
            {
                var existingOrder = await this.context.Orders
                    .Include(o => o.OrderItems)
                    .FirstOrDefaultAsync(o => o.OrderId == id);

                if (existingOrder == null)
                {
                    this.logger.LogWarning("Order with ID {OrderId} not found for update", id);
                    return null;
                }

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
                }

                // Update order properties
                existingOrder.CustomerId = order.CustomerId;
                existingOrder.OrderDate = order.OrderDate;

                // Remove existing order items
                this.context.OrderItems.RemoveRange(existingOrder.OrderItems);

                // Add new order items
                foreach (var item in order.OrderItems)
                {
                    item.OrderId = id;
                    existingOrder.OrderItems.Add(item);
                }

                // Calculate total amount
                existingOrder.TotalAmount = order.OrderItems.Sum(item => item.TotalPrice);

                await this.context.SaveChangesAsync();
                await transaction.CommitAsync();

                this.logger.LogInformation("Updated order {OrderId} with {ItemCount} items, total amount {TotalAmount}", 
                    id, order.OrderItems.Count, existingOrder.TotalAmount);

                return existingOrder;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                this.logger.LogError(ex, "Error updating order {OrderId}", id);
                throw;
            }
        }

        public async Task<bool> DeleteOrderAsync(int id)
        {
            using var transaction = await this.context.Database.BeginTransactionAsync();
            try
            {
                var order = await this.context.Orders
                    .Include(o => o.OrderItems)
                    .FirstOrDefaultAsync(o => o.OrderId == id);

                if (order == null)
                {
                    this.logger.LogWarning("Order with ID {OrderId} not found for deletion", id);
                    return false;
                }

                // Remove order items first (cascade delete should handle this, but being explicit)
                this.context.OrderItems.RemoveRange(order.OrderItems);
                
                // Remove order
                this.context.Orders.Remove(order);

                await this.context.SaveChangesAsync();
                await transaction.CommitAsync();

                this.logger.LogInformation("Deleted order {OrderId} and its {ItemCount} items", id, order.OrderItems.Count);
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                this.logger.LogError(ex, "Error deleting order {OrderId}", id);
                throw;
            }
        }
    }
}

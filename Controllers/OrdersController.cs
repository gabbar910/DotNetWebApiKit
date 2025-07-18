using System.ComponentModel.DataAnnotations;
using DotNetApiStarterKit.Models;
using DotNetApiStarterKit.Services;
using Microsoft.AspNetCore.Mvc;

namespace DotNetApiStarterKit.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService orderService;
        private readonly ILogger<OrdersController> logger;

        public OrdersController(IOrderService orderService, ILogger<OrdersController> logger)
        {
            this.orderService = orderService;
            this.logger = logger;
        }

        /// <summary>
        /// Creates a new order with order items
        /// </summary>
        /// <param name="order">Order details including customer ID, order date, and order items</param>
        /// <returns>The created order with generated order ID</returns>
        /// <response code="201">Order created successfully</response>
        /// <response code="400">Invalid request data or validation errors</response>
        /// <response code="404">Customer or part not found</response>
        /// <response code="409">Insufficient stock for requested items</response>
        /// <response code="500">Internal server error</response>
        [HttpPost]
        [ProducesResponseType(typeof(Order), 201)]
        [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
        [ProducesResponseType(typeof(ProblemDetails), 404)]
        [ProducesResponseType(typeof(ProblemDetails), 409)]
        [ProducesResponseType(typeof(ProblemDetails), 500)]
        public async Task<ActionResult<Order>> CreateOrder([FromBody] Order order)
        {
            try
            {
                this.logger.LogInformation(
                    "Creating new order for customer {CustomerId} with {ItemCount} items",
                    order.CustomerId,
                    order.OrderItems?.Count ?? 0);

                // Validate model state
                if (!this.ModelState.IsValid)
                {
                    this.logger.LogWarning(
                        "Invalid model state for order creation: {Errors}",
                        string.Join(", ", this.ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                    return this.BadRequest(this.ModelState);
                }

                // Additional validation for order items
                if (order.OrderItems == null || !order.OrderItems.Any())
                {
                    this.ModelState.AddModelError("OrderItems", "At least one order item is required");
                    return this.BadRequest(this.ModelState);
                }

                // Validate each order item
                for (int i = 0; i < order.OrderItems.Count; i++)
                {
                    var item = order.OrderItems[i];
                    if (item.PartId <= 0)
                    {
                        this.ModelState.AddModelError($"OrderItems[{i}].PartId", "Part ID must be a positive number");
                    }

                    if (item.Quantity <= 0)
                    {
                        this.ModelState.AddModelError($"OrderItems[{i}].Quantity", "Quantity must be a positive number");
                    }
                }

                if (!this.ModelState.IsValid)
                {
                    return this.BadRequest(this.ModelState);
                }

                var createdOrder = await this.orderService.CreateOrderAsync(order);

                this.logger.LogInformation(
                    "Successfully created order {OrderId} for customer {CustomerId}",
                    createdOrder.OrderId,
                    createdOrder.CustomerId);

                return this.CreatedAtAction(
                    nameof(this.GetOrderById),
                    new { id = createdOrder.OrderId },
                    createdOrder);
            }
            catch (ArgumentException ex)
            {
                this.logger.LogWarning(ex, "Validation error while creating order: {Message}", ex.Message);
                return this.BadRequest(new ProblemDetails
                {
                    Title = "Validation Error",
                    Detail = ex.Message,
                    Status = 400,
                });
            }
            catch (InvalidOperationException ex)
            {
                this.logger.LogWarning(ex, "Business logic error while creating order: {Message}", ex.Message);
                return this.Conflict(new ProblemDetails
                {
                    Title = "Business Logic Error",
                    Detail = ex.Message,
                    Status = 409,
                });
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Unexpected error while creating order for customer {CustomerId}", order.CustomerId);
                return this.StatusCode(500, new ProblemDetails
                {
                    Title = "Internal Server Error",
                    Detail = "An unexpected error occurred while processing your request",
                    Status = 500,
                });
            }
        }

        /// <summary>
        /// Gets all orders with their order items
        /// </summary>
        /// <returns>List of all orders</returns>
        /// <response code="200">Orders retrieved successfully</response>
        /// <response code="500">Internal server error</response>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<Order>), 200)]
        [ProducesResponseType(typeof(ProblemDetails), 500)]
        public async Task<ActionResult<IEnumerable<Order>>> GetAllOrders()
        {
            try
            {
                this.logger.LogInformation("Retrieving all orders");
                var orders = await this.orderService.GetAllOrdersAsync();

                this.logger.LogInformation("Retrieved {Count} orders", orders.Count());
                return this.Ok(orders);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error retrieving all orders");
                return this.StatusCode(500, new ProblemDetails
                {
                    Title = "Internal Server Error",
                    Detail = "An unexpected error occurred while retrieving orders",
                    Status = 500,
                });
            }
        }

        /// <summary>
        /// Gets a specific order by ID with its order items
        /// </summary>
        /// <param name="id">Order ID</param>
        /// <returns>Order details</returns>
        /// <response code="200">Order found and returned</response>
        /// <response code="404">Order not found</response>
        /// <response code="500">Internal server error</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(Order), 200)]
        [ProducesResponseType(typeof(ProblemDetails), 404)]
        [ProducesResponseType(typeof(ProblemDetails), 500)]
        public async Task<ActionResult<Order>> GetOrderById(int id)
        {
            try
            {
                this.logger.LogInformation("Retrieving order with ID {OrderId}", id);

                if (id <= 0)
                {
                    return this.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid Order ID",
                        Detail = "Order ID must be a positive number",
                        Status = 400,
                    });
                }

                var order = await this.orderService.GetOrderByIdAsync(id);

                if (order == null)
                {
                    this.logger.LogWarning("Order with ID {OrderId} not found", id);
                    return this.NotFound(new ProblemDetails
                    {
                        Title = "Order Not Found",
                        Detail = $"Order with ID {id} was not found",
                        Status = 404,
                    });
                }

                this.logger.LogInformation("Successfully retrieved order {OrderId}", id);
                return this.Ok(order);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error retrieving order with ID {OrderId}", id);
                return this.StatusCode(500, new ProblemDetails
                {
                    Title = "Internal Server Error",
                    Detail = "An unexpected error occurred while retrieving the order",
                    Status = 500,
                });
            }
        }
    }
}

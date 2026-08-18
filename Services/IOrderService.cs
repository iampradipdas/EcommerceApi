using EcommerceApi.Dal;
using EcommerceApi.Dal.Entities;
using EcommerceApi.DTOs.Orders;
using EcommerceApi.RabbitMQ;
using EcommerceApi.RabbitMQ.Interface;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace EcommerceApi.Services
{
    public interface IOrderService
    {
        Task<OrderResponseDto> PlaceOrderAsync(int userId, PlaceOrderDto dto);
        Task<IEnumerable<OrderResponseDto>> GetOrderHistoryAsync(int userId);
        Task<OrderResponseDto?> GetOrderDetailsAsync(int userId, int orderId);

        Task<bool> insertIntoPublishLog(string message_body, string queueName, string exchangeName = "");
    }

    public class OrderService : IOrderService
    {
        private readonly EcomDbContext _db;
        private readonly ICartService _cartService;
        private readonly ILogger<OrderService> _logger;

        private readonly IRabbitMqService _rabbitMqService;

        private readonly IMQueueProcessingService _mQueueProcessingService;

        public OrderService(EcomDbContext db, ICartService cartService, ILogger<OrderService> logger,
         IRabbitMqService rabbitMqService, IMQueueProcessingService mQueueProcessingService)
        {
            _db = db;
            _cartService = cartService;
            _logger = logger;
            _rabbitMqService = rabbitMqService;
            _mQueueProcessingService = mQueueProcessingService;
        }

        public async Task<OrderResponseDto> PlaceOrderAsync(int userId, PlaceOrderDto dto)
        {
            // 1. Get current cart items
            var cart = await _cartService.GetCartAsync(userId);
            if (!cart.Items.Any())
                throw new InvalidOperationException("Cannot place order with an empty cart.");

            // 2. Start Transaction
            // using var transaction = await _db.Database.BeginTransactionAsync();

            try
            {
                // Verify and update stock levels
                var orderItemsList = new List<OrderItem>();
                foreach (var cartItem in cart.Items)
                {
                    var product = await _db.Products.FindAsync(cartItem.ProductId);
                    if (product == null || !product.IsActive)
                        throw new InvalidOperationException($"Product '{cartItem.Name}' is no longer available.");

                    if (product.Stock < cartItem.Quantity)
                        throw new InvalidOperationException($"Insufficient stock for product '{cartItem.Name}'. Only {product.Stock} items are available in stock.");

                    // Deduct stock
                    product.Stock -= cartItem.Quantity;

                    // Create OrderItem
                    var orderItem = new OrderItem
                    {
                        ProductId = cartItem.ProductId,
                        Quantity = cartItem.Quantity,
                        UnitPrice = product.DiscountPrice ?? product.Price
                    };
                    orderItemsList.Add(orderItem);
                }

                // Create Order record
                var order = new Order
                {
                    UserId = userId,
                    TotalAmount = cart.TotalPrice,
                    ShippingAddress = dto.ShippingAddress.Trim(),
                    Status = "Pending",
                    OrderDate = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    OrderItems = orderItemsList
                };

                _db.Orders.Add(order);
                await _db.SaveChangesAsync(); // Saves order and gets OrderId (OrderItem.OrderId matches automatically due to navigation list)

                // Clear the user's shopping cart
                await _cartService.ClearCartAsync(userId);
                
                var random = new Random();

                var payload = new
                {
                    orderId = random.Next(100000, 999999),
                    userId = userId,
                    status = "PENDING"
                };

                string message_body = JsonSerializer.Serialize(payload);

                await this.insertIntoPublishLog(message_body, "test_queue");
                

                await _mQueueProcessingService.ProcessQueueAsync("test_queue");

                // Commit database transaction
                // await transaction.CommitAsync();

                return MapToDto(order);
            }
            catch (Exception)
            {
                // Rollback transaction on failure
                // await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<IEnumerable<OrderResponseDto>> GetOrderHistoryAsync(int userId)
        {
            var orders = await _db.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return orders.Select(o => MapToDto(o));
        }

        public async Task<OrderResponseDto?> GetOrderDetailsAsync(int userId, int orderId)
        {
            var order = await _db.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.UserId == userId && o.OrderId == orderId);

            return order == null ? null : MapToDto(order);
        }

        private static OrderResponseDto MapToDto(Order o) => new()
        {
            OrderId = o.OrderId,
            UserId = o.UserId,
            TotalAmount = o.TotalAmount,
            Status = o.Status,
            ShippingAddress = o.ShippingAddress,
            OrderDate = o.OrderDate,
            OrderItems = o.OrderItems.Select(oi => new OrderItemDto
            {
                OrderItemId = oi.OrderItemId,
                ProductId = oi.ProductId,
                ProductName = oi.Product?.Name ?? "Unknown Product",
                ProductImageUrl = oi.Product?.ImageUrl,
                Quantity = oi.Quantity,
                UnitPrice = oi.UnitPrice,
                Subtotal = oi.UnitPrice * oi.Quantity
            }).ToList()
        };

        public async Task<bool> insertIntoPublishLog(string message_body, string queueName, string exchangeName = "")
        {
            try
            {
                var PublishLog = new PublishLog
                {
                    Id = Guid.NewGuid(),
                    MessageBody = message_body,
                    QueueName = queueName,
                    ExchangeName = exchangeName,
                    Status = "PENDING",
                    CreatedAt = DateTime.Now
                };

                _db.PublishLogs.Add(PublishLog);
                await _db.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to insert message into PublishLog.");

                return false;
            }


        }
    }
}

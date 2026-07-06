using EcommerceApi.Dal;
using EcommerceApi.Dal.Entities;
using EcommerceApi.DTOs.Orders;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EcommerceApi.Services
{
    public interface IOrderService
    {
        Task<OrderResponseDto> PlaceOrderAsync(int userId, PlaceOrderDto dto);
        Task<IEnumerable<OrderResponseDto>> GetOrderHistoryAsync(int userId);
        Task<OrderResponseDto?> GetOrderDetailsAsync(int userId, int orderId);
    }

    public class OrderService : IOrderService
    {
        private readonly EcomDbContext _db;
        private readonly ICartService _cartService;

        public OrderService(EcomDbContext db, ICartService cartService)
        {
            _db = db;
            _cartService = cartService;
        }

        public async Task<OrderResponseDto> PlaceOrderAsync(int userId, PlaceOrderDto dto)
        {
            // 1. Get current cart items
            var cart = await _cartService.GetCartAsync(userId);
            if (!cart.Items.Any())
                throw new InvalidOperationException("Cannot place order with an empty cart.");

            // 2. Start Transaction
            using var transaction = await _db.Database.BeginTransactionAsync();

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

                // Commit database transaction
                await transaction.CommitAsync();

                return MapToDto(order);
            }
            catch (Exception)
            {
                // Rollback transaction on failure
                await transaction.RollbackAsync();
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
    }
}

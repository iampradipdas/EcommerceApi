using EcommerceApi.Dal;
using EcommerceApi.Dal.Entities;
using EcommerceApi.DTOs.Cart;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EcommerceApi.Services
{
    public interface ICartService
    {
        Task<CartDto> GetCartAsync(int userId);
        Task<CartDto> AddItemAsync(int userId, int productId, int quantity);
        Task<CartDto> UpdateQuantityAsync(int userId, int productId, int quantity);
        Task<CartDto> RemoveItemAsync(int userId, int productId);
        Task ClearCartAsync(int userId);
    }

    public class CartService : ICartService
    {
        private readonly EcomDbContext _db;

        public CartService(EcomDbContext db)
        {
            _db = db;
        }

        public async Task<CartDto> GetCartAsync(int userId)
        {
            var items = await _db.CartItems
                .Include(c => c.Product)
                .Where(c => c.UserId == userId)
                .ToListAsync();

            var mappedItems = items.Select(i => new CartItemDto
            {
                CartItemId = i.CartItemId,
                ProductId = i.ProductId,
                Name = i.Product.Name,
                ImageUrl = i.Product.ImageUrl,
                Price = i.Product.DiscountPrice ?? i.Product.Price,
                Quantity = i.Quantity,
                Stock = i.Product.Stock
            }).ToList();

            var totalItems = mappedItems.Sum(i => i.Quantity);
            var totalPrice = mappedItems.Sum(i => i.Price * i.Quantity);

            return new CartDto
            {
                Items = mappedItems,
                TotalItems = totalItems,
                TotalPrice = totalPrice
            };
        }

        public async Task<CartDto> AddItemAsync(int userId, int productId, int quantity)
        {
            var product = await _db.Products.FirstOrDefaultAsync(p => p.ProductId == productId && p.IsActive);
            if (product == null)
                throw new InvalidOperationException("Product not found or inactive.");

            var existingItem = await _db.CartItems
                .FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == productId);

            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
            }
            else
            {
                var cartItem = new CartItem
                {
                    UserId = userId,
                    ProductId = productId,
                    Quantity = quantity,
                    AddedAt = DateTime.Now
                };
                _db.CartItems.Add(cartItem);
            }

            await _db.SaveChangesAsync();
            return await GetCartAsync(userId);
        }

        public async Task<CartDto> UpdateQuantityAsync(int userId, int productId, int quantity)
        {
            var existingItem = await _db.CartItems
                .FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == productId);

            if (existingItem == null)
                throw new InvalidOperationException("Item not found in cart.");

            if (quantity <= 0)
            {
                _db.CartItems.Remove(existingItem);
            }
            else
            {
                existingItem.Quantity = quantity;
            }

            await _db.SaveChangesAsync();
            return await GetCartAsync(userId);
        }

        public async Task<CartDto> RemoveItemAsync(int userId, int productId)
        {
            var existingItem = await _db.CartItems
                .FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == productId);

            if (existingItem != null)
            {
                _db.CartItems.Remove(existingItem);
                await _db.SaveChangesAsync();
            }

            return await GetCartAsync(userId);
        }

        public async Task ClearCartAsync(int userId)
        {
            var items = await _db.CartItems.Where(c => c.UserId == userId).ToListAsync();
            if (items.Any())
            {
                _db.CartItems.RemoveRange(items);
                await _db.SaveChangesAsync();
            }
        }
    }
}

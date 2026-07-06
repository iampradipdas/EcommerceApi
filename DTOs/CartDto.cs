using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EcommerceApi.DTOs.Cart
{
    public class CartItemDto
    {
        public int CartItemId { get; set; }
        public int ProductId { get; set; }
        public string Name { get; set; } = null!;
        public string? ImageUrl { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public int Stock { get; set; } // useful for checkout checks
    }

    public class CartDto
    {
        public List<CartItemDto> Items { get; set; } = new();
        public int TotalItems { get; set; }
        public decimal TotalPrice { get; set; }
    }

    public class AddToCartDto
    {
        [Required]
        public int ProductId { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1.")]
        public int Quantity { get; set; }
    }

    public class UpdateCartQtyDto
    {
        [Required]
        public int ProductId { get; set; }

        [Required]
        [Range(0, int.MaxValue, ErrorMessage = "Quantity must be 0 or positive.")]
        public int Quantity { get; set; }
    }
}

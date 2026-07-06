using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EcommerceApi.DTOs.Orders
{
    public class PlaceOrderDto
    {
        [Required(ErrorMessage = "Shipping address is required.")]
        public string ShippingAddress { get; set; } = null!;
    }

    public class OrderItemDto
    {
        public int OrderItemId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = null!;
        public string? ProductImageUrl { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Subtotal { get; set; }
    }

    public class OrderResponseDto
    {
        public int OrderId { get; set; }
        public int UserId { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = null!;
        public string ShippingAddress { get; set; } = null!;
        public DateTime OrderDate { get; set; }
        public List<OrderItemDto> OrderItems { get; set; } = new();
    }
}

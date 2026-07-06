using EcommerceApi.Dal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace EcommerceApi.Controllers
{
    [Route("api/admin/orders")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminOrdersController : ControllerBase
    {
        private readonly EcomDbContext _db;

        public AdminOrdersController(EcomDbContext db)
        {
            _db = db;
        }

        // GET: api/admin/orders
        [HttpGet]
        public async Task<IActionResult> GetAllOrders()
        {
            var orders = await _db.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            var result = orders.Select(o => new
            {
                o.OrderId,
                o.TotalAmount,
                o.Status,
                o.ShippingAddress,
                o.OrderDate,
                UserEmail = o.User.Email,
                UserName = $"{o.User.FirstName} {o.User.LastName}",
                ItemCount = o.OrderItems.Sum(oi => oi.Quantity),
                Items = o.OrderItems.Select(oi => new
                {
                    oi.OrderItemId,
                    oi.ProductId,
                    ProductName = oi.Product?.Name ?? "Unknown Product",
                    oi.Quantity,
                    oi.UnitPrice,
                    Subtotal = oi.UnitPrice * oi.Quantity
                }).ToList()
            });

            return Ok(result);
        }

        // PATCH: api/admin/orders/{id}/status
        [HttpPatch("{id:int}/status")]
        public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] UpdateOrderStatusDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var order = await _db.Orders.FindAsync(id);
            if (order == null)
                return NotFound(new { message = $"Order with id {id} not found." });

            order.Status = dto.Status;
            order.UpdatedAt = DateTime.Now;

            await _db.SaveChangesAsync();
            return Ok(new { message = $"Order status updated to {dto.Status}.", status = dto.Status });
        }
    }

    public class UpdateOrderStatusDto
    {
        [Required]
        [RegularExpression("^(Pending|Processing|Shipped|Completed|Cancelled)$", ErrorMessage = "Invalid order status.")]
        public string Status { get; set; } = null!;
    }
}

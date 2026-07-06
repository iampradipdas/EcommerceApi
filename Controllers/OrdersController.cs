using EcommerceApi.DTOs.Orders;
using EcommerceApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace EcommerceApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public OrdersController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        // POST: api/orders
        [HttpPost]
        public async Task<IActionResult> PlaceOrder([FromBody] PlaceOrderDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var userId = GetUserId();
                var order = await _orderService.PlaceOrderAsync(userId, dto);
                return CreatedAtAction(nameof(GetOrderDetails), new { id = order.OrderId }, order);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while processing your order.", details = ex.Message });
            }
        }

        // GET: api/orders
        [HttpGet]
        public async Task<IActionResult> GetOrderHistory()
        {
            var userId = GetUserId();
            var orders = await _orderService.GetOrderHistoryAsync(userId);
            return Ok(orders);
        }

        // GET: api/orders/{id}
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetOrderDetails(int id)
        {
            var userId = GetUserId();
            var order = await _orderService.GetOrderDetailsAsync(userId, id);

            if (order == null)
                return NotFound(new { message = $"Order with id {id} not found." });

            return Ok(order);
        }

        private int GetUserId()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var userId))
            {
                throw new UnauthorizedAccessException("Invalid or missing user identity claims.");
            }
            return userId;
        }
    }
}

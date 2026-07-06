using EcommerceApi.DTOs.Cart;
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
    public class CartController : ControllerBase
    {
        private readonly ICartService _cartService;

        public CartController(ICartService cartService)
        {
            _cartService = cartService;
        }

        // GET: api/cart
        [HttpGet]
        public async Task<IActionResult> GetCart()
        {
            var userId = GetUserId();
            var cart = await _cartService.GetCartAsync(userId);
            return Ok(cart);
        }

        // POST: api/cart/add
        [HttpPost("add")]
        public async Task<IActionResult> AddItem([FromBody] AddToCartDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var userId = GetUserId();
                var cart = await _cartService.AddItemAsync(userId, dto.ProductId, dto.Quantity);
                return Ok(cart);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // PUT: api/cart/update
        [HttpPut("update")]
        public async Task<IActionResult> UpdateQuantity([FromBody] UpdateCartQtyDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var userId = GetUserId();
                var cart = await _cartService.UpdateQuantityAsync(userId, dto.ProductId, dto.Quantity);
                return Ok(cart);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // DELETE: api/cart/remove/{productId}
        [HttpDelete("remove/{productId:int}")]
        public async Task<IActionResult> RemoveItem(int productId)
        {
            var userId = GetUserId();
            var cart = await _cartService.RemoveItemAsync(userId, productId);
            return Ok(cart);
        }

        // DELETE: api/cart/clear
        [HttpDelete("clear")]
        public async Task<IActionResult> ClearCart()
        {
            var userId = GetUserId();
            await _cartService.ClearCartAsync(userId);
            return Ok(new { message = "Cart cleared successfully." });
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

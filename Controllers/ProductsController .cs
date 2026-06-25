using EcommerceApi.DTOs.Products;
using EcommerceApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EcommerceApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;

        public ProductsController(IProductService productService)
        {
            _productService = productService;
        }

   
        // Public — no auth needed
        // ─────────────────────────────────────────────────────────────────────────
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll([FromQuery] ProductQueryDto query)
        {
            var result = await _productService.GetAllAsync(query);
            return Ok(result);
        }

        // Public — no auth needed
        // ─────────────────────────────────────────────────────────────────────────
        [HttpGet("{id:int}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(int id)
        {
            var product = await _productService.GetByIdAsync(id);

            if (product == null)
                return NotFound(new { message = $"Product with id {id} not found." });

            return Ok(product);
        }

        // Admin only
        // ─────────────────────────────────────────────────────────────────────────
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] CreateProductDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var product = await _productService.CreateAsync(dto);

                // 201 Created with Location header pointing to the new resource
                return CreatedAtAction(
                    nameof(GetById),
                    new { id = product.ProductId },
                    product
                );
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // Admin only — partial update (only sent fields are updated)
        // ─────────────────────────────────────────────────────────────────────────
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateProductDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var product = await _productService.UpdateAsync(id, dto);

                if (product == null)
                    return NotFound(new { message = $"Product with id {id} not found." });

                return Ok(product);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }


        // Admin only — soft delete (IsActive = false)
        // ─────────────────────────────────────────────────────────────────────────
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _productService.DeleteAsync(id);

            if (!deleted)
                return NotFound(new { message = $"Product with id {id} not found." });

            return Ok(new { message = "Product deleted successfully." });
        }


        // Admin only — undo soft delete
        // ─────────────────────────────────────────────────────────────────────────
        [HttpPatch("{id:int}/restore")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Restore(int id)
        {
            var restored = await _productService.RestoreAsync(id);

            if (!restored)
                return NotFound(new { message = $"Product with id {id} not found." });

            return Ok(new { message = "Product restored successfully." });
        }
    }
}

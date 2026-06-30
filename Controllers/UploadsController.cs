using EcommerceApi.DTOs.Products;
using EcommerceApi.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EcommerceApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UploadsController : ControllerBase
    {
        private readonly IFileUploadService _uploadService;
        private readonly IProductService _productService;

        public UploadsController(
            IFileUploadService uploadService,
            IProductService productService)
        {
            _uploadService = uploadService;
            _productService = productService;
        }

        // ─────────────────────────────────────────────────────────────────────────
        // POST api/uploads/product-image
        // Upload a standalone image, returns the URL
        // ─────────────────────────────────────────────────────────────────────────
        [HttpPost("product-image")]
        public async Task<IActionResult> UploadProductImage(IFormFile file)
        {
            if (!_uploadService.IsValidFile(file, out var errorMessage))
                return BadRequest(new { message = errorMessage });

            var imageUrl = await _uploadService.UploadAsync(file, "products");

            return Ok(new { imageUrl });
            // Response: { "imageUrl": "/uploads/products/products/abc123.jpg" }
        }
        // ─────────────────────────────────────────────────────────────────────────
        // POST api/uploads/product-image/5
        // Upload image AND immediately update the product's ImageUrl in the DB
        // ─────────────────────────────────────────────────────────────────────────
        [HttpPost("product-image/{productId:int}")]
        public async Task<IActionResult> UploadAndAttach(int productId, IFormFile file)
        {
            // 1. Validate file
            if (!_uploadService.IsValidFile(file, out var errorMessage))
                return BadRequest(new { message = errorMessage });

            // 2. Check product exists
            var product = await _productService.GetByIdAsync(productId);
            if (product == null)
                return NotFound(new { message = $"Product {productId} not found." });

            // 3. Delete old image if one exists
            if (!string.IsNullOrWhiteSpace(product.ImageUrl))
                await _uploadService.DeleteAsync(product.ImageUrl);

            // 4. Upload new image
            var imageUrl = await _uploadService.UploadAsync(file, "products");

            // 5. Update product record in DB
            await _productService.UpdateAsync(productId, new UpdateProductDto
            {
                ImageUrl = imageUrl
            });

            return Ok(new { imageUrl, message = "Image uploaded and product updated." });
        }

        // ─────────────────────────────────────────────────────────────────────────
        // POST api/uploads/multiple
        // Upload up to 5 images at once — returns array of URLs
        // ─────────────────────────────────────────────────────────────────────────
        [HttpPost("multiple")]
        public async Task<IActionResult> UploadMultiple(List<IFormFile> files)
        {
            if (files == null || files.Count == 0)
                return BadRequest(new { message = "No files provided." });

            if (files.Count > 5)
                return BadRequest(new { message = "Maximum 5 files allowed per upload." });

            var uploadedUrls = new List<string>();
            var errors = new List<string>();

            foreach (var file in files)
            {
                if (!_uploadService.IsValidFile(file, out var errorMsg))
                {
                    errors.Add($"{file.FileName}: {errorMsg}");
                    continue;
                }

                var url = await _uploadService.UploadAsync(file, "products");
                uploadedUrls.Add(url);
            }

            return Ok(new
            {
                uploadedUrls,
                errors,
                uploadedCount = uploadedUrls.Count,
                failedCount = errors.Count
            });
        }

        // ─────────────────────────────────────────────────────────────────────────
        // DELETE api/uploads/product-image
        // Delete an image file from disk
        // ─────────────────────────────────────────────────────────────────────────
        [HttpDelete("product-image")]
        public async Task<IActionResult> DeleteImage([FromQuery] string imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
                return BadRequest(new { message = "imageUrl is required." });

            var deleted = await _uploadService.DeleteAsync(imageUrl);

            if (!deleted)
                return NotFound(new { message = "Image not found on server." });

            return Ok(new { message = "Image deleted successfully." });
        }
    }
}

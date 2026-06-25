using EcommerceApi.Dal;
using EcommerceApi.Dal.Entities;
using EcommerceApi.DTOs.Common;
using EcommerceApi.DTOs.Products;
using Microsoft.EntityFrameworkCore;

namespace EcommerceApi.Services
{
    public class ProductService : IProductService
    {
        private readonly EcomDbContext _db;

        public ProductService(EcomDbContext db)
        {
            _db = db;
        }

        public async Task<PaginatedResult<ProductResponseDto>> GetAllAsync(ProductQueryDto query)
        {
            // Clamp page size to prevent abuse
            query.PageSize = Math.Clamp(query.PageSize, 1, 50);

            var q = _db.Products
                       .Include(p => p.Category)
                       .Where(p => p.IsActive)  
                       .AsQueryable();

            // --- Filters ---
            if (!string.IsNullOrWhiteSpace(query.Search))
                q = q.Where(p => p.Name.ToLower().Contains(query.Search.ToLower()) ||
                                  (p.Description != null &&
                                   p.Description.ToLower().Contains(query.Search.ToLower())));

            if (query.CategoryId.HasValue)
                q = q.Where(p => p.CategoryId == query.CategoryId.Value);

            if (query.MinPrice.HasValue)
                q = q.Where(p => p.Price >= query.MinPrice.Value);

            if (query.MaxPrice.HasValue)
                q = q.Where(p => p.Price <= query.MaxPrice.Value);

            if (query.OnSale == true)
                q = q.Where(p => p.DiscountPrice != null && p.DiscountPrice < p.Price);

            // --- Sorting ---
            q = (query.SortBy.ToLower(), query.SortOrder.ToLower()) switch
            {
                ("name", "asc") => q.OrderBy(p => p.Name),
                ("name", "desc") => q.OrderByDescending(p => p.Name),
                ("price", "asc") => q.OrderBy(p => p.Price),
                ("price", "desc") => q.OrderByDescending(p => p.Price),
                ("createdat", "asc") => q.OrderBy(p => p.CreatedAt),
                _ => q.OrderByDescending(p => p.CreatedAt)  // default
            };

            var totalCount = await q.CountAsync();

            var items = await q
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(p => MapToDto(p))
                .ToListAsync();

            return new PaginatedResult<ProductResponseDto>
            {
                Data = items,
                TotalCount = totalCount,
                Page = query.Page,
                PageSize = query.PageSize
            };
        }

        // ─── GET BY ID ────────────────────────────────────────────────────────────
        public async Task<ProductResponseDto?> GetByIdAsync(int id)
        {
            var product = await _db.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.ProductId == id && p.IsActive);

            return product == null ? null : MapToDto(product);
        }

        // ─── CREATE ───────────────────────────────────────────────────────────────
        public async Task<ProductResponseDto> CreateAsync(CreateProductDto dto)
        {
            var categoryExists = await _db.Categories
                .AnyAsync(c => c.CategoryId == dto.CategoryId && c.IsActive);
            if (!categoryExists)
                throw new InvalidOperationException("Category not found.");

            // Validate discount < price
            if (dto.DiscountPrice.HasValue && dto.DiscountPrice >= dto.Price)
                throw new InvalidOperationException("Discount price must be less than the original price.");

            var product = new Product
            {
                Name = dto.Name.Trim(),
                Slug = GenerateSlug(dto.Name),
                Description = dto.Description?.Trim(),
                Price = dto.Price,
                DiscountPrice = dto.DiscountPrice,
                Stock = dto.Stock,
                ImageUrl = dto.ImageUrl,
                CategoryId = dto.CategoryId,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            _db.Products.Add(product);
            await _db.SaveChangesAsync();

            // Reload with category for the response
            await _db.Entry(product).Reference(p => p.Category).LoadAsync();

            return MapToDto(product);
        }

        // ─── UPDATE (partial — only updates fields that are sent) ─────────────────
        public async Task<ProductResponseDto?> UpdateAsync(int id, UpdateProductDto dto)
        {
            var product = await _db.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null) return null;

            // Only update fields that were actually provided
            if (dto.Name != null) { product.Name = dto.Name.Trim(); product.Slug = GenerateSlug(dto.Name); }
            if (dto.Description != null) product.Description = dto.Description.Trim();
            if (dto.Price != null) product.Price = dto.Price.Value;
            if (dto.DiscountPrice != null) product.DiscountPrice = dto.DiscountPrice;
            if (dto.Stock != null) product.Stock = dto.Stock.Value;
            if (dto.ImageUrl != null) product.ImageUrl = dto.ImageUrl;
            if (dto.CategoryId != null) product.CategoryId = dto.CategoryId.Value;
            if (dto.IsActive != null) product.IsActive = dto.IsActive.Value;

            // Re-validate discount after update
            if (product.DiscountPrice.HasValue && product.DiscountPrice >= product.Price)
                throw new InvalidOperationException("Discount price must be less than the original price.");

            await _db.SaveChangesAsync();

            // Reload category in case CategoryId changed
            await _db.Entry(product).Reference(p => p.Category).LoadAsync();

            return MapToDto(product);
        }

        // ─── SOFT DELETE ──────────────────────────────────────────────────────────
        public async Task<bool> DeleteAsync(int id)
        {
            var product = await _db.Products.FindAsync(id);
            if (product == null) return false;

            product.IsActive = false;
            await _db.SaveChangesAsync();
            return true;
        }

        // ─── RESTORE (undo soft delete) ───────────────────────────────────────────
        public async Task<bool> RestoreAsync(int id)
        {
            var product = await _db.Products.FindAsync(id);
            if (product == null) return false;

            product.IsActive = true;
            await _db.SaveChangesAsync();
            return true;
        }

        // ─── HELPERS ──────────────────────────────────────────────────────────────
        private static ProductResponseDto MapToDto(Product p) => new()
        {
            ProductId = p.ProductId,
            Name = p.Name,
            Slug = p.Slug,
            Description = p.Description,
            Price = p.Price,
            DiscountPrice = p.DiscountPrice,
            Stock = p.Stock,
            ImageUrl = p.ImageUrl,
            IsActive = p.IsActive,
            CreatedAt = p.CreatedAt,
            CategoryName = p.Category?.Name ?? ""
        };

        private static string GenerateSlug(string name) =>
            name.ToLower()
                .Trim()
                .Replace(" ", "-")
                .Replace("&", "and")
                .Replace("'", "")
                .Replace(",", "");
    }
}

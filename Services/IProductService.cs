using EcommerceApi.DTOs.Common;
using EcommerceApi.DTOs.Products;

namespace EcommerceApi.Services
{
    public interface IProductService
    {
        Task<PaginatedResult<ProductResponseDto>> GetAllAsync(ProductQueryDto query);
        Task<ProductResponseDto?> GetByIdAsync(int id);
        Task<ProductResponseDto> CreateAsync(CreateProductDto dto);
        Task<ProductResponseDto?> UpdateAsync(int id, UpdateProductDto dto);
        Task<bool> DeleteAsync(int id);
        Task<bool> RestoreAsync(int id);
    }
}

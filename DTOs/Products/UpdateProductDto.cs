using System.ComponentModel.DataAnnotations;

namespace EcommerceApi.DTOs.Products
{
    public class UpdateProductDto
    {
        [MaxLength(200)]
        public string? Name { get; set; }

        public string? Description { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal? Price { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal? DiscountPrice { get; set; }

        [Range(0, int.MaxValue)]
        public int? Stock { get; set; }

        public string? ImageUrl { get; set; }

        public int? CategoryId { get; set; }

        public bool? IsActive { get; set; }
    }
}

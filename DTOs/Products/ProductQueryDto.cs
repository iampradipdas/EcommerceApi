namespace EcommerceApi.DTOs.Products
{
    public class ProductQueryDto
    {
        public string? Search { get; set; }
        public int? CategoryId { get; set; }        // filter by category
        public decimal? MinPrice { get; set; }        // price range filter
        public decimal? MaxPrice { get; set; }
        public bool? OnSale { get; set; }        // only discounted items
        public string SortBy { get; set; } = "createdAt";  // name | price | createdAt
        public string SortOrder { get; set; } = "desc"; 
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}

namespace EcommerceApi.DTOs.Products
{
    public class ProductResponseDto
{
    public int     ProductId     { get; set; }
    public string  Name          { get; set; } = null!;
    public string  Slug          { get; set; } = null!;
    public string? Description   { get; set; }
    public decimal Price         { get; set; }
    public decimal? DiscountPrice { get; set; }
    public int     Stock         { get; set; }
    public string? ImageUrl      { get; set; }
    public bool    IsActive      { get; set; }
    public DateTime CreatedAt   { get; set; }
    public string  CategoryName  { get; set; } = null!;  // joined from Category table
    public decimal FinalPrice    => DiscountPrice ?? Price;
    public bool    IsOnSale      => DiscountPrice.HasValue && DiscountPrice < Price;
    public double  DiscountPercent =>
        IsOnSale ? Math.Round((double)(Price - DiscountPrice!.Value) / (double)Price * 100) : 0;
}
}

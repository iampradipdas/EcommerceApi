namespace EcommerceApi.Services
{
    public interface IFileUploadService
    {
        Task<string> UploadAsync(IFormFile file, string subfolder = "products");
        Task<bool> DeleteAsync(string imageUrl);
        bool IsValidFile(IFormFile file, out string errorMessage);
    }
}

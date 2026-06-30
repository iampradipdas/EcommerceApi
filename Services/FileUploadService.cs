namespace EcommerceApi.Services
{
    public class FileUploadService : IFileUploadService
    {
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _config;

        private readonly string[] _allowedExtensions;
        private readonly long _maxFileSizeBytes;
        private readonly string _uploadFolder;

        public FileUploadService(IWebHostEnvironment env, IConfiguration config)
        {
            _env = env;
            _config = config;

            _allowedExtensions = _config
                .GetSection("FileUpload:AllowedExtensions")
                .Get<string[]>()!;

            var maxMb = _config.GetValue<int>("FileUpload:MaxFileSizeMB");
            _maxFileSizeBytes = maxMb * 1024 * 1024;           // 5 MB → 5242880 bytes

            _uploadFolder = _config["FileUpload:UploadFolder"]!;  // uploads/products
        }

        // ─── UPLOAD ──────────────────────────────────────────────────────────────
        public async Task<string> UploadAsync(IFormFile file, string subfolder = "products")
        {
            // 1. Build the physical folder path inside wwwroot
            var folderPath = Path.Combine(_env.WebRootPath, _uploadFolder, subfolder);

            // 2. Create folder if it doesn't exist
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            // 3. Generate a unique filename — prevents overwriting existing files
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var uniqueName = $"{Guid.NewGuid()}{extension}";          // e.g. a3f2c1d4-....jpg
            var physicalPath = Path.Combine(folderPath, uniqueName);

            // 4. Save the file to disk
            using (var stream = new FileStream(physicalPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // 5. Return the relative URL (what Angular will use in <img src="...">)
            return $"/{_uploadFolder}/{subfolder}/{uniqueName}";
            // e.g. /uploads/products/products/a3f2c1d4-....jpg
        }

        // ─── DELETE ──────────────────────────────────────────────────────────────
        public async Task<bool> DeleteAsync(string imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl)) return false;

            // Convert URL back to physical path
            // imageUrl = /uploads/products/products/abc.jpg
            var relativePath = imageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var physicalPath = Path.Combine(_env.WebRootPath, relativePath);

            if (!File.Exists(physicalPath)) return false;

            File.Delete(physicalPath);
            return await Task.FromResult(true);
        }

        // ─── VALIDATION ──────────────────────────────────────────────────────────
        public bool IsValidFile(IFormFile file, out string errorMessage)
        {
            errorMessage = string.Empty;

            // 1. Check file is not empty
            if (file == null || file.Length == 0)
            {
                errorMessage = "No file provided.";
                return false;
            }

            // 2. Check file size
            if (file.Length > _maxFileSizeBytes)
            {
                errorMessage = $"File size exceeds {_config["FileUpload:MaxFileSizeMB"]} MB limit.";
                return false;
            }

            // 3. Check file extension
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!_allowedExtensions.Contains(extension))
            {
                errorMessage = $"File type '{extension}' is not allowed. " +
                               $"Allowed: {string.Join(", ", _allowedExtensions)}";
                return false;
            }

            // 4. Check file signature (magic bytes) — prevents fake extensions
            //    e.g. someone renames a .exe to .jpg
            if (!HasValidMagicBytes(file))
            {
                errorMessage = "File content does not match the expected image format.";
                return false;
            }

            return true;
        }

        // ─── MAGIC BYTES CHECK ───────────────────────────────────────────────────
        // Reads the first few bytes of the file to verify it's really an image
        private static bool HasValidMagicBytes(IFormFile file)
        {
            using var reader = new BinaryReader(file.OpenReadStream());
            var bytes = reader.ReadBytes(4);

            // JPEG: FF D8 FF
            if (bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF)
                return true;

            // PNG: 89 50 4E 47
            if (bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47)
                return true;

            // WEBP: 52 49 46 46 (RIFF header — need more bytes to confirm WEBP)
            if (bytes[0] == 0x52 && bytes[1] == 0x49 && bytes[2] == 0x46 && bytes[3] == 0x46)
                return true;

            return false;
        }
    }
}

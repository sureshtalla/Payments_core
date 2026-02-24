using Microsoft.AspNetCore.Hosting;

namespace Payments_core.Services.FileStorage
{
    public class LocalFileService : ILocalFileService
    {
        private readonly IWebHostEnvironment _env;

        public LocalFileService(IWebHostEnvironment env)
        {
            _env = env;
        }

        public async Task<string> SaveKycFileAsync(IFormFile file, long userId, string docType)
        {
            if (file == null || file.Length == 0)
                return null;

            // Validate extension
            var allowed = new[] { ".pdf", ".jpg", ".jpeg", ".png" };
            var extension = Path.GetExtension(file.FileName).ToLower();

            if (!allowed.Contains(extension))
                throw new Exception("Invalid file type");

            // 10MB validation
            if (file.Length > 10 * 1024 * 1024)
                throw new Exception("File size exceeds 10MB");

            // Create secure folder path
            string basePath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "SecureStorage",
                "kyc",
                userId.ToString()
            );

            if (!Directory.Exists(basePath))
                Directory.CreateDirectory(basePath);

            string fileName = $"{docType}_{DateTime.UtcNow:yyyyMMddHHmmss}{extension}";
            string fullPath = Path.Combine(basePath, fileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Return relative path for DB
            return $"kyc/{userId}/{fileName}";
        }
    }
}
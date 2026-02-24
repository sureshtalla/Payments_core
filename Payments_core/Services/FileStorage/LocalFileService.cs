using Microsoft.AspNetCore.Hosting;
using Payments_core.Services.FileStorage;

public class LocalFileService : ILocalFileService
{
    private readonly IWebHostEnvironment _env;

    public LocalFileService(IWebHostEnvironment env)
    {
        _env = env;
    }

    public async Task<string> SaveFileAsync(IFormFile file, long userId, string docType)
    {
        if (file == null || file.Length == 0)
            return null;

        string uploadsFolder = Path.Combine(
            _env.WebRootPath,
            "uploads",
            "kyc",
            userId.ToString()
        );

        if (!Directory.Exists(uploadsFolder))
            Directory.CreateDirectory(uploadsFolder);

        string extension = Path.GetExtension(file.FileName);
        string fileName = $"{docType}_{DateTime.UtcNow:yyyyMMddHHmmss}{extension}";
        string fullPath = Path.Combine(uploadsFolder, fileName);

        using (var stream = new FileStream(fullPath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        // return relative URL to store in DB
        return $"uploads/kyc/{userId}/{fileName}";
    }
}
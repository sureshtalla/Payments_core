namespace Payments_core.Helpers
{
    public static class FileUploadHelper
    {
        public static async Task<string?> SaveFileAsync(IFormFile file, string rootFolder)
        {
            if (file == null) return null;
            Directory.CreateDirectory(rootFolder);
            var ext = Path.GetExtension(file.FileName);
            var fileName = $"{Guid.NewGuid()}{ext}";
            var path = Path.Combine(rootFolder, fileName);
            using (var fs = new FileStream(path, FileMode.Create))
            {
                await file.CopyToAsync(fs);
            }
            // Return a relative or public URL in production
            return path;
        }
    }
}

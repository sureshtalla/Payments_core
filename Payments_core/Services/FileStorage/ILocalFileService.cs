namespace Payments_core.Services.FileStorage
{
    public interface ILocalFileService
    {
        Task<string> SaveFileAsync(IFormFile file, long userId, string docType);
    }
}

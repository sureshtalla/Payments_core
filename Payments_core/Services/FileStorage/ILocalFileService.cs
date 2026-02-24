using Microsoft.AspNetCore.Http;

namespace Payments_core.Services.FileStorage
{
    public interface ILocalFileService
    {
        Task<string> SaveKycFileAsync(IFormFile file, long userId, string docType);
    }
}
using Payments_core.Models;

namespace Payments_core.Services.SuperDistributorService
{
    public interface ISuperDistributorService
    {

        Task<(long userId, long merchantId)>CreateFullOnboardingAsync(SuperDistributorRequest req,string panUrl,string aadhaarUrl);

        Task<SuperDistributorFullResponse> GetFullByUserIdAsync(long userId);

        Task<IEnumerable<SuperDistributorCardDto>> GetCardsAsync(int roleId, long userId);
        Task<uperDistributorProfileDto?> GetCardDetailedInfo( int roleId, long userId);

        Task<bool> GetKYCStatus(long userId);

      
        Task<List<GetFilesinfo>> GetFiles(long userId);
    }
}

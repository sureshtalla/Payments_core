using Payments_core.Models;

namespace Payments_core.Services.SuperDistributorService
{
    public interface ISuperDistributorService
    {
        
            Task<(long userId, long merchantId)> CreateFullOnboardingAsync(SuperDistributorRequest req);

            Task<SuperDistributorFullResponse> GetFullByUserIdAsync(long userId);

        Task<IEnumerable<SuperDistributorCardDto>> GetCardsAsync(int roleId);
        Task<uperDistributorProfileDto?> GetCardDetailedInfo( int roleId, long userId);

        Task<bool> GetKYCStatus(long userId);
    }
}

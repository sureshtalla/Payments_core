using Payments_core.Models;

namespace Payments_core.Services.SuperDistributorService
{
    public interface ISuperDistributorService
    {
        
            Task<(long userId, long merchantId)> CreateFullOnboardingAsync(SuperDistributorRequest req);

            Task<SuperDistributorFullResponse> GetFullByUserIdAsync(long userId);
    }
}

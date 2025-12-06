using Payments_core.Models;

namespace Payments_core.Services.MerchantDataService
{
    public interface IMerchantDataService
    {
        Task<IEnumerable<MerchantListItemDto>> GetAllMerchantsAsync();
        Task<int> UpdateMerchantApprovalAsync(MerchantApprovalRequest req);
        Task<int> UpdateMerchantKycStatusAsync(MerchantKycUpdateRequest req);

    }
}

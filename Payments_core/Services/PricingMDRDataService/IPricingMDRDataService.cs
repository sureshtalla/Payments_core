using Payments_core.Models;

namespace Payments_core.Services.PricingMDRDataService
{
    public interface IPricingMDRDataService
    {
        Task<IEnumerable<MdrPricingDto>> GetMdrPricing(string? category, int? providerId);
        Task<MdrPricingDto?> InsertMdrPricing(MdrPricingCreateRequest request);
        Task<MdrPricingDto?> UpdateMdrPricing(MdrPricingUpdateRequest request);
    }
}

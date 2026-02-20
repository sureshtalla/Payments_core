using Microsoft.AspNetCore.Mvc;
using Payments_core.Models;
using System.Diagnostics.Metrics;
using System.Globalization;

namespace Payments_core.Services.MasterDataService
{
    public interface IMasterDataService
    {

        Task<IEnumerable<Roles>> GetAllRoles();
        Task<IEnumerable<ProviderDto>> GetProvidersAsync();
        Task<IEnumerable<ProviderDto>> GetProvidersList();
        Task<IEnumerable<MdrPricing>> GetMdrPricingAsync();
        Task<IEnumerable<Biller>> GetBillerAsync(string Category);

        Task<IEnumerable<BusineessRoles>> RolebasedBusineessName(int RoleId);
        Task<IEnumerable<PaymentMode>> GetPaymentModes();
        Task<IEnumerable<ProductCategory>> GetProductCategories();
        Task<int> AddOrUpdateProvider(Provider request);

        Task<IEnumerable<BusineessRoles>> RolebasedUserWise(int RoleId,int UserId);

        Task<RetailerFeatureItem> GetGlobal();
        Task<RetailerFeatureItem> GetUser(long userId);
        Task<int> UpdateGlobal(RetailerFeatureItem model, long adminId);
        Task<int> UpdateIndividual(long userId, RetailerFeatureItem model, long adminId);
        Task<int> UpdateMultipleIndividuals(BulkRetailerFeatureUpdateRequest request);
    }
}

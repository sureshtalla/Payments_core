using Dapper;
using Newtonsoft.Json;
using Payments_core.Models;
using Payments_core.Services.DataLayer;
using System.Data;
using System.Diagnostics.Metrics;

namespace Payments_core.Services.MasterDataService
{
    public class MasterDataService : IMasterDataService
    {
        IDapperContext dbContext;
        public MasterDataService(IDapperContext _dbContext)
        {
            dbContext = _dbContext;
        }

        public async Task<IEnumerable<Roles>> GetAllRoles()
        {
            return await dbContext.GetData<Roles>("sp_roles_get_all", null);
        }

        public async Task<IEnumerable<ProviderDto>> GetProvidersAsync()
        {
            return await dbContext.GetData<ProviderDto>("sp_master_get_providers", null);
        }

        public async Task<IEnumerable<ProviderDto>> GetProvidersList()
        {
            return await dbContext.GetData<ProviderDto>("sp_master_get_providers_list", null);
        }

        public async Task<IEnumerable<MdrPricing>> GetMdrPricingAsync()
        {
            return await dbContext.GetData<MdrPricing>("sp_master_get_mdr_pricing", null);
        }

        public async Task<IEnumerable<Biller>> GetBillerAsync(string Category)
        {
            DynamicParameters parameters = new DynamicParameters();
            parameters.Add("p_category", Category);
            return await dbContext.GetData<Biller>("sp_master_get_billers", parameters);
        }

        public async Task<IEnumerable<BusineessRoles>> RolebasedBusineessName(int RoleId)
        {
            DynamicParameters parameters = new DynamicParameters();
            parameters.Add("p_role_id", RoleId);
            parameters.Add("p_user_id", null);
            return await dbContext.GetData<BusineessRoles>("sp_GetBusinessNamesByRole", parameters);
        }

        public async Task<IEnumerable<PaymentMode>> GetPaymentModes()
        {
            return await dbContext.GetData<PaymentMode>("GetPaymentModes", null);
        }

        public async Task<IEnumerable<ProductCategory>> GetProductCategories()
        {
            return await dbContext.GetData<ProductCategory>("GetProductCategories", null);
        }

        public async Task<int> AddOrUpdateProvider(Provider request)
        {
            var param = new DynamicParameters();
            param.Add("P_Id", request.Id);
            param.Add("p_Code", request.ProviderCode);
            param.Add("p_Name", request.ProviderName);
            param.Add("p_ShotName", request.ShotName);
            param.Add("p_Product", request.Product_Id);
            param.Add("p_Type", request.ProviderType);
            param.Add("p_Status", request.IsActive);

            return await dbContext.SetData("AddOrUpdateProvider", param);
        }

        public async Task<IEnumerable<BusineessRoles>> RolebasedUserWise(int RoleId, int UserId)
        {
            DynamicParameters parameters = new DynamicParameters();
            parameters.Add("p_role_id", RoleId);
            parameters.Add("p_user_id", UserId);
            return await dbContext.GetData<BusineessRoles>("sp_GetBusinessNamesByRole", parameters);
        }

        public async Task<RetailerFeatureItem> GetGlobal()
        {
            return (await dbContext.GetData<RetailerFeatureItem>(
                "sp_get_global_retailer_features",
                null)).FirstOrDefault();
        }

        public async Task<RetailerFeatureItem> GetUser(long userId)
        {
            var param = new DynamicParameters();
            param.Add("p_user_id", userId);

            return (await dbContext.GetData<RetailerFeatureItem>(
                "sp_get_retailer_features",
                param)).FirstOrDefault();
        }

        public async Task<int> UpdateGlobal(RetailerFeatureItem model, long adminId)
        {
            var param = new DynamicParameters();
            param.Add("p_payin", model.Payin);
            param.Add("p_payout", model.Payout);
            param.Add("p_wallet", model.Wallet);
            param.Add("p_credit_card", model.CreditCard);
            param.Add("p_admin_id", adminId);

            return await dbContext.SetData(
                "sp_update_global_retailer_features",
                param);
        }

        public async Task<int> UpdateMultipleIndividuals(BulkRetailerFeatureUpdateRequest request)
        {
            var json = JsonConvert.SerializeObject(request.Retailers);

            var param = new DynamicParameters();
            param.Add("p_json", json);
            param.Add("p_admin_id", request.AdminId);

            return await dbContext.SetData(
                "sp_update_retailer_individual_bulk",
                param);
        }

        public async Task<int> UpdateIndividual(long userId, RetailerFeatureItem model, long adminId)
        {
            var param = new DynamicParameters();
            param.Add("p_user_id", userId);
            param.Add("p_payin", model.Payin);
            param.Add("p_payout", model.Payout);
            param.Add("p_wallet", model.Wallet);
            param.Add("p_credit_card", model.CreditCard);
            param.Add("p_admin_id", adminId);

            return await dbContext.SetData(
                "sp_update_retailer_individual_features",
                param);
        }
      }
    }

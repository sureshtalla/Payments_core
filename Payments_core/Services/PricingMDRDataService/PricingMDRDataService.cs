using Dapper;
using Payments_core.Models;
using Payments_core.Services.DataLayer;
using System.Data;
using System.Reflection.Metadata;
using System.Security.Cryptography;

namespace Payments_core.Services.PricingMDRDataService
{
    public class PricingMDRDataService:IPricingMDRDataService
    {
        private readonly IDapperContext _dbContext;

        public PricingMDRDataService(IDapperContext dbContext)
        {
            _dbContext = dbContext;
        }
        public async Task<IEnumerable<MdrPricingDto>> GetMdrPricing(string? category, int? providerId)
        {
            var p = new DynamicParameters();
            p.Add("p_category", category);
            p.Add("p_provider_id", providerId);

            return await _dbContext.GetData<MdrPricingDto>("sp_get_mdr_pricing", p);
        }

        public async Task<MdrPricingDto?> InsertMdrPricing(MdrPricingCreateRequest req)
        {
            var p = new DynamicParameters();
            p.Add("p_product_category_Id", req.ProductTypeId);
            p.Add("p_payment_mode_id", req.paymentMethodId);
            p.Add("p_provider_id", req.ProviderId);
            p.Add("p_slab_min_amount", req.SlabMinAmount);
            p.Add("p_slab_max_amount", req.SlabMaxAmount);
            p.Add("p_mdr_percent", req.MdrPercent);
            p.Add("p_fixed_fee", req.FixedFee);
            p.Add("p_effective_from", req.EffectiveFrom);
            p.Add("p_effective_to", req.EffectiveTo);

            return await _dbContext.GetSingleData<MdrPricingDto>("sp_insert_mdr_pricing", p);
        }

        public async Task<MdrPricingDto?> UpdateMdrPricing(MdrPricingUpdateRequest req)
        {
            var p = new DynamicParameters();
            p.Add("p_id", req.Id);
            p.Add("p_mdr_percent", req.MdrPercent);
            p.Add("p_fixed_fee", req.FixedFee);
            p.Add("p_effective_from", req.EffectiveFrom);
            p.Add("p_effective_to", req.EffectiveTo);

            return await _dbContext.GetSingleData<MdrPricingDto>("sp_update_mdr_pricing", p);
        }

        public async Task<IEnumerable<CommissionSchemeDto>> GetCommissionSchemes(string CategoryId, int ProviderId)
        {
            var param = new DynamicParameters();
            param.Add("p_CategoryId", CategoryId);
            param.Add("p_Provider_Id", ProviderId);

            return await _dbContext.GetData<CommissionSchemeDto>("GetCommissionSchemes", param);
        }

        public async Task<int> AddOrUpdateCommissionSchemes(CommissionSchemeRequest req)
        {
            var param = new DynamicParameters();
            param.Add("P_Id", req.Id);
            param.Add("P_ProviderId", req.ProviderId);
            param.Add("P_PaymentModeId", req.paymentMethodId);
            param.Add("P_Product_TypeId", req.ProductTypeId);
            param.Add("P_Admin_Percent", req.Admin_Percent);
            param.Add("P_SD_Percent", req.SD_Percent);
            param.Add("P_Distributor_Percent", req.Distributor_Percent);
            param.Add("P_Retailer_Percent", req.Retailer_Percent);
            param.Add("P_Effective_From", req.EffectiveFrom);
            param.Add("P_Effective_To", req.EffectiveTo);

            return await _dbContext.SetData("AddOrUpdateCommissionSchemes", param);
        }


        // ✅ Create
        public async Task<SpecialPriceRequest> SpecialPriceCreateAsync(SpecialPriceRequest req)  
        {
            var p= new DynamicParameters();
            p.Add("p_user_id", req.UserId);
            p.Add("p_product_category_id", req.ProductCategoryId);
            p.Add("p_price", req.Price);
            p.Add("p_description", req.Description);
            p.Add("p_created_by", req.ActionBy);

            return await _dbContext.GetSingleData<SpecialPriceRequest>("sp_create_special_price", p);
        }

        // ✅ Update
        public async Task<bool> SpecialPriceUpdateAsync( SpecialPriceRequest req)
        {
            var parameters = new DynamicParameters();
            parameters.Add("p_user_id", req.UserId);
            parameters.Add("p_price", req.Price);
            parameters.Add("p_description", req.Description);
            parameters.Add("p_modified_by", req.ActionBy);

            var rows = await _dbContext.ExecuteAsync(
                "sp_update_special_price",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return rows > 0;
        }

        // ✅ Activate / Inactivate
        public async Task<bool> SpecialPriceChangeStatusAsync(bool isActive, long userId)
        {
            var parameters = new DynamicParameters();
            parameters.Add("p_is_active", isActive ? 1 : 0);
            parameters.Add("p_user_id", userId);

            var rows = await _dbContext.ExecuteAsync(
                "sp_change_special_price_status",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return rows > 0;
        }

        // ✅ Get Prices
        public async Task<IEnumerable<SpecialPrice>> GetSpecialPriceAsync()
        {
            var parameters = new DynamicParameters();
            return await _dbContext.GetData<SpecialPrice>("sp_get_special_prices", parameters);
        }


        // CREATE
        public async Task<RoutingRuleRequest> RoutingCreateAsync(RoutingRuleRequest req)
        {
            var param = new DynamicParameters();
            param.Add("p_priority", req.Priority);
            param.Add("p_rule_name", req.RuleName);
            param.Add("p_criteria", req.Criteria);
            param.Add("p_provider", req.Provider);

            return await _dbContext.GetSingleData<RoutingRuleRequest>("sp_routing_rule_create", param);
        }

        // UPDATE
        public async Task<bool> RoutingUpdateAsync(long Id, RoutingRuleRequest req)
        {
            var param = new DynamicParameters();
            param.Add("p_id", Id);
            param.Add("p_priority", req.Priority);
            param.Add("p_rule_name", req.RuleName);
            param.Add("p_criteria", req.Criteria);
            param.Add("p_provider", req.Provider);

            var rows = await _dbContext.ExecuteAsync(
               "sp_routing_rule_update",
               param,
               commandType: CommandType.StoredProcedure
           );

            return rows > 0;
        }

        // GET ALL
        public async Task<IEnumerable<RoutingRule>> RoutingGetAllAsync()
        {
            var parameters = new DynamicParameters();
            return await _dbContext.GetData<RoutingRule>("sp_routing_rule_get_all", parameters);
        }
    }
}

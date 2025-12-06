using Dapper;
using Payments_core.Helpers;
using Payments_core.Models;
using Payments_core.Services.DataLayer;
using System.Data;

namespace Payments_core.Services.SuperDistributorService
{
    public class SuperDistributorService: ISuperDistributorService
    {

        private readonly IDapperContext _dbContext;

        public SuperDistributorService(IDapperContext dbContext)
        {
            _dbContext = dbContext;
        }

        // 🔵 1. Full onboarding using single stored procedure
        public async Task<(long userId, long merchantId)> CreateFullOnboardingAsync(SuperDistributorRequest req)
        {
            var param = new DynamicParameters();

            // USER
            param.Add("p_role_id", req.RoleId);
            param.Add("p_parent_user_id", req.ParentUserId);
            param.Add("p_full_name", req.FullName);
            param.Add("p_business_name", req.BusinessName);
            param.Add("p_email", req.Email);
            param.Add("p_mobile", req.Mobile);
            param.Add("p_password_hash", BCrypt.Net.BCrypt.HashPassword(req.Password));

            // MERCHANT
           // param.Add("p_legal_name", req.LegalName);
           // param.Add("p_trade_name", req.TradeName);
            //param.Add("p_business_type", req.BusinessType);
            //param.Add("p_category", req.Category);
            //param.Add("p_website_url", req.WebsiteUrl);
            //param.Add("p_settlement_profile", req.SettlementProfile);
            //param.Add("p_enabled_products", req.EnabledProducts);

            // KYC PROFILE
            param.Add("p_pan", req.PanNumber);
            param.Add("p_aadhaar4", req.AadhaarLast4);
           // param.Add("p_gstin", req.Gstin);
            param.Add("p_addr1", req.Address1);
            //param.Add("p_addr2", req.Address2);
            //param.Add("p_city", req.City);
            //param.Add("p_state", req.State);
            //param.Add("p_pincode", req.Pincode);
            //param.Add("p_bank_acc", req.BankAccountNo);
            //param.Add("p_bank_ifsc", req.BankIfsc);
            param.Add("isAuthorVerified", req.isAuthorVerified);
            // KYC DOCUMENT URLS
            param.Add("p_pan_url", req.PanUrl);
            param.Add("p_aadhaar_url", req.AadhaarUrl);
            // param.Add("p_gst_url", req.GstUrl);
            //param.Add("p_bank_url", req.BankUrl);

            param.Add("user_id", req.user_id);
            
            // OUTPUT VALUES
            param.Add("o_user_id", dbType: DbType.Int64, direction: ParameterDirection.Output);
            param.Add("o_merchant_id", dbType: DbType.Int64, direction: ParameterDirection.Output);

            await _dbContext.ExecuteAsync(
                "sp_merchants_create_full",
                param,
                CommandType.StoredProcedure
            );

            long userId = param.Get<long>("o_user_id");
            long merchantId = param.Get<long>("o_merchant_id");

            return (userId, merchantId);
        }

        // 🔵 2. Get full onboarding details
        public async Task<SuperDistributorFullResponse> GetFullByUserIdAsync(long userId)
        {
            using var multi = await _dbContext.QueryMultipleAsync(
                "sp_onboarding_get_full",
                new { p_user_id = userId },
                commandType: CommandType.StoredProcedure);

            var user = await multi.ReadFirstOrDefaultAsync<User>();
            var merchant = await multi.ReadFirstOrDefaultAsync<Merchant>();
            var profile = await multi.ReadFirstOrDefaultAsync<KycProfile>();
            var docs = (await multi.ReadAsync<KycDocument>()).ToList();

            return new SuperDistributorFullResponse
            {
                User = user,
                Merchant = merchant,
                Profile = profile,
                Documents = docs
            };
        }
        public async Task<IEnumerable<SuperDistributorCardDto>> GetCardsAsync(int roleId)
        {
            var param = new { p_role_id = roleId };
            return await _dbContext.GetData<SuperDistributorCardDto>(
                "sp_user_cards_get_all",
                param
            );
        }

        public async Task<uperDistributorProfileDto?> GetCardAsync(  int roleId, long userId)
        {
            var param = new { p_user_id = userId, p_role_id = roleId };
            var result = await _dbContext.GetData<uperDistributorProfileDto>("sp_user_card_get_one", param);
            return result.FirstOrDefault();
        }


    }
}

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

            // password only if create (optional)
            // If update and password is null/empty, skip hashing
            if (!string.IsNullOrWhiteSpace(req.Password))
                param.Add("p_password_hash", BCrypt.Net.BCrypt.HashPassword(req.Password));
            else
                param.Add("p_password_hash", null);

            // KYC PROFILE
            param.Add("p_pan", req.PanNumber);
            param.Add("p_aadhaar4", req.AadhaarLast4);
            param.Add("p_addr1", req.Address1);
            param.Add("p_isAuthorVerified", req.isAuthorVerified);

            // KYC DOCUMENT URLS
            param.Add("p_pan_url", req.PanUrl);
            param.Add("p_aadhaar_url", req.AadhaarUrl);

            // IMPORTANT: separate fields
            param.Add("p_created_by", req.UserId);  // logged-in user id
            param.Add("p_super_user_id", req.super_user_id);

            // IMPORTANT: record id for update
            param.Add("p_user_id", req.UserId);        // null/0 for insert, >0 for update
            param.Add("p_merchant_id", req.MerchantId);

            // OUTPUT VALUES
            param.Add("o_user_id", dbType: DbType.Int64, direction: ParameterDirection.Output);
            param.Add("o_merchant_id", dbType: DbType.Int64, direction: ParameterDirection.Output);

            await _dbContext.ExecuteAsync(
                "sp_merchants_upsert_full",
                param,
                CommandType.StoredProcedure
            );

            long? userId = param.Get<long?>("o_user_id");
            long? merchantId = param.Get<long?>("o_merchant_id");

            if (!userId.HasValue)
                throw new ApplicationException("sp_merchants_upsert_full returned NULL o_user_id. Fix SP update branch.");

            if (!merchantId.HasValue)
                throw new ApplicationException("sp_merchants_upsert_full returned NULL o_merchant_id. Fix SP update branch.");

            return (userId.Value, merchantId.Value);
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
        public async Task<IEnumerable<SuperDistributorCardDto>> GetCardsAsync(int roleId, long userId)
        {
            var param = new { p_role_id = roleId, p_user_id = userId };
            return await _dbContext.GetData<SuperDistributorCardDto>(
                "sp_user_cards_get_all",
                param
            );
        }

        public async Task<uperDistributorProfileDto?> GetCardDetailedInfo(  int roleId, long userId)
        {
            var param = new { p_user_id = userId, p_role_id = roleId };
            var result = await _dbContext.GetData<uperDistributorProfileDto>("sp_user_card_get_one", param);
            return result.FirstOrDefault();
        }
        public async Task<bool> GetKYCStatus(long userId)
        {
            var param = new { p_user_id = userId };
            var result = await _dbContext.GetData<bool>("sp_check_kyc_status", param);
            return result.FirstOrDefault();
        }

    }
}

using Payments_core.Services.DataLayer;

namespace Payments_core.Services.KycVerificationService
{
    public class KycApiCredentialService
    {
        private readonly IDapperContext _db;

        public KycApiCredentialService(IDapperContext db)
        {
            _db = db;
        }

        public async Task<dynamic> GetCashfreeCredentials()
        {
            var result = await _db.GetData<dynamic>(
                "sp_get_kyc_verification_api_credentials",
                new { p_provider_name = "CASHFREE" });

            return result.FirstOrDefault();
        }
    }
}

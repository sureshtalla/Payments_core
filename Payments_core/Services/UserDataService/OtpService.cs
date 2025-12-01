using Dapper;
using Payments_core.Services.DataLayer;
using System.Data;

namespace Payments_core.Services.UserDataService
{
    public class OtpService : IOtpService
    {
        private readonly IDapperContext _dbContext;
        public OtpService(IDapperContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<string> GenerateOtpAsync(long userId, string mobile)
        {
            string otp = new Random().Next(100000, 999999).ToString();

            var param = new DynamicParameters();
            param.Add("@p_user_id", userId);
            param.Add("@p_mobile", mobile);
            param.Add("@p_otp", otp);
            await _dbContext.SetData("sp_save_login_otp", param);

            // TODO: Integrate SMS sending API here
            return otp;
        }

        public async Task<bool> VerifyOtpAsync(long userId, string otp)
        {
            var param = new DynamicParameters();
            param.Add("@p_user_id", userId);
            param.Add("@p_otp", otp);

            // Calling stored procedure
            var result = await _dbContext.GetSingleData<int>("sp_verify_login_otp", param);

            return result == 1;  // 1 = OTP verified, 0 = invalid
        }

    }
}

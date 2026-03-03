using Dapper;
using Payments_core.Services.DataLayer;
using System.Security.Cryptography;

namespace Payments_core.Services.UserDataService
{
    public class OtpService : IOtpService
    {
        private readonly IDapperContext _dbContext;

        public OtpService(IDapperContext dbContext)
        {
            _dbContext = dbContext;
        }

        // ==============================
        // GENERATE SECURE OTP
        // ==============================
        public async Task<(string Otp, string SessionId)> GenerateOtpAsync(long userId, string mobile)
        {
            string otp = GenerateSecureOtp();
            string sessionId = Guid.NewGuid().ToString("N");

            string otpHash = BCrypt.Net.BCrypt.HashPassword(otp);

            var param = new DynamicParameters();
            param.Add("p_user_id", userId);
            param.Add("p_mobile", mobile);
            param.Add("p_otp_hash", otpHash);
            param.Add("p_expiry", DateTime.UtcNow.AddMinutes(3));
            param.Add("p_login_session_id", sessionId);

            await _dbContext.SetData("sp_save_login_otp_secure", param);

            return (otp, sessionId);
        }

        // ==============================
        // VERIFY OTP
        // ==============================
        public async Task<bool> VerifyOtpAsync(long userId, string sessionId, string inputOtp)
        {
            var param = new DynamicParameters();
            param.Add("p_user_id", userId);
            param.Add("p_login_session_id", sessionId);

            var record = await _dbContext.GetSingleData<OtpRecord>(
                "sp_verify_login_otp_secure",
                param);

            if (record == null)
                return false;

            if (record.is_used == 1)
                return false;

            if (record.expiry < DateTime.UtcNow)
                return false;

            if (record.attempt_count >= 3)
                return false;

            bool isValid = BCrypt.Net.BCrypt.Verify(inputOtp, record.otp_hash);

            if (!isValid)
            {
                await IncrementAttempt(userId, sessionId);
                return false;
            }

            await MarkOtpUsed(userId, sessionId);

            return true;
        }

        // ==============================
        // GET OTP RECORD
        // ==============================
        private async Task<OtpRecord?> GetOtpRecord(long userId, string sessionId)
        {
            var result = await _dbContext.GetData<OtpRecord>(
                @"SELECT otp_hash, expiry, attempt_count, is_used
                  FROM login_otps
                  WHERE user_id = @userId
                    AND login_session_id = @sessionId
                  ORDER BY created_at DESC
                  LIMIT 1",
                new { userId, sessionId });

            return result.FirstOrDefault();
        }

        // ==============================
        // INCREMENT ATTEMPT
        // ==============================
        private async Task IncrementAttempt(long userId, string sessionId)
        {
            var incParam = new DynamicParameters();
            incParam.Add("p_user_id", userId);
            incParam.Add("p_login_session_id", sessionId);

            await _dbContext.SetData(
                "sp_increment_login_otp_attempt",
                incParam);
        }

        // ==============================
        // MARK OTP USED
        // ==============================
        private async Task MarkOtpUsed(long userId, string sessionId)
        {
            var usedParam = new DynamicParameters();
            usedParam.Add("p_user_id", userId);
            usedParam.Add("p_login_session_id", sessionId);

            await _dbContext.SetData(
                "sp_mark_login_otp_used",
                usedParam);
        }

        // ==============================
        // CRYPTO SECURE OTP GENERATOR
        // ==============================
        private string GenerateSecureOtp()
        {
            var bytes = new byte[4];
            RandomNumberGenerator.Fill(bytes);
            int value = BitConverter.ToInt32(bytes, 0) & 0x7fffffff;
            return (value % 1000000).ToString("D6");
        }

        // ==============================
        // DTO
        // ==============================
        private class OtpRecord
        {
            public string otp_hash { get; set; }
            public DateTime expiry { get; set; }
            public int attempt_count { get; set; }
            public int is_used { get; set; }
        }
    }
}
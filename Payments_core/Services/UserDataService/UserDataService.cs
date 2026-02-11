using Dapper;
using MySqlConnector;
using Payments_core.Models;
using Payments_core.Services.DataLayer;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;

namespace Payments_core.Services.UserDataService
{
    public class UserDataService : IUserDataService
    {
        private readonly IDapperContext _dbContext;

        public UserDataService(IDapperContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<long> RegisterUserAsync(UserRegisterRequest request, string passwordHash)
        {
            var param = new DynamicParameters();
            param.Add("p_full_name", request.FullName);
            param.Add("p_mobile", request.Mobile);
            param.Add("p_email", request.Email);
            param.Add("p_password_hash", passwordHash);
            param.Add("p_role_id", request.RoleId);
            param.Add("p_parent_user_id", request.ParentUserId);
            param.Add("p_business_name", request.BusinessName);
            param.Add("p_tin_no", request.TinNo);
            param.Add("p_new_id", dbType: System.Data.DbType.Int64, direction: System.Data.ParameterDirection.Output);

            // ✅ Use SetData instead of Execute
            await _dbContext.SetData("sp_user_register", param);

            return param.Get<long>("p_new_id");
        }

        public async Task<UserProfileResponse?> GetUserByMobileAsync(string UserName)
        {
            var param = new DynamicParameters();
            param.Add("p_username", UserName);

            var data = await _dbContext.GetData<UserProfileResponse>("sp_user_login", param);
            return data.FirstOrDefault();
        }

        public async Task<UserProfileResponse?> GetProfileAsync(long id)
        {
            var param = new DynamicParameters();
            param.Add("p_id", id);

            var data = await _dbContext.GetData<UserProfileResponse>("sp_user_get_profile", param);
            return data.FirstOrDefault();
        }

        public async Task<bool> UpdateProfileAsync(UserUpdateProfileRequest request)
        {
            var param = new DynamicParameters();
            param.Add("p_id", request.Id);
            param.Add("p_full_name", request.FullName);
            param.Add("p_business_name", request.BusinessName);
            param.Add("p_tin_no", request.TinNo);

            // ✅ Use SetData instead of Execute
            await _dbContext.SetData("sp_user_update_profile", param);

            return true;
        }
        //public bool VerifyPassword(string plain, string hash)
        //{
        //    return BCrypt.Net.BCrypt.Verify(plain, hash);
        //}

        public bool VerifyPassword(string plain, string hash)
        {
            if (string.IsNullOrWhiteSpace(plain) || string.IsNullOrWhiteSpace(hash))
                return false;

            return BCrypt.Net.BCrypt.Verify(plain, hash);
        }

        public async Task<bool> UpdateLoginAttemptAsync(UserProfileResponse user)
        {
            var param = new DynamicParameters();
            param.Add("p_id", user.Id);
            param.Add("p_failed_attempts", user.failed_attempts);
            param.Add("p_is_blocked", user.is_blocked);
            param.Add("p_blocked_until", user.blocked_until);

            await _dbContext.SetData("sp_user_update_login_attempt", param);

            return true;
        }
        public async Task<IEnumerable<UserManagementResponse?>> GetUserManagementProfile()
        {
            var param = new DynamicParameters();
           return await _dbContext.GetData<UserManagementResponse>("sp_GetUsers", param);
        }
        public async Task<bool> ManageUserStatusAsync(ManageUserStatusRequest request)
        {
            var param = new DynamicParameters();
            param.Add("p_user_id", request.UserId);
            param.Add("p_action", request.Action);
            param.Add("p_status", request.StatusValue);
            await _dbContext.SetData("sp_manage_user_status", param); // ✅ just await

            return true; // return success explicitly
        }
        public async Task<string?> GetUserPasswordHashAsync(long userId)
        {
            var param = new DynamicParameters();
            param.Add("p_user_id", userId);

            var result = await _dbContext.GetData<string>(
                "sp_GetUserPasswordHash", param);

            return result.FirstOrDefault();
        }

        public async Task<bool> UpdateUserPasswordAsync(long userId, string passwordHash)
        {
            var param = new DynamicParameters();
            param.Add("p_user_id", userId);
            param.Add("p_password_hash", passwordHash);

            await _dbContext.SetData("sp_UpdateUserPassword", param);
            return true;
        }

        public async Task<string?> GetUserTinNoAsync(long userId)
        {
            var param = new DynamicParameters();
            param.Add("p_user_id", userId);

            var result = await _dbContext.GetData<string>(
                "sp_GetUserTinNo", param);

            return result.FirstOrDefault();
        }

        public async Task<bool> UpdateUserTinNoAsync(long userId, string tinNo)
        {
            var param = new DynamicParameters();
            param.Add("p_user_id", userId);
            param.Add("p_tin_no", tinNo);

            await _dbContext.SetData("sp_UpdateUserTinNo", param);
            return true;
        }

        public async Task<bool> SaveHashedOtpAsync(string mobile, string otpHash, DateTime expiry)
        {
            try
            {
                var param = new DynamicParameters();
                param.Add("p_mobile_no", mobile);
                param.Add("p_otp_hash", otpHash);
                param.Add("p_expiry", expiry);

                await _dbContext.SetData("sp_SaveHashedOtp", param);
                return true;
            }
            catch (MySqlException ex) // MySQL
            {
                // _logger.LogError(ex, "MySQL error while saving OTP for mobile {Mobile}", mobile);
                throw new DataServiceException("Database error while saving OTP", ex);
            }
            catch (Exception ex)
            {
                // _logger.LogError(ex, "Unexpected error while saving OTP for mobile {Mobile}", mobile);
                throw new DataServiceException("Unexpected error while saving OTP", ex);
            }
        }
        public class DataServiceException : Exception
        {
            public DataServiceException(string message, Exception inner)
                : base(message, inner) { }
        }


        public async Task<(string?, DateTime?, bool)> GetHashedOtpAsync(string mobile)
        {
            var param = new DynamicParameters();
            param.Add("p_mobile_no", mobile);

            var result = await _dbContext.GetData<dynamic>("sp_GetHashedOtp", param);
            var row = result.FirstOrDefault();

            if (row == null)
                return (null, null, false);

            return (row.otp_hash, row.otp_expiry, row.is_verified == 1);
        }

        public async Task<bool> VerifyMobileAsync(string mobile)
        {
            var param = new DynamicParameters();
            param.Add("p_mobile_no", mobile);

            await _dbContext.SetData("sp_VerifyMobile", param);
            return true;
        }

        public async Task<bool> ValidateUserTpin(long userId, string tpin)
        {
            var parameters = new DynamicParameters();
            parameters.Add("p_user_id", userId);
            parameters.Add("p_tpin", tpin);

            var result = await _dbContext.GetData<int>(
                "sp_user_validate_tpin",
                parameters
            );

            return result.FirstOrDefault() > 0;
        }

    }
}

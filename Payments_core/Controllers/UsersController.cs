using Microsoft.AspNetCore.Mvc;
using Payments_core.Models;
using Payments_core.Services.MasterDataService;
using Payments_core.Services.OTPService;
using Payments_core.Services.UserDataService;

namespace Payments_core.Controllers
{

    [ApiController]
    [Route("api/users")]
    public class UsersController : Controller
    {

        private readonly IUserDataService userDataService;
        private readonly IOtpService otpDataService;
        private readonly IMSG91OTPService msgOtpService;

        public UsersController(IUserDataService _userDataService, IOtpService _otpDataService, IMSG91OTPService _msgOtpService)
        {
            userDataService = _userDataService;
            otpDataService = _otpDataService;
            msgOtpService = _msgOtpService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserRegisterRequest request)
        {
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            var result = await userDataService.RegisterUserAsync(request, passwordHash);

            return Ok(new { user_id = result, message = "Registered successfully" });
        }

        //[HttpPost("login")]
        //public async Task<IActionResult> Login([FromBody] UserLoginRequest request)
        //{
        //    var user = await userDataService.GetUserByMobileAsync(request.UserName);
        //    if (user == null)
        //        return Unauthorized("Invalid user");

        //    bool isValid = userDataService.VerifyPassword(request.Password, user.password_hash);
        //    if (!isValid)
        //        return Unauthorized("Invalid password");

        //    // Generate OTP
        //    string otp = await otpDataService.GenerateOtpAsync(user.Id, user.Mobile);

        //    return Ok(new
        //    {
        //        message = "Password verified. OTP sent.",
        //        user_id = user.Id
        //    });
        //}
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] UserLoginRequest request)
        {
            var user = await userDataService.GetUserByMobileAsync(request.UserName);

            if (user == null)
                return Unauthorized("Invalid user");

            // Check if blocked
            if (user.is_blocked && user.blocked_until > DateTime.UtcNow)
            {
                return Unauthorized($"User is blocked until {user.blocked_until}. Try after 24 hours.");
            }

            bool isValid = userDataService.VerifyPassword(request.Password, user.password_hash);

            if (!isValid)
            {
                user.failed_attempts++;

                // Block for 24 hours
                if (user.failed_attempts >= 3)
                {
                    user.is_blocked = true;
                    user.blocked_until = DateTime.UtcNow.AddHours(24);

                    await userDataService.UpdateLoginAttemptAsync(user);

                    return Unauthorized("Account blocked for 24 hours due to multiple wrong attempts.");
                }

                await userDataService.UpdateLoginAttemptAsync(user);

                return Unauthorized($"Invalid Password. Attempts left: {3 - user.failed_attempts}");
            }

            // Reset attempts
            user.failed_attempts = 0;
            user.is_blocked = false;
            user.blocked_until = null;

            await userDataService.UpdateLoginAttemptAsync(user);

            // Generate OTP
            string otp = await otpDataService.GenerateOtpAsync(user.Id, user.Mobile);
            var msgConfig = await msgOtpService.GetMSGOTPConfigAsync();
            var result = await msgOtpService.MSG91SendOTPAsync(otp, user.Mobile, msgConfig.MSGOtpAuthKey, msgConfig.MSGOtpTemplateId, msgConfig.MSGUrl);

            if (result)
            {
                return Ok(new
                {
                    message = "Password verified. OTP sent.",
                    user_id = user.Id
                });
            }
            else
            {
                return Ok(new
                {
                    message = "The password was verified, but the OTP could not be sent. Please try again.",
                    user_id = user.Id
                });
            }
        }

        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest request)
        {
            bool isValid = await otpDataService.VerifyOtpAsync(request.UserId, request.Otp);
            if (!isValid)
                return Unauthorized("Invalid OTP");

            var user = await userDataService.GetProfileAsync(request.UserId);

            return Ok(new
            {
                message = "Login successful",
                data = user
            });
        }

        [HttpGet("profile/{id}")]
        public async Task<IActionResult> Profile(long id)
        {
            var result = await userDataService.GetProfileAsync(id);
            return Ok(result);
        }

        [HttpPut("profile/update")]
        public async Task<IActionResult> UpdateProfile([FromBody] UserUpdateProfileRequest request)
        {
            var success = await userDataService.UpdateProfileAsync(request);
            return Ok(new { success, message = "Profile updated successfully." });
        }


        [HttpGet("UserManagementProfile")]
        public async Task<IActionResult> GetUserManagementProfile()
        {
            var result = await userDataService.GetUserManagementProfile();
            return Ok(result);
        }
    }
}

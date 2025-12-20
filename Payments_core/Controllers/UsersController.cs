using Microsoft.AspNetCore.Identity.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Payments_core.Models;
using Payments_core.Services.MasterDataService;
using Payments_core.Services.OTPService;
using Payments_core.Services.UserDataService;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Microsoft.AspNetCore.Authorization;

namespace Payments_core.Controllers
{

    [Authorize]
    [ApiController]
    [Route("api/users")]
    public class UsersController : Controller
    {

        private readonly IUserDataService userDataService;
        private readonly IOtpService otpDataService;
        private readonly IMSG91OTPService msgOtpService;
        IConfiguration config;

        public UsersController(IUserDataService _userDataService, IOtpService _otpDataService, IMSG91OTPService _msgOtpService, IConfiguration _config)
        {
            userDataService = _userDataService;
            otpDataService = _otpDataService;
            msgOtpService = _msgOtpService;
            config = _config;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserRegisterRequest request)
        {
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            var result = await userDataService.RegisterUserAsync(request, passwordHash);

            return Ok(new { user_id = result, message = "Registered successfully" });
        }

        [HttpPost("login")]
        [AllowAnonymous]
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
        [AllowAnonymous]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest request)
        {
            bool isValid = await otpDataService.VerifyOtpAsync(request.UserId, request.Otp);
            if (!isValid)
                return Unauthorized("Invalid OTP");

            var user = await userDataService.GetProfileAsync(request.UserId);

            var claims = new[]
                {
                    new Claim(ClaimTypes.Name, user.full_name),
                    new Claim(ClaimTypes.Role, user.role_name),
                };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                issuer: config["Issuer"],
                audience: config["Audience"],
                claims: claims,
                expires: DateTime.Now.AddHours(1),
                signingCredentials: creds);

            user.Token = new JwtSecurityTokenHandler().WriteToken(token);

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

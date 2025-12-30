using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Payments_core.Models;
using Payments_core.Services.OTPService;
using Payments_core.Services.UserDataService;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;

namespace Payments_core.Controllers
{

    //[Authorize]
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
            if (request == null || request.UserId <= 0 || string.IsNullOrWhiteSpace(request.Otp))
                return BadRequest("Invalid request data");

            bool isValid = await otpDataService.VerifyOtpAsync(request.UserId, request.Otp);
            if (!isValid)
                return Unauthorized("Invalid OTP");

            var user = await userDataService.GetProfileAsync(request.UserId);
            if (user == null)
                return Unauthorized("User not found");

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, user.full_name ?? "User"),
                new Claim(ClaimTypes.Role, user.role_name ?? "User"),
                new Claim("UserId", request.UserId.ToString())
            };

            var jwtKey = config["JwtSettings:Key"];
            if (string.IsNullOrWhiteSpace(jwtKey))
                return StatusCode(500, "JWT Key missing in configuration");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: config["JwtSettings:Issuer"],
                audience: config["JwtSettings:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(
                    Convert.ToDouble(config["JwtSettings:DurationInMinutes"])
                ),
                signingCredentials: creds
            );

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
        [HttpPut("profile/managestatus")]
        public async Task<IActionResult> ManageUserStatus([FromBody] ManageUserStatusRequest request)
        {
            if (request.Action == "status" && string.IsNullOrWhiteSpace(request.StatusValue))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "StatusValue is required when action is status."
                });
            }

            var success = await userDataService.ManageUserStatusAsync(
                request
            );

            return Ok(new
            {
                success,
                message = request.Action == "status"
                    ? "User status updated successfully."
                    : "User unblocked successfully."
            });
        }

        [HttpPost("resetpassword")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            if (request.NewPassword != request.ConfirmPassword)
                return BadRequest(new { alert = "New password and confirm password do not match" });

            // 🔐 Get existing hash
            var storedHash = await userDataService.GetUserPasswordHashAsync(request.UserId);
            if (storedHash == null)
                return BadRequest(new { alert = "User not found" });

            // 🔐 Compare old password
            if (!BCrypt.Net.BCrypt.Verify(request.OldPassword, storedHash))
                return BadRequest(new { alert = "Old password is incorrect" });

            // 🔐 Hash new password
            var newHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);

            await userDataService.UpdateUserPasswordAsync(request.UserId, newHash);

            return Ok(new { message = "Password updated successfully" });
        }

        [HttpPost("updatetin")]
        public async Task<IActionResult> UpdateTin([FromBody] UpdateTinRequest request)
        {
            // 1️⃣ New & confirm must match
            if (request.TinNo != request.ConfirmTinNo)
                return BadRequest(new { alert = "TIN number and confirm TIN do not match" });

            // 2️⃣ Validate 6-digit format
            if (!Regex.IsMatch(request.TinNo, @"^\d{6}$"))
                return BadRequest(new { alert = "TIN must be exactly 6 digits" });

            // 3️⃣ Get existing TIN
            var existingTin = await userDataService.GetUserTinNoAsync(request.UserId);
            if (existingTin == null)
                return BadRequest(new { alert = "User not found" });

            // 4️⃣ Compare old TIN
            if (existingTin != request.OldTinNo)
                return BadRequest(new { alert = "Old TIN number is incorrect" });

            // 5️⃣ Update TIN
            await userDataService.UpdateUserTinNoAsync(request.UserId, request.TinNo);

            return Ok(new { message = "TIN updated successfully" });
        }



    }
}

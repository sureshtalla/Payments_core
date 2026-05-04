using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Payments_core.Models;
using Payments_core.Services.OTPService;
using Payments_core.Services.UserDataService;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.RateLimiting;

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
        private string GenerateAccessToken(UserProfileResponse user)
        {
            var claims = new[]
            {
                new Claim("UserId", user.Id.ToString()),
                new Claim(ClaimTypes.Role, user.role_name)
            };

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(config["JwtSettings:Key"]));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var duration = int.Parse(config["JwtSettings:DurationInMinutes"]);

            var token = new JwtSecurityToken(
                issuer: config["JwtSettings:Issuer"],
                audience: config["JwtSettings:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(duration),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private string GenerateSecureRefreshToken()
        {
            var randomBytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);

            return Convert.ToBase64String(randomBytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", "");
        }

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

        [EnableRateLimiting("login")]
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] UserLoginRequest request)
        {
            var user = await userDataService.GetUserByMobileAsync(request.UserName);

            if (user == null)
                return Unauthorized("Invalid user");

            if (user.is_blocked && user.blocked_until > DateTime.UtcNow)
                return Unauthorized($"User blocked until {user.blocked_until}");

            bool isValid = userDataService.VerifyPassword(
                request.Password,
                user.password_hash);

            if (!isValid)
            {
                user.failed_attempts++;

                if (user.failed_attempts >= 3)
                {
                    user.is_blocked = true;
                    user.blocked_until = DateTime.UtcNow.AddHours(24);
                }

                await userDataService.UpdateLoginAttemptAsync(new UserProfileResponse
                {
                    Id = user.Id,
                    failed_attempts = user.failed_attempts,
                    is_blocked = user.is_blocked,
                    blocked_until = user.blocked_until,
                    full_name = "",
                    Mobile = "",
                    Email = "",
                    role_id = 0,
                    role_name = "",
                    business_name = "",
                    Status = "",
                    tinno = "",
                    PAN = "",
                    Aadhar = "",
                    Address = "",
                    Token = ""
                });

                return Unauthorized("Invalid credentials");
            }

            // Reset attempts
            user.failed_attempts = 0;
            user.is_blocked = false;
            user.blocked_until = null;

            await userDataService.UpdateLoginAttemptAsync(new UserProfileResponse
            {
                Id = user.Id,
                failed_attempts = 0,
                is_blocked = false,
                blocked_until = null,
                full_name = "",
                Mobile = "",
                Email = "",
                role_id = 0,
                role_name = "",
                business_name = "",
                Status = "",
                tinno = "",
                PAN = "",
                Aadhar = "",
                Address = "",
                Token = ""
            });

            // 🔥 Use real mobile from DB (already 91XXXXXXXXXX format)
            if (string.IsNullOrWhiteSpace(user.mobile))
                return StatusCode(500, "User mobile not configured");

            // ================================================================
            // DEMO USER — skip SMS only, OTP saved to DB normally
            // Remove this block before production
            // ================================================================
            if (request.UserName.Trim().ToLower() == "demo")
            {
                var demoSessionId = await otpDataService.SaveDemoOtpAsync(user.Id, user.mobile);
                return Ok(new
                {
                    message = "OTP sent successfully.",
                    user_id = user.Id,
                    session_id = demoSessionId
                });
            }
            // ================================================================
            // END DEMO USER
            // ================================================================

            // Generate OTP
            var (otp, sessionId) = await otpDataService.GenerateOtpAsync(
                user.Id,
                user.mobile
            );

            var msgConfig = await msgOtpService.GetMSGOTPConfigAsync();

            // Send OTP using REAL mobile (NOT request.UserName)
            var result = await msgOtpService.MSG91SendOTPAsync(
                otp,
                user.mobile,   // ✅ FIXED
                msgConfig.MSGOtpAuthKey,
                msgConfig.MSGOtpTemplateId,
                msgConfig.MSGUrl
            );

            if (!result)
                return StatusCode(500, "OTP sending failed");

            return Ok(new
            {
                message = "Password verified. OTP sent.",
                user_id = user.Id,
                session_id = sessionId
            });
        }

        [EnableRateLimiting("otp")]
        [HttpPost("verify-otp")]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest request)
        {
            if (request == null ||
                request.UserId <= 0 ||
                string.IsNullOrWhiteSpace(request.Otp) ||
                string.IsNullOrWhiteSpace(request.SessionId))
            {
                return BadRequest("Invalid request data");
            }

            // 🔐 Verify OTP (Session Bound + BCrypt)
            bool isValid = await otpDataService.VerifyOtpAsync(
                request.UserId,
                request.SessionId,
                request.Otp);

            if (!isValid)
                return Unauthorized("Invalid OTP");

            // 🔍 Get user profile
            var user = await userDataService.GetProfileAsync(request.UserId);
            if (user == null)
                return Unauthorized("User not found");

            // 🔐 Generate Tokens
            var accessToken = GenerateAccessToken(user);
            var refreshToken = GenerateSecureRefreshToken();

            // Save refresh token in DB
            await userDataService.SaveUserSessionAsync(
                user.Id,
                refreshToken,
                HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                Request.Headers["User-Agent"].ToString(),
                DateTime.UtcNow.AddDays(7)
            );

            // 🔐 Store refresh token in HttpOnly Cookie
            Response.Cookies.Append("refreshToken", refreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = DateTime.UtcNow.AddDays(7)
            });

            return Ok(new
            {
                message = "Login successful",
                access_token = accessToken,
                expires_in_minutes = 15,
                user = new
                {
                    user.Id,
                    user.full_name,
                    user.role_id,
                    user.role_name,
                    user.parent_user_id,
                    user.business_name,
                    user.Status
                }
            });
        }

        [HttpPost("refresh-token")]
        [AllowAnonymous]
        public async Task<IActionResult> RefreshToken()
        {
            var refreshToken = Request.Cookies["refreshToken"];

            if (string.IsNullOrEmpty(refreshToken))
                return Unauthorized("No refresh token");

            // 🔥 ALWAYS DECODE
            refreshToken = Uri.UnescapeDataString(refreshToken);

            var (userId, isValid) =
                await userDataService.ValidateRefreshTokenAsync(refreshToken);

            if (!isValid)
                return Unauthorized("Invalid refresh token");

            var user = await userDataService.GetProfileAsync(userId);
            if (user == null)
                return Unauthorized();

            var newAccessToken = GenerateAccessToken(user);
            var newRefreshToken = GenerateSecureRefreshToken();

            // 🔥 Revoke ALL old tokens of user
            await userDataService.RevokeAllUserRefreshTokensAsync(user.Id);

            await userDataService.SaveUserSessionAsync(
                user.Id,
                newRefreshToken,
                HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                Request.Headers["User-Agent"].ToString(),
                DateTime.UtcNow.AddDays(7)
            );

            Response.Cookies.Append("refreshToken", newRefreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = DateTime.UtcNow.AddDays(7)
            });

            return Ok(new
            {
                accessToken = newAccessToken,
                expiresInMinutes = int.Parse(config["JwtSettings:DurationInMinutes"])
            });
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var refreshToken = Request.Cookies["refreshToken"];

            if (!string.IsNullOrEmpty(refreshToken))
                await userDataService.RevokeRefreshTokenAsync(refreshToken);

            Response.Cookies.Delete("refreshToken");

            return Ok(new { message = "Logged out successfully" });
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

            var success = await userDataService.ManageUserStatusAsync(request);

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

        [HttpPost("updateTpin")]
        public async Task<IActionResult> UpdateTpin([FromBody] UpdateTinRequest request)
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

        [EnableRateLimiting("otp")]
        [HttpPost("send-otp")]
        public async Task<IActionResult> SendOtp([FromBody] SendOtpRequest request)
        {
            // 1️⃣ Generate OTP (session-based, stored in login_otps)
            var (otp, sessionId) = await otpDataService.GenerateOtpAsync(request.UserId, request.Mobile);

            // 2️⃣ Send OTP via SMS provider
            var msgConfig = await msgOtpService.GetMSGOTPConfigAsync();
            var result = await msgOtpService.MSG91SendOTPAsync(otp, request.Mobile, msgConfig.MSGOtpAuthKey, msgConfig.MSGOtpTemplateId, msgConfig.MSGUrl);

            if (result)
            {
                return Ok(new
                {
                    message = "Password verified. OTP sent.",
                    user_id = request.UserId,
                    session_id = sessionId
                });
            }
            else
            {
                return Ok(new
                {
                    message = "OTP could not be sent. Please try again.",
                });
            }
        }

        [HttpPost("mobileverify-otp")]
        public async Task<IActionResult> MobileVerifyOtp([FromBody] VerifyMobileOtpRequest request)
        {
            var (otpHash, expiry, isVerified) =
                await userDataService.GetHashedOtpAsync(request.Mobile);

            if (isVerified)
                return BadRequest(new { alert = "Mobile already verified" });

            if (otpHash == null)
                return BadRequest(new { alert = "OTP not found" });

            if (DateTime.UtcNow > expiry)
                return BadRequest(new { alert = "OTP expired" });

            // 🔐 BCrypt compare
            if (!BCrypt.Net.BCrypt.Verify(request.Otp, otpHash))
                return BadRequest(new { alert = "Invalid OTP" });

            // ✅ OTP correct
            await userDataService.VerifyMobileAsync(request.Mobile);

            return Ok(new { message = "Mobile verified successfully" });
        }

    }
}
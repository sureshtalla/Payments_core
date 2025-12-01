using Microsoft.AspNetCore.Mvc;
using Payments_core.Models;
using Payments_core.Services.MasterDataService;
using Payments_core.Services.UserDataService;

namespace Payments_core.Controllers
{

    [ApiController]
    [Route("api/users")]
    public class UsersController : Controller
    {

        private readonly IUserDataService userDataService;
        private readonly IOtpService otpDataService;

        public UsersController(IUserDataService _userDataService, IOtpService _otpDataService)
        {
            userDataService = _userDataService;
            otpDataService = _otpDataService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserRegisterRequest request)
        {
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            var result = await userDataService.RegisterUserAsync(request, passwordHash);

            return Ok(new { user_id = result, message = "Registered successfully" });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] UserLoginRequest request)
        {
            var user = await userDataService.GetUserByMobileAsync(request.UserName);
            if (user == null)
                return Unauthorized("Invalid user");

            bool isValid = userDataService.VerifyPassword(request.Password, user.password_hash);
            if (!isValid)
                return Unauthorized("Invalid password");

            // Generate OTP
            string otp = await otpDataService.GenerateOtpAsync(user.Id, user.Mobile);

            return Ok(new
            {
                message = "Password verified. OTP sent.",
                user_id = user.Id
            });
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
    }
}

using Microsoft.AspNetCore.Mvc;
using Payments_core.Models;
using Payments_core.Services.SuperDistributorService;

namespace Payments_core.Controllers
{
    [ApiController]
    [Route("api/users")]
    public class SuperDistributorController : Controller
    {
        private readonly ISuperDistributorService _service;
        public SuperDistributorController(ISuperDistributorService service) { _service = service; }

        // 🔵 Full onboarding (User + Merchant + KYC + Docs)
        [HttpPost("full")]
        public async Task<IActionResult> CreateFull([FromBody] SuperDistributorRequest req)
        {
            var (userId, merchantId) = await _service.CreateFullOnboardingAsync(req);

            return Ok(new SuperDistributorResponse
            {
                UserId = userId,
                MerchantId = merchantId,
                Message = "Full onboarding created successfully"
            });
        }

        // 🔵 Get full onboarding info by user id
        [HttpGet("{userId}")]
        public async Task<IActionResult> GetFull(long userId)
        {
            var result = await _service.GetFullByUserIdAsync(userId);

            if (result == null)
                return NotFound(new { message = "User not found" });

            return Ok(result);
        }
    }
}

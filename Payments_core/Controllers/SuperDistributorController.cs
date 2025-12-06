using Microsoft.AspNetCore.Mvc;
using Payments_core.Models;
using Payments_core.Services.SuperDistributorService;

namespace Payments_core.Controllers
{
    [ApiController]
    [Route("api/superdistributor")]
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

        [HttpGet("GetCards/{roleId}")]
        public async Task<IActionResult> GetCards(int roleId)
        {
            var data = await _service.GetCardsAsync(roleId);
            return Ok(data);
        }

        [HttpGet("GetCardDetailedInfo/{roleId}/{userId}")]
        public async Task<IActionResult> GetCardDetailedInfo( int roleId, long userId)
        {
            var data = await _service.GetCardDetailedInfo( roleId, userId);
            return Ok(data);
        }


    }
}

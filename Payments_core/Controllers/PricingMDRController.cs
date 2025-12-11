using Microsoft.AspNetCore.Mvc;
using Payments_core.Models;
using Payments_core.Services.MerchantDataService;
using Payments_core.Services.PricingMDRDataService;

namespace Payments_core.Controllers
{
    [ApiController]
    [Route("api/pricingMDR")]
    public class PricingMDRController : Controller
    {
        private readonly IPricingMDRDataService _service;
        public PricingMDRController(IPricingMDRDataService service) { _service = service; }
        
        [HttpGet("GetMdrPricing")]
        public async Task<IActionResult> GetMdrPricing([FromQuery] string? category, [FromQuery] int? providerId)
        {
            var data = await _service.GetMdrPricing(category, providerId);
            return Ok(data);
        }

        [HttpPost("CreatePricingMDR")]
        public async Task<IActionResult> Create(MdrPricingCreateRequest req)
        {
            try
            {
                var result = await _service.InsertMdrPricing(req);

                if (result == null)
                    return BadRequest("Failed to insert MDR pricing");

                return Ok("Data loaded successfully");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal Server Error: {ex.Message}");
            }
        }

        [HttpGet("PricingUpdate/{pricingId}")]
        public async Task<IActionResult> Update(long pricingId, MdrPricingUpdateRequest req)
        {
            if (pricingId != req.Id)
                return BadRequest("ID mismatch");

            var result = await _service.UpdateMdrPricing(req);
            return Ok(result);
        }
    }
}

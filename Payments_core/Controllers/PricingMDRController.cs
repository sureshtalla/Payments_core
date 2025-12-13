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
                // Call the service to insert the data
                var result = await _service.InsertMdrPricing(req);

                if (result == null)
                {
                    // Return BadRequest with a more informative error message
                    return BadRequest(new { success = false, message = "Failed to insert MDR pricing" });
                }

                // Return Ok with a success flag and message
                return Ok(new { success = true, message = "Data loaded successfully" });
            }
            catch (Exception ex)
            {
                // Log the exception (optional)
                //_logger.LogError(ex, "Error occurred while inserting MDR pricing");

                // Return a 500 error with the exception message
                return StatusCode(500, new { success = false, message = $"Internal Server Error: {ex.Message}" });
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

        [HttpGet("GetCommissionSchemes/{CategoryId}/{ProviderId}")]
        public async Task<IActionResult> GetCommissionSchemes(string CategoryId, int ProviderId)
        {
            var data = await _service.GetCommissionSchemes(CategoryId, ProviderId);
            return Ok(data);
        }

        [HttpPost("AddOrUpdateCommissionSchemes")]
        public async Task<IActionResult> AddOrUpdateCommissionSchemes(CommissionSchemeRequest req)
        {
            try
            {
                // Call the service to insert the data
                var result = await _service.AddOrUpdateCommissionSchemes(req);

                if (result == null)
                {
                    // Return BadRequest with a more informative error message
                    return BadRequest(new { success = false, message = "Failed to insert/update commission scheme" });
                }

                // Return Ok with a success flag and message
                return Ok(new { success = true, message = "Inserted or updated commission scheme successfully" });
            }
            catch (Exception ex)
            {
                // Log the exception (optional)
                //_logger.LogError(ex, "Error occurred while inserting MDR pricing");

                // Return a 500 error with the exception message
                return StatusCode(500, new { success = false, message = $"Internal Server Error: {ex.Message}" });
            }
        }
    }
}

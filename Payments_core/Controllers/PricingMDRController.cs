using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payments_core.Models;
using Payments_core.Services.PricingMDRDataService;

namespace Payments_core.Controllers
{
    [Authorize]
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

        [HttpPut("PricingUpdate")]
        public async Task<IActionResult> Update([FromBody] MdrPricingUpdateRequest req)
        {
            try
            {
                var result = await _service.UpdateMdrPricing(req);

                if (result == null)
                    return BadRequest("Failed to update MDR pricing");

                return Ok("Pricing updated successfully");
            }
            catch (MySqlConnector.MySqlException ex)
            {
                // Database-specific error
                return StatusCode(500, $"Database error: {ex.Message}");
            }
            catch (Exception ex)
            {
                // General error
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }


        [HttpGet("GetCommissionSchemes/{ProviderId}")]
        public async Task<IActionResult> GetCommissionSchemes(int ProviderId)
        {
            var data = await _service.GetCommissionSchemes( ProviderId);
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

        // ✅ Create
        [HttpPost("SpecialPriceCreateAsync")]
        public async Task<IActionResult> SpecialPriceCreateAsync(SpecialPriceRequest request)
        {
            var id = await _service.SpecialPriceCreateAsync(request);
            return Ok(new { message = "Special price created", id });
        }

        // ✅ Update
        [HttpPut("SpecialPriceUpdateAsync")]
        public async Task<IActionResult> SpecialPriceUpdateAsync( SpecialPriceRequest request)
        {
            var result = await _service.SpecialPriceUpdateAsync( request);
            return result ? Ok("Updated successfully") : BadRequest("Update failed");
        }

        // ✅ Activate / Inactivate
        [HttpPut("SpecialPriceChangeStatusAsync/status")]
        public async Task<IActionResult> SpecialPriceChangeStatusAsync(bool isActive, long userId)
        {
            var result = await _service.SpecialPriceChangeStatusAsync(isActive, userId);
            return result ? Ok("Status updated") : BadRequest("Failed");
        }

        // ✅ Get Prices
        [HttpGet("GetSpecialPriceAsync")]
        public async Task<IActionResult> GetSpecialPriceAsync()
        {
            var data = await _service.GetSpecialPriceAsync();
            return Ok(data);
        }



        // CREATE
        [HttpPost("RoutingCreateAsync")]
        public async Task<IActionResult> RoutingCreate(RoutingRuleRequest request)
        {
            var id = await _service.RoutingCreateAsync(request);
            return Ok(new { message = "Rule created successfully", id });
        }

        // UPDATE
        [HttpPut("RoutingUpdateAsync/{id}")]
        public async Task<IActionResult> RoutingUpdateAsync(long id, RoutingRuleRequest request)
        {
            var updated = await _service.RoutingUpdateAsync(id,request);
            return updated ? Ok("Updated successfully") : NotFound("Record not found");
        }

        // GET ALL
        [HttpGet("RoutingGetAll")]
        public async Task<IActionResult> RoutingGetAll()
        {
            var data = await _service.RoutingGetAllAsync();
            return Ok(data);
        }

    }
}

using Microsoft.AspNetCore.Mvc;
using Payments_core.Models;
using Payments_core.Services.MerchantDataService;
using Payments_core.Services.SuperDistributorService;

namespace Payments_core.Controllers
{
    [ApiController]
    [Route("api/merchantdata")]
    public class MerchantDataController : Controller
    {
        private readonly IMerchantDataService _service;
        public MerchantDataController(IMerchantDataService service) { _service = service; }
       
        [HttpGet("GetMerchantsData")]
        public async Task<IActionResult> GetMerchantsData()
        {
            var data = await _service.GetAllMerchantsAsync();
            return Ok(data);
        }

        [HttpPost("merchant/approval")]
        public async Task<IActionResult> UpdateMerchantApproval([FromBody] MerchantApprovalRequest req)
        {
            if (req.Action == "REJECT" && string.IsNullOrWhiteSpace(req.Remarks))
                return BadRequest("Remarks are required when rejecting.");

            var result = await _service.UpdateMerchantApprovalAsync(req);
            return Ok(new { success = true });
        }


        [HttpPost("merchantkyc/update")]
        public async Task<IActionResult> UpdateMerchantKyc([FromBody] MerchantKycUpdateRequest req)
        {
            var result = await _service.UpdateMerchantKycStatusAsync(req);
            return Ok(new { success = true });
        }

    }
}

using System.Transactions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payments_core.Models;
using Payments_core.Services.MasterDataService;
using Payments_core.Services.MerchantDataService;
using Payments_core.Services.SuperDistributorService;

namespace Payments_core.Controllers
{
    [Authorize]
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

        [HttpPost("Merchant/WalletLoad")]
        public async Task<IActionResult> WalletLoad([FromBody] WalletLoadInit req)
        {
            req.TransactionId = Guid.NewGuid().ToString("N").ToUpper();
            var result = await _service.WalletLoad(req);
            await _service.WalletLoadCommissionPercent(req);
            return Ok(new { success = true });
        }

        [HttpPost("Merchant/CreateBeneficiary")]
        public async Task<IActionResult> CreateBeneficiary([FromBody] Beneficiary req)
        {
            var result = await _service.CreateBeneficiary(req);
            return Ok(new { success = true });
        }

        [HttpPost("Merchant/VerifyBeneficiary/{Id}")]
        public async Task<IActionResult> VerifyBeneficiary(int Id)
        {
            var result = await _service.VerifyBeneficiary(Id);
            return Ok(new { success = true });
        }

        [HttpGet]
        [Route("GetBeneficiaries/{UserId}")]
        public async Task<IActionResult> GetBeneficiaries(int UserId)
        {
            try
            {
                var data = await _service.GetBeneficiaries(UserId);
                return Ok(data);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost("Merchant/PayoutAsync")]
        public async Task<IActionResult> PayoutAsync(PayoutRequestInit req)
        {
            PayoutRequest request = new PayoutRequest
            {
                UserId = req.UserId,
                Amount = req.Amount,
                FeeAmount = req.FeeAmount,
                Mode = req.Mode,
                TPin = req.TPin,
                BeneficiaryId = req.BeneficiaryId,
                TransactionId = Guid.NewGuid().ToString("N").ToUpper()
            };

            var response = await _service.PayoutInitAsync(request);
            //To be done
            request.Status = PayoutStatus.SUCCESS;
            request.Reason = "Success";
            var result = await _service.PayoutAsync(request);
            return Ok(new { success = true });
        }

        [HttpPost("Merchant/WalletTransfer")]
        public async Task<IActionResult> WalletTransfer(WalletTransferInit req)
        {
            var response = await _service.WalletTransfer(req);
            return Ok(new { success = true });
        }

    }
}

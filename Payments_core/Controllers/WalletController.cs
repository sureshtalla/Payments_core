using System.Transactions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payments_core.Models;
using Payments_core.Services.WalletService;


namespace Payments_core.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/wallets")]
    public class WalletController : Controller
    {
        private readonly IWalletService _service;
        public WalletController(IWalletService service) { _service = service; }

        //[HttpPost("WalletLoad")]
        //public async Task<IActionResult> WalletLoad([FromBody] WalletLoadInit req)
        //{
        //    req.TransactionId = Guid.NewGuid().ToString("N").ToUpper();
        //    var result = await _service.WalletLoad(req);
        //    await _service.WalletLoadCommissionPercent(req);
        //    return Ok(new { success = true });
        //}


        // ================================
        // PAYIN INIT
        // ================================
        [HttpPost("WalletLoad")]
        public async Task<IActionResult> PayinInitiate(WalletLoadInit req)
        {
            req.TransactionId = Guid.NewGuid().ToString("N").ToUpper();

            await _service.WalletLoad(req);

            return Ok(new { success = true, txnId = req.TransactionId });
        }

        // ================================
        // PAYIN SUCCESS
        // ================================
        [HttpPost("payin/success")]
        public async Task<IActionResult> PayinSuccess(WalletLoadSuccessDto dto)
        {
            await _service.UpdateWalletLoadStatus(dto.UserId,
                dto.TransactionId, 1, "SUCCESS");

            await _service.WalletLoadCommissionPercent(new WalletLoadInit
            {
                UserId = dto.UserId,
                TransactionId = dto.TransactionId,
                Amount = dto.Amount,
                ProviderId = dto.ProviderId,
                ProductTypeId = dto.ProductTypeId,
                PaymentModeId = dto.PaymentModeId
            });

            return Ok(new { success = true });
        }

        // ================================
        // PAYOUT INIT (HOLD)
        // ================================
        [HttpPost("payout/initiate")]
        public async Task<IActionResult> PayoutInitiate(PayoutRequestInit req)
        {
            string txnId = Guid.NewGuid().ToString("N").ToUpper();

            decimal total = req.Amount + req.FeeAmount;

            await _service.CheckDailyPayoutLimit(req.UserId, total);

            // HOLD WALLET
            var holdTxn = await _service.HoldAsync(
                req.UserId,
                total,
                "PAYOUT",
                txnId,
                "Payout Hold");

         

            await _service.PayoutInitAsync(new PayoutRequest
            {
                UserId = req.UserId,
                BeneficiaryId = req.BeneficiaryId,
                Amount = req.Amount,
                FeeAmount = req.FeeAmount,
                Mode = req.Mode,
                TPin = req.TPin,
                TransactionId = txnId
            });

            return Ok(new
            {
                success = true,
                txnId = txnId,
                holdTxnId = holdTxn
            });
        }

        // ================================
        // PAYOUT COMPLETE
        // ================================
        [HttpPost("payout/complete")]
        public async Task<IActionResult> PayoutComplete(
            PayoutCompletionDto dto)
        {
            decimal total = dto.Amount + dto.FeeAmount;

            if (dto.Success)
            {
                await _service.FinalizeAsync(
                    dto.UserId,
                    total,
                    "PAYOUT",
                    dto.TransactionId,
                    dto.HoldTxnId,
                    "Success");
            }
            else
            {
                await _service.ReleaseAsync(
                    dto.UserId,
                    total,
                    "PAYOUT",
                    dto.TransactionId,
                    dto.HoldTxnId,
                    "Failed");
            }

            return Ok(new { success = true });
        }


        // ================================
        //  Wallet Transfer
        // ================================
        [HttpPost("WalletTransfer")]
        public async Task<IActionResult> WalletTransfer(WalletTransferInit req)
        {
            var response = await _service.WalletTransfer(req);
            return Ok(new { success = true });
        }

        // ================================
        // BENEFICIARY MANAGEMENT
        // ================================

        [HttpPost("CreateBeneficiary")]
        public async Task<IActionResult> CreateBeneficiary([FromBody] Beneficiary req)
        {
            var result = await _service.CreateBeneficiary(req);
            return Ok(new { success = true });
        }

        // ================================
        // beneficiary verification is a critical step before allowing payouts to that beneficiary.
        // ================================

        [HttpPost("VerifyBeneficiary/{Id}")]
        public async Task<IActionResult> VerifyBeneficiary(int Id)
        {
            var result = await _service.VerifyBeneficiary(Id);
            return Ok(new { success = true });
        }

        // ================================
        //  Beneficiary List for a User (CRITICAL FOR PAYOUTS - USER MUST SELECT BENEFICIARY BEFORE PAYOUT)
        // ================================

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

        // ================================
        //  Wallet Ledger Report (CRITICAL FOR USER TRANSACTION HISTORY AND DISPUTE RESOLUTION)
        // ================================
        [HttpGet]
        [Route("GetLedgerReport/{FromDate}/{ToDate}/{TransactionType}/{UserId}")]
        public async Task<IActionResult> GetLedgerReport(DateTime FromDate, DateTime ToDate, int TransactionType, int UserId)
        {
            try
            {
                var data = await _service.GetLedgerReport(FromDate, ToDate, TransactionType, UserId);
                return Ok(data);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}

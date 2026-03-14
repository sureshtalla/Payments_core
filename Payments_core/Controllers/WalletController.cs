using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payments_core.Models;
using Payments_core.Models.Settings;
using Payments_core.Services.Security;
using Payments_core.Services.WalletService;
using System.Text.Json;

namespace Payments_core.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/wallets")]
    public class WalletController : Controller
    {
        private readonly IWalletService _service;
        private readonly AuditService _audit;
        private readonly IdempotencyService _idempotency;
        private readonly PaymentSettings _settings;

        public WalletController(
        IWalletService service,
        AuditService audit,
        IdempotencyService idempotency,
        PaymentSettings settings
        )
        {
        _service = service;
        _audit = audit;
        _idempotency = idempotency;
            _settings = settings;
        }

        // ================================
        // PAYIN INIT
        // ================================
        [HttpPost("WalletLoad")]
        public async Task<IActionResult> PayinInitiate([FromBody] WalletLoadInit req)
        {
            if (req == null || req.Amount <= 0)
                return BadRequest("Invalid request");

            req.TransactionId = Guid.NewGuid().ToString("N").ToUpper();

            await _service.WalletLoad(req);

            return Ok(new
            {
                success = true,
                txnId = req.TransactionId
            });
        }

        // ================================
        // PAYIN SUCCESS
        // ================================
        [HttpPost("payin/success")]
        public async Task<IActionResult> PayinSuccess([FromBody] WalletLoadSuccessDto dto)
        {
            if (dto == null)
                return BadRequest("Invalid request");

            await _service.UpdateWalletLoadStatus(
                dto.UserId,
                dto.TransactionId,
                1,
                "SUCCESS");

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
        public async Task<IActionResult> PayoutInitiate([FromBody] PayoutRequestInit req)
        {
            if (req == null || req.Amount <= 0)
                return BadRequest("Invalid payout request");

            string txnId = Guid.NewGuid().ToString("N").ToUpper();

            decimal total = req.Amount + req.FeeAmount;
            var key = Request.Headers["Idempotency-Key"].FirstOrDefault();

            if (string.IsNullOrWhiteSpace(key))
                return BadRequest("Missing Idempotency-Key header");

            var valid = await _idempotency.ValidateRequest(
                key,
                "PAYOUT_INIT");

            if (!valid)
                return BadRequest("Duplicate request");

            await _service.CheckDailyPayoutLimit(req.UserId, total);

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
                txnId,
                holdTxnId = holdTxn
            });
        }

        // ================================
        // PAYOUT COMPLETE
        // ================================
        [HttpPost("payout/complete")]
        public async Task<IActionResult> PayoutComplete([FromBody] PayoutCompletionDto dto)
        {
            if (dto == null)
                return BadRequest("Invalid request");

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
        // WALLET TRANSFER
        // ================================
        [HttpPost("WalletTransfer")]
        public async Task<IActionResult> WalletTransfer(
            [FromBody] WalletTransferInit req)
        {
            if (req == null || req.Amount <= 0)
                return BadRequest("Invalid request");

            var key = Request.Headers["Idempotency-Key"].ToString();

            var valid = await _idempotency.ValidateRequest(
                key,
                "WALLET_TRANSFER");

            if (!valid)
                return BadRequest("Duplicate request");

            await _service.WalletTransfer(req);

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

            await _audit.InsertAuditLog(
                req.FromUserId,
                "WALLET_TRANSFER",
                "wallet",
                req.ToUserId,
                JsonSerializer.Serialize(req),
                ip
            );

            return Ok(new { success = true });
        }
        // ================================
        // CREATE PAYIN (PG ROUTING)
        // ================================
        [HttpPost("payin/create")]
        public async Task<IActionResult> CreatePayin(
            [FromBody] PayinCreateRequest req)
        {
            if (req == null || req.Amount <= 0)
                return BadRequest("Invalid amount");

            var key = Request.Headers["Idempotency-Key"].FirstOrDefault();

            if (string.IsNullOrWhiteSpace(key))
                return BadRequest("Missing Idempotency-Key header");

            var valid = await _idempotency.ValidateRequest(
                key,
                "PAYIN_CREATE");

            if (!valid)
                return BadRequest("Duplicate request");

            // Redirect URL to Angular payment callback page
            string callbackUrl = _settings.WebhookUrl;

            var requestId =
                await _service.CreatePayinTransaction(
                    req.UserId,
                    req.MerchantId,
                    req.Amount,
                    callbackUrl);

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

            await _audit.InsertAuditLog(
                req.UserId,
                "PAYIN_CREATE",
                "pg_transaction",
                null,
                JsonSerializer.Serialize(new
                {
                    requestId,
                    merchantId = req.MerchantId,
                    amount = req.Amount
                }),
                ip
            );

            return Ok(new
            {
                success = true,
                requestId
            });
        }

        // ================================
        // CREATE PAYOUT
        // ================================
        [HttpPost("payout/create")]
        public async Task<IActionResult> CreatePayout(
            [FromBody] PayoutRequestInit req)
        {
            var key = Request.Headers["Idempotency-Key"].ToString();

            var valid = await _idempotency.ValidateRequest(
                key,
                "PAYOUT_CREATE");

            if (!valid)
                return BadRequest("Duplicate request");

            var txnId = await _service.CreatePayoutOrder(
                req.UserId,
                (int)req.BeneficiaryId,
                req.Amount,
                req.FeeAmount,
                req.Mode.ToString(),
                req.TPin);

            return Ok(new
            {
                success = true,
                txnId
            });
        }

        // ================================
        // CREATE BENEFICIARY
        // ================================
        [HttpPost("CreateBeneficiary")]
        public async Task<IActionResult> CreateBeneficiary([FromBody] Beneficiary req)
        {
            await _service.CreateBeneficiary(req);
            return Ok(new { success = true });
        }

        // ================================
        // VERIFY BENEFICIARY
        // ================================
        [HttpPost("VerifyBeneficiary/{Id}")]
        public async Task<IActionResult> VerifyBeneficiary(int Id)
        {
            await _service.VerifyBeneficiary(Id);
            return Ok(new { success = true });
        }

        // ================================
        // GET BENEFICIARIES
        // ================================
        [HttpGet("GetBeneficiaries/{UserId}")]
        public async Task<IActionResult> GetBeneficiaries(int UserId)
        {
            var data = await _service.GetBeneficiaries(UserId);
            return Ok(data);
        }

        // ================================
        // WALLET LEDGER REPORT
        // ================================
        [HttpGet("GetLedgerReport/{FromDate}/{ToDate}/{TransactionType}/{UserId}")]
        public async Task<IActionResult> GetLedgerReport(
            DateTime FromDate,
            DateTime ToDate,
            int TransactionType,
            int UserId)
        {
            var data = await _service.GetLedgerReport(
                FromDate,
                ToDate,
                TransactionType,
                UserId);

            return Ok(data);
        }

        // ================================================
        // WALLET BALANCE
        // ================================================
        [HttpGet("balance/{userId}")]
        public async Task<IActionResult> GetWalletBalance(long userId)
        {
            var balance = await _service.GetWalletBalance(userId);

            if (balance == null)
                return NotFound("Wallet not found");

            return Ok(balance);
        }

        // =======================================
        // GET PAYMENT STATUS
        // =======================================
        [HttpGet("status/{requestId}")]
        public async Task<IActionResult> GetStatus(string requestId)
        {
            var txn = await _service.GetPaymentStatus(requestId);

            if (txn == null)
                return NotFound(new
                {
                    success = false,
                    message = "Transaction not found"
                });

            return Ok(new
            {
                success = true,
                status = txn.status,
                amount = txn.amount,
                requestId = txn.request_id
            });
        }
    }
}
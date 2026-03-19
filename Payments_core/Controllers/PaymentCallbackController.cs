using Microsoft.AspNetCore.Mvc;
using Payments_core.Services.WalletService;
using Payments_core.Services.Security;
using System.Text.Json;
using Payments_core.Services.FailureQueue;

namespace Payments_core.Controllers
{
    [ApiController]
    [Route("api/payments")]
    public class PaymentCallbackController : ControllerBase
    {
        private readonly IWalletService _wallet;
        private readonly WebhookSignatureService _signature;
        private readonly AuditService _audit;
        private readonly FailureService _failure;

        public PaymentCallbackController(
            IWalletService wallet,
            WebhookSignatureService signature,
            AuditService audit,
            FailureService failure)
        {
            _wallet = wallet;
            _signature = signature;
            _audit = audit;
            _failure = failure;
        }

        [HttpPost("callback")]
        public async Task<IActionResult> ProviderCallback()
        {
            string payload;
            string requestId = "";
            string status = "";
            string signatureHeader = "";

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

            // ── Read Headers ───────────────────────────────────────────────
            var headers = Request.Headers.ToDictionary(
                h => h.Key,
                h => h.Value.ToString());

            // ── Read Payload ───────────────────────────────────────────────
            using (var reader = new StreamReader(Request.Body))
            {
                payload = await reader.ReadToEndAsync();
            }

            if (string.IsNullOrWhiteSpace(payload))
                return BadRequest("Empty payload");

            // ── Detect Provider + Load Real Secret from Config ─────────────
            int providerId = 1;
            string secret = "";

            if (headers.TryGetValue("X-Cashfree-Signature", out signatureHeader!))
            {
                providerId = 2;
                secret = _signature.GetCashfreeSecret(); // reads from appsettings / env var
            }
            else if (headers.TryGetValue("X-Razorpay-Signature", out signatureHeader!))
            {
                providerId = 3;
                secret = _signature.GetRazorpaySecret(); // reads from appsettings / env var
            }

            // ── Signature Verification ─────────────────────────────────────
            if (!string.IsNullOrEmpty(secret))
            {
                if (!_signature.VerifyHmac(payload, signatureHeader, secret))
                    return Unauthorized("Invalid webhook signature");
            }

            // ── Parse JSON ─────────────────────────────────────────────────
            try
            {
                var json = JsonDocument.Parse(payload);

                if (json.RootElement.TryGetProperty("request_id", out var rid))
                    requestId = rid.GetString() ?? "";

                if (json.RootElement.TryGetProperty("status", out var st))
                    status = st.GetString() ?? "";
            }
            catch
            {
                return BadRequest("Invalid callback payload");
            }

            if (string.IsNullOrEmpty(requestId))
                return BadRequest("request_id missing");

            if (string.IsNullOrEmpty(status))
                return BadRequest("status missing");

            // ── Log Webhook ────────────────────────────────────────────────
            var logId = await _wallet.InsertWebhookLog(
                providerId,
                "PAYIN_CALLBACK",
                JsonSerializer.Serialize(headers),
                payload);

            try
            {
                // ── Fetch Transaction ──────────────────────────────────────
                var txn = await _wallet.GetPgTransaction(requestId);

                await _wallet.UpdateWebhookTxnLink(logId, txn?.id);

                if (txn == null)
                {
                    await _wallet.UpdateWebhookStatus(logId, "FAILED");
                    return NotFound("Transaction not found");
                }

                // ── Idempotency Check ──────────────────────────────────────
                if (txn.status == "SUCCESS")
                {
                    await _wallet.UpdateWebhookStatus(logId, "IGNORED");
                    return Ok("Already processed");
                }

                // ── Update PG Status ───────────────────────────────────────
                await _wallet.UpdatePgTransactionStatus(requestId, status, payload);

                // ── Wallet Credit Protection ───────────────────────────────
                if (status == "SUCCESS")
                {
                    var credited = await _wallet.IsWalletCredited(requestId);
                    if (!credited)
                        await _wallet.ProcessPayinWalletCredit(requestId);
                }

                // ── Update Webhook Status ──────────────────────────────────
                await _wallet.UpdateWebhookStatus(logId, "PROCESSED");

                // ── Audit Log ──────────────────────────────────────────────
                await _audit.InsertAuditLog(
                    txn.created_by_user,
                    "PAYIN_CALLBACK",
                    "pg_transaction",
                    txn.id,
                    payload,
                    ip);

                return Ok(new { success = true, requestId });
            }
            catch (Exception ex)
            {
                await _wallet.UpdateWebhookStatus(logId, "FAILED");

                await _failure.LogFailure(
                    requestId,
                    "PAYIN_CALLBACK",
                    payload,
                    ex.Message);

                // Generic message to client — never expose ex.Message in production
                return StatusCode(500, new
                {
                    success = false,
                    message = "Callback processing failed. Please retry.",
                    traceId = HttpContext.TraceIdentifier
                });
            }
        }
    }
}
using Microsoft.AspNetCore.Mvc;
using Payments_core.Services.Payments;
using Payments_core.Services.WalletService;

namespace Payments_core.Controllers
{
    [ApiController]
    [Route("payments")]
    public class PaymentRedirectController : Controller
    {
        private readonly PaymentRouterService _router;
        private readonly IWalletService _wallet;
        private readonly PgRetryService _attempt;
        public PaymentRedirectController(
            PaymentRouterService router,
            IWalletService wallet,
            PgRetryService attempt  )
        {
            _router = router;
            _wallet = wallet;
            _attempt = attempt;
        }

        [HttpGet("redirect/{requestId}")]
        public async Task<IActionResult> RedirectToPg(string requestId)
        {
            var txn = await _wallet.GetPgTransaction(requestId);

            if (txn == null)
                return NotFound("Transaction not found");

            // Prevent multiple redirects
            if (txn.status != "INITIATED")
                return BadRequest("Invalid transaction state");

            // Expire transaction after 30 minutes
            DateTime created = txn.created_at;

            if (created < DateTime.UtcNow.AddMinutes(-30))
                return BadRequest("Transaction expired");

            var gateways = await _router.GetGateways("PAYIN");

            int attempt = 1;

            foreach (var (gateway, provider) in gateways)
            {
                try
                {
                    await _attempt.LogAttempt(
                        requestId,
                        provider.id,
                        attempt,
                        "INITIATED",
                        "");

                    var url = await gateway.CreatePayin(
                        requestId,
                        txn.amount,
                        txn.callback_url,
                        provider);

                    return Redirect(url);
                }
                catch (Exception ex)
                {
                    await _attempt.LogAttempt(
                        requestId,
                        provider.id,
                        attempt,
                        "FAILED",
                        ex.Message);

                    attempt++;
                    continue;
                }
            }

            return BadRequest("All PG failed");
        }
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payments_core.Services.BankService;

namespace Payments_core.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/banks")]
    public class BankController : Controller
    {
        private readonly IBankService _service;

        public BankController(IBankService service)
        {
            _service = service;
        }

        // ============================================================
        // GET ALL BANKS
        // GET /api/banks/all
        // ============================================================
        [HttpGet("all")]
        public async Task<IActionResult> GetAllBanks()
        {
            var data = await _service.GetAllBanksAsync();
            return Ok(data);
        }

        // ============================================================
        // GET BANKS FOR PAYOUT DROPDOWN
        // GET /api/banks/payout
        // Returns: PUBLIC + PRIVATE + SMALL_FINANCE + PAYMENTS only
        // ============================================================
        [HttpGet("payout")]
        public async Task<IActionResult> GetPayoutBanks()
        {
            var data = await _service.GetPayoutBanksAsync();
            return Ok(data);
        }

        // ============================================================
        // GET BANKS BY TYPE
        // GET /api/banks/type/{bankType}
        // bankType: PUBLIC | PRIVATE | SMALL_FINANCE | PAYMENTS | FOREIGN | RRB | COOPERATIVE
        // ============================================================
        [HttpGet("type/{bankType}")]
        public async Task<IActionResult> GetBanksByType(string bankType)
        {
            var data = await _service.GetBanksByTypeAsync(bankType.ToUpper());
            return Ok(data);
        }

        // ============================================================
        // SEARCH BANKS (typeahead)
        // GET /api/banks/search?q=hdfc
        // ============================================================
        [HttpGet("search")]
        public async Task<IActionResult> SearchBanks([FromQuery] string q)
        {
            if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
                return BadRequest(new { success = false, message = "Search term must be at least 2 characters." });

            var data = await _service.SearchBanksAsync(q);
            return Ok(data);
        }

        // ============================================================
        // AUTO-DETECT BANK FROM IFSC CODE
        // GET /api/banks/ifsc/{ifscCode}
        // Example: /api/banks/ifsc/HDFC0001234  → returns HDFC Bank
        // ============================================================
        [HttpGet("ifsc/{ifscCode}")]
        public async Task<IActionResult> GetBankByIFSC(string ifscCode)
        {
            if (string.IsNullOrWhiteSpace(ifscCode) || ifscCode.Length < 4)
                return BadRequest(new { success = false, message = "Invalid IFSC code." });

            var data = await _service.GetBankByIFSCPrefixAsync(ifscCode);

            if (data == null)
                return NotFound(new { success = false, message = "Bank not found for this IFSC." });

            return Ok(data);
        }
    }
}

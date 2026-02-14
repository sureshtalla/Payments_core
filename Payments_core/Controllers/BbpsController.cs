using Microsoft.AspNetCore.Mvc;
using Payments_core.Services.BBPSService;
using Payments_core.Models.BBPS;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.Json;
using Payments_core.Services.BBPSService.Repository;


namespace Payments_core.Controllers
{
    [ApiController]
    [Route("api/bbps")]
    public class BbpsController : ControllerBase
    {
        private readonly IBbpsService _bbps;
        private readonly IBbpsRepository _repo;
        public BbpsController(IBbpsService bbps,
        IBbpsRepository repo)
        {
            _bbps = bbps;
            _repo = repo;
        }

        // -------------------------------------------------
        // FETCH BILL
        // -------------------------------------------------
        [HttpPost("fetch")]
        public async Task<IActionResult> FetchBill([FromBody] FetchReq req)
        {
            try
            {
                // 🔐 Backend-generated agent info (NPCI compliant)
                var agentDeviceInfo = new AgentDeviceInfo
                {
                    Ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1",
                    InitChannel = "AGT",              // As per BBPS
                    Mac = Environment.MachineName     // Logical identifier
                };

                var res = await _bbps.FetchBill(
                    req.UserId,
                    req.BillerId,
                    req.Inputs,
                    agentDeviceInfo,
                    req.CustomerInfo
                );

                if (res.ResponseCode != "000")
                    return BadRequest(res);

                return Ok(res);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    code = "FETCH_ERROR",
                    message = ex.Message
                });
            }
        }

        // -------------------------------------------------
        // PAY BILL
        // -------------------------------------------------
        [HttpPost("pay")]
        public async Task<IActionResult> PayBill([FromBody] PayReq req)
        {
            try
            {
                var res = await _bbps.PayBill(
                    req.UserId,
                    req.BillerId,
                    req.BillRequestId,
                    req.InputParams,
                    req.BillerResponse,
                    req.AdditionalInfo,
                    req.Amount,
                    req.AmountTag,
                    req.Tpin,
                    req.CustomerMobile,
                    req.RequestId
                );

                Console.WriteLine($"[PAY][CTRL] ResponseCode={res.ResponseCode}, TxnRefId={res.TxnRefId}");

                // Always return 200 for BBPS business responses
                return Ok(res);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PAY][CTRL][ERROR] {ex}");

                return StatusCode(500, new
                {
                    success = false,
                    code = "PAY_ERROR",
                    message = ex.Message
                });
            }
        }


        // -------------------------------------------------
        // BILL VALIDATION
        // -------------------------------------------------
        [HttpPost("validate")]
        public async Task<IActionResult> ValidateBill(
            [FromBody] FetchReq req)
        {
            var res = await _bbps.ValidateBill(
                req.BillerId,
                req.Inputs
            );

            if (!res.IsValid)
                return BadRequest(res);

            return Ok(res);
        }

        // -------------------------------------------------
        // CHECK STATUS
        // -------------------------------------------------
        [HttpGet("status/{txnRefId}")]
        public async Task<IActionResult> Status(string txnRefId)
        {
            try
            {
                // Get requestId + billRequestId from DB
                var requestId = await _repo.GetRequestIdByTxnRef(txnRefId);

                if (string.IsNullOrEmpty(requestId))
                    return NotFound("Transaction not found");

                // billRequestId is optional now
                var billRequestId = await _repo.GetBillRequestIdByTxnRef(txnRefId);

                var res = await _bbps.CheckStatus(
                    requestId,
                    txnRefId,
                    billRequestId
                );

                return Ok(res);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    code = "STATUS_ERROR",
                    message = ex.Message
                });
            }
        }

        // -------------------------------------------------
        // SYNC BILLERS
        // -------------------------------------------------
        [HttpPost("sync-billers")]
        public async Task<IActionResult> SyncBillers()
        {
            try
            {
                await _bbps.SyncBillers();
                return Ok(new
                {
                    success = true,
                    message = "BBPS billers synced successfully"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    code = "SYNC_ERROR",
                    message = ex.Message
                });
            }
        }

        [HttpGet("billers")]
        public async Task<IActionResult> GetBillers([FromQuery] string category)
        {
            if (string.IsNullOrWhiteSpace(category))
                return BadRequest("Category is required");

            var billers = await _bbps.GetBillersByCategory(category);
            return Ok(billers);
        }

        [HttpGet("biller-params/{billerId}")]
        public async Task<IActionResult> GetBillerParams(string billerId)
        {
            var result = await _bbps.GetBillerParams(billerId);
            return Ok(result);
        }

        // -------------------------------------------------
        // GET BILLER BY ID (WITH supportsAdhoc)
        // -------------------------------------------------
        [HttpGet("biller/{billerId}")]
        public async Task<IActionResult> GetBillerById(string billerId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(billerId))
                    return BadRequest("BillerId is required");

                var biller = await _bbps.GetBillerById(billerId);

                if (biller == null)
                    return NotFound("Biller not found");

                return Ok(biller);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        

    }


    // -------------------------------------------------
    // REQUEST DTOs
    // -------------------------------------------------
    //public record FetchReq(
    //    long UserId,
    //    string BillerId,
    //    Dictionary<string, string> Inputs
    //);

 
    public class FetchReq
    {
      public long UserId { get; set; }
      public string BillerId { get; set; }
      public Dictionary<string, string> Inputs { get; set; }
      public CustomerInfo CustomerInfo { get; set; }
    }
    public class PayReq
    {
        public long UserId { get; set; }
        public string BillerId { get; set; } = string.Empty;
        public Dictionary<string, string>? InputParams { get; set; }
        public string? BillRequestId { get; set; }
        public JsonElement? BillerResponse { get; set; }

        public JsonElement? AdditionalInfo { get; set; }

        public decimal Amount { get; set; }
        public string Tpin { get; set; } = string.Empty;
        public string CustomerMobile { get; set; } = string.Empty;

        public string? RequestId { get; set; }

        public string? AmountTag { get; set; }
    }
}
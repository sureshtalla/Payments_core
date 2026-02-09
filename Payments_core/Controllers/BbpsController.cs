using Microsoft.AspNetCore.Mvc;
using Payments_core.Services.BBPSService;
using Payments_core.Models.BBPS;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Payments_core.Controllers
{
    [ApiController]
    [Route("api/bbps")]
    public class BbpsController : ControllerBase
    {
        private readonly IBbpsService _bbps;

        public BbpsController(IBbpsService bbps)
        {
            _bbps = bbps;
        }

        // -------------------------------------------------
        // FETCH BILL
        // -------------------------------------------------
        [HttpPost("fetch")]
        public async Task<IActionResult> FetchBill([FromBody] FetchReq req)
        {
            try
            {
                var res = await _bbps.FetchBill(
                    req.UserId,
                    req.BillerId,
                    req.Inputs
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
                    req.Amount,
                    req.Tpin
                );

                if (res.ResponseCode == "999")
                    return Accepted(res);

                if (res.ResponseCode != "000")
                    return BadRequest(res);

                return Ok(res);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    code = "PAY_ERROR",
                    message = ex.Message
                });
            }
        }

        // -------------------------------------------------
        // CHECK STATUS
        // -------------------------------------------------
        [HttpGet("status/{txnRefId}/{billRequestId}")]
        public async Task<IActionResult> Status(
            string txnRefId,
            string billRequestId)
        {
            try
            {
                var res = await _bbps.CheckStatus(txnRefId, billRequestId);
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

    }



    // -------------------------------------------------
    // REQUEST DTOs
    // -------------------------------------------------
    public record FetchReq(
        long UserId,
        string BillerId,
        Dictionary<string, string> Inputs
    );

    public record PayReq(
        long UserId,
        string BillerId,
        string BillRequestId,
        decimal Amount,
        string Tpin
    );
}
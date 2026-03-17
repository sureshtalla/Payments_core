using Microsoft.AspNetCore.Mvc;
using Payments_core.Models.KycVerification;
using Payments_core.Services.KycVerificationService;

[Route("api/kyc")]
public class KycVerificationController : Controller
{
    private readonly IKycVerificationService _service;

    public KycVerificationController(IKycVerificationService service)
    {
        _service = service;
    }

    [HttpPost("verify-pan")]
    public async Task<IActionResult> VerifyPan([FromBody] PanVerifyRequest req)
    {
        Console.WriteLine("PAN VERIFY REQUEST");
        Console.WriteLine($"UserId: {req.UserId}");
        Console.WriteLine($"PAN: {req.Pan}");

        var result = await _service.VerifyPan(req.UserId, req.Pan);

        return Ok(result);
    }

    [HttpPost("aadhaar/start")]
    public async Task<IActionResult> StartAadhaarVerification([FromBody] AadhaarVerifyRequest req)
    {
        Console.WriteLine("==== AADHAAR VERIFY START ====");
        Console.WriteLine($"UserId: {req.UserId}");
        Console.WriteLine($"Aadhaar: {req.Aadhaar}");

        try
        {
            var result = await _service.StartAadhaarVerification(req.UserId, req.Aadhaar);

            Console.WriteLine("==== DIGILOCKER LINK GENERATED ====");
            Console.WriteLine(result);

            return Ok(result);
        }
        catch (Exception ex)
        {
            Console.WriteLine("==== AADHAAR START ERROR ====");
            Console.WriteLine($"Message: {ex.Message}");
            Console.WriteLine($"InnerException: {ex.InnerException?.Message}");
            Console.WriteLine($"StackTrace: {ex.StackTrace}");

            return StatusCode(500, new
            {
                success = false,
                code = "AADHAAR_START_ERROR",
                message = ex.Message,
                inner = ex.InnerException?.Message,
                step = "StartAadhaarVerification"
            });
        }
    }

    [HttpGet("aadhaar/complete/{userId}/{verificationId}")]
    public async Task<IActionResult> CompleteAadhaar(long userId, string verificationId)
    {
        Console.WriteLine("==== AADHAAR COMPLETE VERIFY ====");
        Console.WriteLine($"UserId: {userId}");
        Console.WriteLine($"VerificationId: {verificationId}");

        var result = await _service.CompleteAadhaarVerification(userId, verificationId);

        Console.WriteLine("==== AADHAAR VERIFY RESULT ====");
        Console.WriteLine(result);

        return Ok(result);
    }

    [HttpPost("verify-bank")]
    public async Task<IActionResult> VerifyBank([FromBody] BankVerifyRequest req)
    {
        Console.WriteLine("==== BANK VERIFY REQUEST ====");
        Console.WriteLine($"BeneficiaryId: {req.BeneficiaryId}");

        try
        {
            var result = await _service.VerifyBank(req.BeneficiaryId);
            Console.WriteLine($"BANK VERIFY RESULT: {result}");
            return Ok(result);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"BANK VERIFY ERROR: {ex.Message}");

            var msg = ex.Message.ToLower();

            if (msg.Contains("not found"))
                return NotFound(new { success = false, message = ex.Message });

            if (msg.Contains("credentials"))
                return StatusCode(503, new
                {
                    success = false,
                    message = "Verification service not configured. Please contact support."
                });

            return StatusCode(502, new
            {
                success = false,
                message = "Bank verification service unavailable. Please try again later.",
                detail = ex.Message
            });
        }
    }
    [HttpGet("verify-bank-status")]
    public async Task<IActionResult> GetBankVerificationStatus([FromQuery] string referenceId, [FromQuery] int beneficiaryId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(referenceId))
                return BadRequest(new { success = false, message = "referenceId is required" });

            var result = await _service.GetBankVerificationStatus(referenceId, beneficiaryId);

            return Ok(result);
        }
        catch (Exception ex)
        {
            Console.WriteLine("BANK VERIFY STATUS ERROR: " + ex.Message);

            return StatusCode(500, new
            {
                success = false,
                message = "Bank verification status check failed.",
                detail = ex.Message
            });
        }
    }
}
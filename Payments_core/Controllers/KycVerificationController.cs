using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payments_core.Models.KycVerification;
using Payments_core.Services.KycVerificationService;

//[Authorize]
//[ApiController]
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
    public async Task<IActionResult> StartAadhaarVerification(AadhaarVerifyRequest req)
    {
        Console.WriteLine("==== AADHAAR VERIFY START ====");
        Console.WriteLine($"UserId: {req.UserId}");
        Console.WriteLine($"Aadhaar: {req.Aadhaar}");

        var result = await _service.StartAadhaarVerification(req.UserId, req.Aadhaar);

        Console.WriteLine("==== DIGILOCKER LINK GENERATED ====");
        Console.WriteLine(result);

        return Ok(result);
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
    public async Task<IActionResult> VerifyBank(BankVerifyRequest req)
    {
        Console.WriteLine("==== BANK VERIFY REQUEST ====");
        Console.WriteLine($"BeneficiaryId: {req.BeneficiaryId}");

        bool ok = await _service.VerifyBank(req.BeneficiaryId);

        Console.WriteLine($"BANK VERIFY RESULT: {ok}");

        return Ok(new { verified = ok });
    }
}
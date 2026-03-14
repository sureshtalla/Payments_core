using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payments_core.Models.KycVerification;
using Payments_core.Services.KycVerificationService;


[Authorize]
[ApiController]
[Route("api/kyc")]
public class KycVerificationController : Controller
{
    private readonly IKycVerificationService _service;

    public KycVerificationController(IKycVerificationService service)
    {
        _service = service;
    }

    [HttpPost("verify-pan")]
    public async Task<IActionResult> VerifyPan(PanVerifyRequest req)
    {
        var result = await _service.VerifyPan(
            req.UserId,
            req.Pan);

        return Ok(result);
    }

    [HttpPost("aadhaar/start")]
    public async Task<IActionResult> StartAadhaarVerification(AadhaarVerifyRequest req)
    {
        var result = await _service.StartAadhaarVerification(
            req.UserId,
            req.Aadhaar);

        return Ok(result);
    }

    [HttpGet("aadhaar/complete/{userId}/{verificationId}")]
    public async Task<IActionResult> CompleteAadhaar(
      long userId,
      string verificationId)
    {
        var result = await _service.CompleteAadhaarVerification(
            userId,
            verificationId);

        return Ok(result);
    }

    [HttpPost("verify-bank")]
    public async Task<IActionResult> VerifyBank(BankVerifyRequest req)
    {
        bool ok = await _service.VerifyBank(req.BeneficiaryId);

        return Ok(new { verified = ok });
    }
}
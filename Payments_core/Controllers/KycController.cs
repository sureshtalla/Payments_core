using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Authorize]
[ApiController]
[Route("api/kyc")]
public class KycController : ControllerBase
{
    [HttpGet("{userId}/{fileName}")]
    public IActionResult Download(long userId, string fileName)
    {
        var path = Path.Combine(
            Directory.GetCurrentDirectory(),
            "SecureStorage",
            "kyc",
            userId.ToString(),
            fileName
        );

        if (!System.IO.File.Exists(path))
            return NotFound();

        return PhysicalFile(path, "application/octet-stream");
    }
}
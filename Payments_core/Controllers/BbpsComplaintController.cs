using Microsoft.AspNetCore.Mvc;
using Payments_core.Services.BBPSService;

namespace Payments_core.Controllers
{
    [ApiController]
    [Route("api/bbps/complaint")]
    public class BbpsComplaintController : ControllerBase
    {
        private readonly IBbpsComplaintService _service;

        public BbpsComplaintController(IBbpsComplaintService service)
        {
            _service = service;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterComplaintRequest req)
        {
            await _service.RegisterComplaint(
                req.TxnRefId,
                req.BillerId,
                req.ComplaintType,
                req.Description
            );

            return Ok(new { success = true });
        }

        [HttpGet("track/{complaintId}")]
        public async Task<IActionResult> Track(string complaintId)
        {
            await _service.TrackComplaint(complaintId);
            return Ok(new { success = true });
        }
    }

    public class RegisterComplaintRequest
    {
        public string TxnRefId { get; set; }
        public string BillerId { get; set; }
        public string ComplaintType { get; set; }
        public string Description { get; set; }
    }
}
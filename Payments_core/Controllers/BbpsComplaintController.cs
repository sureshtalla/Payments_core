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
            var result = await _service.RegisterComplaint(
                req.TxnRefId,
                req.ComplaintType, // now this means disposition
                req.Description
            );

            return Ok(result); // 🔥 RETURN FULL RESPONSE
        }

        [HttpGet("track/{complaintId}")]
        public async Task<IActionResult> Track(string complaintId)
        {
            var result = await _service.TrackComplaint(complaintId);
            return Ok(result);
        }
    }

        public class RegisterComplaintRequest
        {
            public string TxnRefId { get; set; }
            public string ComplaintType { get; set; }
            public string Description { get; set; }
        }
    
    }
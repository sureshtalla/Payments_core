namespace Payments_core.Models.BBPS
{
    public class BbpsBillerDto
    {

        // ===== Input Parameters (Dynamic UI / Validation) =====
        public List<BbpsBillerInputDto> InputParams { get; set; }
            = new List<BbpsBillerInputDto>();
    }
}
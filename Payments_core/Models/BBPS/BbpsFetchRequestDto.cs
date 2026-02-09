namespace Payments_core.Models.BBPS
{
    public class BbpsFetchRequestDto
    {
        public string BillerId { get; set; }
        public Dictionary<string, string> InputParams { get; set; }
    }
}

namespace Payments_core.Models.BBPS
{
    public class BillerDto
    {
        public string BillerId { get; set; }
        public string BillerName { get; set; }
        public int SupportsAdhoc { get; set; }
    }
}

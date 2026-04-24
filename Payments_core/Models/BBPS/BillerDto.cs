namespace Payments_core.Models.BBPS
{
    public class BillerDto
    {
        public string BillerId { get; set; }
        public string BillerName { get; set; }
        public string Category { get; set; }
        public string FetchRequirement { get; set; }
        public string PaymentAmountExactness { get; set; }
        public int SupportsAdhoc { get; set; }
        public string BillerStatus { get; set; }
    }
}

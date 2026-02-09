namespace Payments_core.Models.BBPS
{
    public class BbpsBillerCatalog
    {
        public string BillerId { get; set; }
        public string BillerName { get; set; }
        public string ServiceCategory { get; set; }
        public string Environment { get; set; }
        public bool IsActive { get; set; }
        public bool MdmSupported { get; set; }
    }
}

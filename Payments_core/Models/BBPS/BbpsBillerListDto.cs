namespace Payments_core.Models.BBPS
{
    public class BbpsBillerListDto
    {
        public string biller_id { get; set; }
        public string biller_name { get; set; }
        public string Category { get; set; }
        public string fetch_requirement { get; set; } // MANDATORY / OPTIONAL
    }
}

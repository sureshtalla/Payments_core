namespace Payments_core.Models
{
    public class MerchantListItemDto
    {
        public long MerchantId { get; set; }
        public string MerchantName { get; set; }
        public string Mobileno { get; set; }
        public string ContactPerson { get; set; }
        public string KycStatus { get; set; }
        public string RiskCategory { get; set; }
       

    }
    public class MerchantApprovalRequest
    {
        public long MerchantId { get; set; }
        public string Action { get; set; } // APPROVE or REJECT
        public string? Remarks { get; set; }  // Required for REJECT
    }
    public class MerchantKycUpdateRequest
    {
        public long MerchantId { get; set; }
        public string KycStatus { get; set; }   // PENDING/VERIFIED/REJECTED/APPROVED
     

    }


}

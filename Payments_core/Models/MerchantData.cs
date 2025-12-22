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
        public string Status { get; set; }
        public string RoleName { get; set; }
        public long RoleId { get; set; }


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

    public class WalletLoadInit
    {
        public string? TransactionId { get; set; }
        public int UserId { get; set; }
        public int ProviderId { get; set; }
        public int ProductTypeId { get; set; }
        public int PaymentModeId { get; set; }
        public int SettlementType { get; set; }
        public decimal Amount { get; set; }
    }

    public class Beneficiary
    {
        public long UserId { get; set; }
        public required string BeneficiaryName { get; set; }
        public required string AccountNumber { get; set; }
        public required string IFSCCode { get; set; }
    }

    public class BeneficiaryDto
    {
        public long Id { get; set; }
        public required string BeneficiaryName { get; set; }
        public required string AccountNumber { get; set; }
        public required string IFSCCode { get; set; }
        // true = Verified, false = Pending
        public bool IsVerified { get; set; }
        public DateTime CreatedOn { get; set; }
    }


}

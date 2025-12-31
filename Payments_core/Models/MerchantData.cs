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

    public class PayoutRequest
    {
        public long BeneficiaryId { get; set; }
        public long UserId { get; set; }
        public decimal Amount { get; set; }
        public decimal FeeAmount { get; set; }
        public required string TPin { get; set; }
        public required PayoutMode Mode { get; set; }
        public string? TransactionId { get; set; }
        public PayoutStatus Status { get; set; }
        public string? Reason { get; set; }
    }

    public class PayoutRequestInit
    {
        public long BeneficiaryId { get; set; }
        public long UserId { get; set; }
        public decimal Amount { get; set; }
        public required string TPin { get; set; }
        public required PayoutMode Mode { get; set; }
        public decimal FeeAmount { get; set; }
    }

    public class WalletTransferInit
    {
        public long FromUserId { get; set; }
        public long ToUserId { get; set; }
        public decimal Amount { get; set; }
        public string? Reason { get; set; }
        public TransactionType TransactionType { get; set; }
        // True for Wallet Transfer and False for Wallet Adjustment
        public bool IsWalletTransfer { get; set; }
    }

    public class LedgerReport
    {
        public DateTime TransactionDate { get; set; }
        public required string TransactionType { get; set; }
        public decimal Amount { get; set; }
        public decimal Charges { get; set; }
        public decimal Commission { get; set; }
        public decimal GST { get; set; }
        public decimal TDS { get; set; }
        public decimal Balance { get; set; }
    }

    public enum PayoutMode
    {
        IMPS = 1,
        NEFT = 2,
        UPI = 3,
        BANK_TRANSFER = 4
    }

    public enum PayoutStatus
    {
        INITIATED = 1,
        PENDING = 2,
        SUCCESS = 3,
        FAILED = 4,
        REVERSED = 5
    }

    public enum TransactionType
    {
        CREDIT = 1,
        DEBIT = 2
    }

}

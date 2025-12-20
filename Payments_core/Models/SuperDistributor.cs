namespace Payments_core.Models
{
    public class SuperDistributorRequest
    {
        // USER
        public long RoleId { get; set; }
        public long? ParentUserId { get; set; }
        public required string FullName { get; set; }
        public required string BusinessName { get; set; }
        public required string Email { get; set; }
        public required string Mobile { get; set; }
        public required string Password { get; set; }

        // MERCHANT
        //public required string LegalName { get; set; }
       // public required string TradeName { get; set; }
        //public required string BusinessType { get; set; }
        //public required string Category { get; set; }
        //public required string WebsiteUrl { get; set; }
        //public required string SettlementProfile { get; set; }
        //public required string EnabledProducts { get; set; }

        // KYC PROFILE
        public required string PanNumber { get; set; }
        public required string AadhaarLast4 { get; set; }
        //public required string Gstin { get; set; }
        public required string Address1 { get; set; }
        //public required string Address2 { get; set; }
        //public required string City { get; set; }
        //public required string State { get; set; }
        //public required string Pincode { get; set; }
        //public required string BankAccountNo { get; set; }
        //public required string BankIfsc { get; set; }
        public bool isAuthorVerified { get; set; }
        // DOCUMENT URLS (after upload)
        public required string PanUrl { get; set; }
        public required string AadhaarUrl { get; set; }
       public int user_id { get; set; }

        public int super_user_id { get; set; }

        //public required string GstUrl { get; set; }
        //public required string BankUrl { get; set; }
    }

    public class SuperDistributorResponse
    {
        public long UserId { get; set; }
        public long MerchantId { get; set; }
        public string Message { get; set; } = "SuperDistributor created successfully";
    }

    public class User
    {
        public long Id { get; set; }
        public long RoleId { get; set; }
        public long? ParentUserId { get; set; }
        public required string FullName { get; set; }
        public required string BusinessName { get; set; }
        public required string Email { get; set; }
        public required string Mobile { get; set; }
        public required string UserCode { get; set; }
        public required string Tinno { get; set; }
        public required string Status { get; set; }
    }
    public class Merchant
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public required string LegalName { get; set; }
        public required string TradeName { get; set; }
        public required string BusinessType { get; set; }
        public required string Category { get; set; }
        public required string WebsiteUrl { get; set; }
        public required string SettlementProfile { get; set; }
        public required string EnabledProducts { get; set; }
    }
    public class KycProfile
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public required string Pan { get; set; }
        public required string Aadhaar4 { get; set; }
        public required string Gstin { get; set; }
        public required string BusinessType { get; set; }
        public required string Address1 { get; set; }
        public required string Address2 { get; set; }
        public required string City { get; set; }
        public required string State { get; set; }
        public required string Pincode { get; set; }
        public required string BankAccountNo { get; set; }
        public required string BankIfsc { get; set; }
        public required string RiskLevel { get; set; }
        public required string Status { get; set; }
        public required string Notes { get; set; }
       
        
    }
    public class KycDocument
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public required string DocType { get; set; }
        public required string FilePath { get; set; }
    }
    public class SuperDistributorFullResponse
    {
        public User? User { get; set; }
        public Merchant? Merchant { get; set; }
        public KycProfile? Profile { get; set; }
        public List<KycDocument>? Documents { get; set; }
    }


    public class SuperDistributorCardDto
    {
        public int UserId { get; set; }
        public string FullName { get; set; }
        public string BusinessName { get; set; }
        public string Email { get; set; }
        public string BusinessAddress { get; set; }
        public string Mobile { get; set; }
        public string KycStatus { get; set; }   // Pending / Verified / Rejected
        public string Status { get; set; }      // Active / Suspended / Inactive
    }

    public class uperDistributorProfileDto
    {
        public long UserId { get; set; }
        public int RoleId { get; set; }
        public long ParentUserId { get; set; }
        public string FullName { get; set; }
        public string BusinessName { get; set; }
        public string Email { get; set; }
        public string Mobile { get; set; }
        public string Password { get; set; }
        public string PanNumber { get; set; }
        public string AadhaarLast4 { get; set; }
        public string Address1 { get; set; }
        public bool IsAuthorVerified { get; set; }
        public string PanUrl { get; set; }
        public string AadhaarUrl { get; set; }
        public long SuperUserId { get; set; }
    }

}

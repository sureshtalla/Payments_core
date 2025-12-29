namespace Payments_core.Models
{
    public class Roles
    {
        public int RoleID { get; set; }
        public required  string RoleName { get; set; }
        public required  string Description { get; set; }
    }

    public class Provider
    {
        public int Id { get; set; }
        public required string ProviderCode { get; set; }
        public required string ProviderName { get; set; }
        public ProviderType ProviderType { get; set; } // JSON as required string
        public string? IsActive { get; set; }
        public int Product_Id { get; set; }
    

    }

    public class ProviderDto
    {
        public int Id { get; set; }
        public required string ProviderCode { get; set; }
        public required string ProviderName { get; set; }
        public required string ProviderType { get; set; } // JSON as required string
        public required string IsActive { get; set; }
        public required string Product { get; set; }

    }

    public enum ProviderType
    {
        PG = 1,
        BBPS = 2,
        PAYOUT = 3,
        VAM = 4
    }

    public class CommissionScheme
    {
        public int Id { get; set; }
        public required string SchemeCode { get; set; }
        public required string Description { get; set; }
        public required string CommissionType { get; set; }
        public decimal CommissionValue { get; set; }
    }

    public class MdrPricing
    {
        public int Id { get; set; }
        public required int ProductTypeId { get; set; }
        public required int providerId { get; set; }
        public required int paymentMethodId { get; set; }
        public decimal MinAmount { get; set; }
        public decimal MaxAmount { get; set; }
        public decimal MdrPercent { get; set; }
        public decimal MdrFixed { get; set; }
        public string? effective_from { get; set; }
        public string? effective_to { get; set; }
        public string? providerName { get; set; }
        public string? ProductTypeName { get; set; }
        public string? paymentMethod { get; set; }
    }
    public class Biller
    {
        public long Id { get; set; }
        public required string BillerCode { get; set; }
        public required string BillerName { get; set; }
        public required string Category { get; set; }
        public required string IsActive { get; set; }
    }
    public class BusineessRoles
    {
        public int Id { get; set; }
        public required string BusinessName { get; set; }
        
    }

    public class PaymentMode
    {
        public int Id { get; set; }
        public required string Payment_Mode { get; set; }
    }

    public class ProductCategory
    {
        public int Id { get; set; }
        public required string CategoryName { get; set; }
    }

    public class MSGOTPConfig
    {
        public required string MSGUrl { get; set; }
        public required string MSGOtpTemplateId { get; set; }
        public required string MSGOtpAuthKey { get; set; }
    }
}

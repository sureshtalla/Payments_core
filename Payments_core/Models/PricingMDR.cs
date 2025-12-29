namespace Payments_core.Models
{
    public class MdrPricingDto
    {
        public long Id { get; set; }
        public int PaymentMethodId { get; set; }
        public string? PaymentMethod { get; set; } = "";
        public int ProductTypeId { get; set; }
        public string? ProductCategory { get; set; } = "";
        public int ProviderId { get; set; }
        public string? ProviderName { get; set; }
        public decimal SlabMinAmount { get; set; }
        public decimal SlabMaxAmount { get; set; }
        public decimal MdrPercent { get; set; }
        public decimal FixedFee { get; set; }
        public DateTime EffectiveFrom { get; set; }
        public DateTime EffectiveTo { get; set; }
    }

    public class MdrPricingCreateRequest
    {
        public int ProductTypeId { get; set; }
        public int paymentMethodId { get; set; }
        public int ProviderId { get; set; }
        public decimal SlabMinAmount { get; set; }
        public decimal SlabMaxAmount { get; set; }
        public decimal MdrPercent { get; set; }
        public decimal FixedFee { get; set; }
        public DateTime EffectiveFrom { get; set; }
        public DateTime EffectiveTo { get; set; }
    }

    public class MdrPricingUpdateRequest
    {
        public long Id { get; set; }
        public decimal MdrPercent { get; set; }
        public decimal FixedFee { get; set; }
        public DateTime EffectiveFrom { get; set; }
        public DateTime EffectiveTo { get; set; }
    }

    public class CommissionSchemeDto
    {
        public long Id { get; set; }
        public int PaymentMethodId { get; set; }
        public string? PaymentMethod { get; set; } = "";
        public int ProductTypeId { get; set; }
        public string? ProductCategory { get; set; } = "";
        public int ProviderId { get; set; }
        public string? ProviderName { get; set; }
        public decimal Admin_Percent { get; set; }
        public decimal mdr_percent { get; set; }
        public decimal SD_Percent { get; set; }
        public decimal Distributor_Percent { get; set; }
        public decimal Retailer_Percent { get; set; }
        public decimal min_amount { get; set; }
        public decimal max_amount { get; set; }
        public DateTime EffectiveFrom { get; set; }
        public DateTime EffectiveTo { get; set; }
    }

    public class CommissionSchemeRequest
    {
        public int Id { get; set; }
        public int ProductTypeId { get; set; }
        public int paymentMethodId { get; set; }
        public int ProviderId { get; set; }
        public decimal mdr_percent { get; set; }
        public decimal Admin_Percent { get; set; }
        public decimal SD_Percent { get; set; }
        public decimal Distributor_Percent { get; set; }
        public decimal Retailer_Percent { get; set; }
        public decimal min_amount { get; set; }
        public decimal max_amount { get; set; }
        public DateTime EffectiveFrom { get; set; }
        public DateTime EffectiveTo { get; set; }
    }

    public class SpecialPrice
    {
        public long Id { get; set; }

        public long user_id { get; set; }
        public int Product_Category_Id { get; set; }
        public int ProviderId { get; set; }
        public int paymentModeId { get; set; }
        public decimal Price { get; set; }
        public string? Description { get; set; }

        public bool IsActive { get; set; }

        public long CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }

        public long? ModifiedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }

        public string? RoleName { get; set; }
        public string? Merchant { get; set; }
        public string? ProductCategory { get; set; }
        public string? Providers { get; set; }
        public string? PaymentMode { get; set; }

    }

    public class SpecialPriceRequest
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public int PaymentModeId { get; set; }
        public int ProviderId { get; set; }
        public int ProductCategoryId { get; set; }
        public decimal Price { get; set; }
        public string Description { get; set; }

        public long ActionBy { get; set; }
    }

    public class RoutingRule
    {
        public long Id { get; set; }
        public int Priority { get; set; }
        public string RuleName { get; set; }
        public string Criteria { get; set; }
        public int Provider { get; set; }
    }
    public class RoutingRuleRequest
    {
        public int Priority { get; set; }
        public string RuleName { get; set; }
        public string Criteria { get; set; }
        public int Provider { get; set; }
    }

}

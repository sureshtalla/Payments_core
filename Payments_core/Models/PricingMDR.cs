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
        public decimal SD_Percent { get; set; }
        public decimal Distributor_Percent { get; set; }
        public decimal Retailer_Percent { get; set; }
        public DateTime EffectiveFrom { get; set; }
        public DateTime EffectiveTo { get; set; }
    }

    public class CommissionSchemeRequest
    {
        public int Id { get; set; }
        public int ProductTypeId { get; set; }
        public int paymentMethodId { get; set; }
        public int ProviderId { get; set; }
        public decimal Admin_Percent { get; set; }
        public decimal SD_Percent { get; set; }
        public decimal Distributor_Percent { get; set; }
        public decimal Retailer_Percent { get; set; }
        public DateTime EffectiveFrom { get; set; }
        public DateTime EffectiveTo { get; set; }
    }
}

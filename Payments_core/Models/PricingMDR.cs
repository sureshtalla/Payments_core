namespace Payments_core.Models
{
    public class MdrPricingDto
    {
        public long Id { get; set; }
        public string Category { get; set; } = "";
        public string CardType { get; set; } = "";
        public int ProviderId { get; set; }
        public decimal SlabMinAmount { get; set; }
        public decimal SlabMaxAmount { get; set; }
        public decimal MdrPercent { get; set; }
        public decimal FixedFee { get; set; }
        public DateTime EffectiveFrom { get; set; }
        public DateTime EffectiveTo { get; set; }
    }

    public class MdrPricingCreateRequest
    {
        public string Category { get; set; } = "";
        public string CardType { get; set; } = "";
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
}

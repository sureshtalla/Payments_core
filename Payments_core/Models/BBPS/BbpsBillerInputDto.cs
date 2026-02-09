namespace Payments_core.Models.BBPS
{
    public class BbpsBillerInputDto
    {
        public string ParamName { get; set; } = string.Empty;

        public string DataType { get; set; } = string.Empty;

        public int MinLength { get; set; }

        public int MaxLength { get; set; }

        public bool IsOptional { get; set; }

        public string Regex { get; set; } = string.Empty;
    }
}
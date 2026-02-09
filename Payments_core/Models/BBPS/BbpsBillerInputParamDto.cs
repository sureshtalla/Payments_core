namespace Payments_core.Models.BBPS
{
    public class BbpsBillerInputParamDto
    {
        public string ParamName { get; set; }
        public string DataType { get; set; }        // NUMERIC / ALPHANUMERIC
        public bool IsOptional { get; set; }
        public int MinLength { get; set; }
        public int MaxLength { get; set; }
        public bool Visibility { get; set; }
    }
}

namespace Payments_core.Models.BBPS
{
    public class AgentDeviceInfo
    {
        public string Ip { get; set; }
        public string InitChannel { get; set; }
        public string Mac { get; set; }
    }

    public class CustomerInfo
    {
        public string CustomerMobile { get; set; }
        public string CustomerEmail { get; set; }
    }
}

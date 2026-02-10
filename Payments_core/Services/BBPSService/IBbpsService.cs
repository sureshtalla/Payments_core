using Payments_core.Models.BBPS;

namespace Payments_core.Services.BBPSService
{
    public interface IBbpsService
    {
        Task<BbpsFetchResponseDto> FetchBill(
             long userId,
             string billerId,
             Dictionary<string, string> inputParams,
             AgentDeviceInfo agentDeviceInfo,
             CustomerInfo customerInfo
         );

        Task<BbpsPayResponseDto> PayBill(
            long userId,
            string billerId,
            string billRequestId,
            decimal amount,
            string tpin,
            string customerMobile
        );

        Task<BbpsBillValidationResponseDto> ValidateBill(string billerId,Dictionary<string, string> inputParams);

        Task<BbpsStatusResponseDto> CheckStatus(
            string txnRefId,
            string billRequestId
        );

        Task SyncBillers();

        // ---------- ✅ REQUIRED FOR /billers API ----------
        Task<IEnumerable<BbpsBillerListDto>> GetBillersByCategory(string category);

        Task<List<BbpsBillerInputParamDto>> GetBillerParams(string billerId);

    }
}
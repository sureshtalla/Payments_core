using Payments_core.Models.BBPS;
using System.Text.Json;
using System.Collections.Generic;

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
         string? billRequestId,
         Dictionary<string, string>? inputParams,
         JsonElement? billerResponse,
         JsonElement? additionalInfo,
         decimal amount,
         string amountTag,
         string tpin,
         string customerMobile,
         string requestId
     );

        Task<BillerDto?> GetBillerById(string billerId);

        Task<BbpsBillValidationResponseDto> ValidateBill(string billerId,Dictionary<string, string> inputParams);

        Task<BbpsStatusResponseDto> CheckStatus(
        string requestId,
        string txnRefId,
        string billRequestId);

        Task SyncBillers();

        // ---------- ✅ REQUIRED FOR /billers API ----------
        Task<IEnumerable<BbpsBillerListDto>> GetBillersByCategory(string category);

        Task<List<BbpsBillerInputParamDto>> GetBillerParams(string billerId);

        Task<object> SearchTransactions(
          string txnRefId,
          string mobile,
          DateTime? fromDate,
          DateTime? toDate
      );

    }
}
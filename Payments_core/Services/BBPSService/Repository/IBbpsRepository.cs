using Payments_core.Models.BBPS;

namespace Payments_core.Services.BBPSService.Repository
{
    public interface IBbpsRepository
    {

        // ---------- ✅ REQUIRED FOR /billers API ----------
        Task<IEnumerable<BbpsBillerListDto>> GetBillersByCategory(string category);

        Task SaveFetchBill(
            string billRequestId,
            long userId,
            string billerId,
            string customerName,
            decimal amount,
            DateTime dueDate,
            string responseCode,
            string responseMessage,
            string rawXml
        );

        Task SavePayment(
            string billRequestId,
            string txnRefId,
            long userId,
            decimal amount,
            string status,
            string responseCode,
            string responseMessage,
            string rawXml
        );

        Task UpdateStatus(
            string txnRefId,
            string billRequestId,
            string status,
            string rawXml
        );


        Task<IEnumerable<(string TxnRefId, string BillRequestId)>> GetPendingTxns();

        Task UpsertBiller(BbpsBillerMaster biller);
  
        Task UpsertBillerInputs(BbpsBillerDto biller);

        Task<IEnumerable<string>> GetActiveBillerIds(string environment);


    }
}
using Payments_core.Models.BBPS;

namespace Payments_core.Services.BBPSService.Repository
{
    public interface IBbpsRepository
    {

        // ---------- ✅ REQUIRED FOR /billers API ----------
        Task<IEnumerable<BbpsBillerListDto>> GetBillersByCategory(string category);

        Task SaveFetchBill(
        string requestId,
        string billRequestId,
        long userId,
        string agentId,
        string billerId,
        string billerCategory,
        string customerName,
        string consumerRef,
        string vehicleRegNo,
        decimal amount,
        DateTime? dueDate,
        string responseCode,
        string responseMessage,
        string rawXml
    );

        Task<string?> GetBillerCategory(string billerId);

            Task SavePayment(
            string requestId,
            string billRequestId,
            string txnRefId,
            long userId,
            decimal amount,
            string status,
            string responseCode,
            string responseMessage,
            string billerId,
            string billerName,
            string paymentMode,
            string rawXml
        );

        Task UpdateStatus(
            string txnRefId,
            string billRequestId,
            string status,
            string rawXml
        );

        Task UpdatePaymentStatus(
        string txnRefId,
        string status
        );
        Task MarkSmsSent(string txnRefId);

        Task<BbpsPendingTxnDto?> GetPaymentByTxnRef(string txnRefId);
        Task<string?> GetRequestIdByTxnRef(string txnRefId);

        Task<string?> GetBillRequestIdByTxnRef(string txnRefId);

        Task<IEnumerable<(string TxnRefId, string BillRequestId)>> GetPendingTxns();

        Task UpsertBiller(BbpsBillerMaster biller);
  
        Task UpsertBillerInputs(BbpsBillerDto biller);

        Task<BillerDto?> GetBillerById(string billerId);

        Task<IEnumerable<string>> GetActiveBillerIds(string environment);

        //Task<IEnumerable<BbpsPendingTxnDto>> GetPendingTransactions();

        //Task UpdateReconAttempt(
        //    string txnRefId,
        //    string status,
        //    string rawStatusXml
        //);


        Task<IEnumerable<BbpsPendingTxnDto>> GetPendingTransactions();

        Task UpdateReconAttempt(
            string txnRefId,
            string status,
            string rawXml
        );

        Task UpdateComplaintSmsStatus(string complaintId, string status);

        Task<List<BbpsTxnReportDto>> SearchTransactions(
            string txnRefId,
            string mobile,
            DateTime? fromDate,
            DateTime? toDate
        );

        Task<dynamic?> GetReceiptRaw(string txnRefId);
    }
}
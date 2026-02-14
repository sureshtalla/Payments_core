using Dapper;
using Payments_core.Models.BBPS;
using Payments_core.Services.DataLayer;
using System.Data;


namespace Payments_core.Services.BBPSService.Repository
{
    public class BbpsRepository : IBbpsRepository
    {
        private readonly IDapperContext _db;

        public BbpsRepository(IDapperContext db)
        {
            _db = db;
        }

        public async Task SaveFetchBill(
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
         string rawXml)
        {
            await _db.ExecuteStoredAsync(
                "sp_bbps_fetch_insert",
                new
                {
                    p_request_id = requestId,
                    p_bill_request_id = billRequestId,
                    p_user_id = userId,
                    p_agent_id = agentId,
                    p_biller_id = billerId,
                    p_biller_category = billerCategory,
                    p_customer_name = customerName,
                    p_consumer_ref = consumerRef,
                    p_vehicle_reg_no = vehicleRegNo,
                    p_bill_amount = amount,
                    p_due_date = dueDate,
                    p_response_code = responseCode,
                    p_response_message = responseMessage,
                    p_raw_xml = rawXml
                });
        }

        public async Task SavePayment(
          string requestId,
          string billRequestId,
          string txnRefId,
          long userId,
          decimal amount,
          string status,
          string responseCode,
          string responseMessage,
          string rawXml)
        {
            await _db.ExecuteStoredAsync(
                "sp_bbps_payment_insert",
                new
                {
                    p_request_id = requestId,
                    p_bill_request_id = billRequestId,
                    p_txn_ref_id = txnRefId,
                    p_user_id = userId,
                    p_amount = amount,
                    p_status = status,
                    p_response_code = responseCode,
                    p_response_message = responseMessage,
                    p_raw_xml = rawXml
                });
        }

        public async Task UpdatePaymentStatus(
        string txnRefId,
        string status)
        {
            await _db.ExecuteStoredAsync(
                "sp_bbps_payment_status_update",
                new
                {
                    p_txn_ref_id = txnRefId,
                    p_status = status
                });
        }

        public async Task UpdateStatus(
            string txnRefId,
            string billRequestId,
            string status,
            string rawXml)
        {
            await _db.ExecuteStoredAsync(
                "sp_bbps_status_update",
                new
                {
                    p_txn_ref_id = txnRefId,
                    p_bill_request_id = billRequestId,
                    p_status = status,
                    p_raw_xml = rawXml
                });
        }

        public async Task<IEnumerable<(string TxnRefId, string BillRequestId)>> GetPendingTxns()
        {
            var rows = await _db.GetData<dynamic>("sp_bbps_get_pending_txns");

            return rows.Select(r => (
                (string)r.txn_ref_id,
                (string)r.bill_request_id
            ));
        }

        public async Task<string?> GetRequestIdByTxnRef(string txnRefId)
        {
            var rows = await _db.GetData<string>(
                "sp_bbps_get_request_id_by_txn_ref",
                new { p_txn_ref_id = txnRefId }
            );

            return rows.FirstOrDefault();
        }

        //public Task UpsertBiller(BbpsBillerDto biller)
        //       => _db.ExecuteStoredAsync("sp_bbps_biller_upsert", new
        //       {
        //           p_biller_id = biller.BillerId,
        //           p_biller_name = biller.BillerName,
        //           p_category = biller.Category,
        //           p_status = biller.Status
        //       });

        public async Task UpsertBiller(BbpsBillerMaster biller)
        {
            await _db.ExecuteStoredAsync(
                "sp_bbps_biller_upsert",
                new
                {
                    p_biller_id = biller.BillerId,
                    p_biller_name = biller.BillerName,
                    p_category = biller.Category,
                    p_fetch_requirement = biller.FetchRequirement,
                    p_payment_amount_exactness = biller.PaymentAmountExactness,
                    p_supports_adhoc = biller.SupportsAdhoc ? 1 : 0
                }
            );
        }

        // File: Payments_core/Services/BBPSService/Repository/BbpsRepository.cs
        public async Task<IEnumerable<string>> GetActiveBillerIds(string environment)
        {
            var parameters = new DynamicParameters();
            parameters.Add("p_environment", environment);

            return await _db.GetData<string>(
                "sp_bbps_get_active_biller_ids",
                parameters
            );
        }

        public Task UpsertBillerInputs(BbpsBillerDto biller)
        {
            // Will be implemented when parsing input params
            return Task.CompletedTask;
        }

        public async Task<IEnumerable<BbpsBillerListDto>> GetBillersByCategory(string category)
        {
            var param = new DynamicParameters();
            param.Add("p_category", category);

            return await _db.GetData<BbpsBillerListDto>(
                "sp_bbps_get_billers_by_category",
                param
            );
        }

        public async Task<string?> GetBillerCategory(string billerId)
        {
            var param = new DynamicParameters();
            param.Add("p_biller_id", billerId);

            var rows = await _db.GetData<string>(
                "sp_bbps_get_biller_category",
                param
            );

            return rows.FirstOrDefault();
        }

        public async Task<IEnumerable<BbpsPendingTxnDto>> GetPendingTransactions()
        {
            return await _db.GetData<BbpsPendingTxnDto>(
                "sp_bbps_get_pending_txns"
            );
        }

        public async Task UpdateReconAttempt(
            string txnRefId,
            string status,
            string rawXml)
        {
            await _db.ExecuteStoredAsync(
                "sp_bbps_recon_update",
                new
                {
                    p_txn_ref_id = txnRefId,
                    p_status = status,
                    p_raw_xml = rawXml
                }
            );
        }

        public async Task<BillerDto?> GetBillerById(string billerId)
        {
            var result = await _db.GetData<BillerDto>(
                "sp_bbps_get_biller_by_id",
                new { p_biller_id = billerId }
            );

            return result.FirstOrDefault();
        }

        public async Task<string?> GetBillRequestIdByTxnRef(string txnRefId)
        {
            var rows = await _db.GetData<string>(
                "sp_bbps_get_bill_request_id_by_txn_ref",
                new { p_txn_ref_id = txnRefId }
            );

            return rows.FirstOrDefault();
        }

    }
}
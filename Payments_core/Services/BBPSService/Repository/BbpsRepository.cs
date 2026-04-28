using System;
using System.Collections.Generic;
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

        //public async Task SavePayment(
        //  string requestId,
        //  string billRequestId,
        //  string txnRefId,
        //  long userId,
        //  decimal amount,
        //  string status,
        //  string responseCode,
        //  string responseMessage,
        //  string rawXml)
        //{
        //    await _db.ExecuteStoredAsync(
        //        "sp_bbps_payment_insert",
        //        new
        //        {
        //            p_request_id = requestId,
        //            p_bill_request_id = billRequestId,
        //            p_txn_ref_id = txnRefId,
        //            p_user_id = userId,
        //            p_amount = amount,
        //            p_status = status,
        //            p_response_code = responseCode,
        //            p_response_message = responseMessage,
        //            p_raw_xml = rawXml
        //        });
        //}

        public async Task SavePayment(
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
                    p_biller_id = billerId,
                    p_biller_name = billerName,
                    p_payment_mode = paymentMode,
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

        public async Task<BbpsPendingTxnDto?> GetPaymentByTxnRef(string txnRefId)
        {
            var result = await _db.GetData<BbpsPendingTxnDto>(
                "sp_bbps_get_payment_by_txn_ref",
                new { p_txn_ref_id = txnRefId }
            );

            return result.FirstOrDefault();
        }
        public async Task MarkSmsSent(string txnRefId)
        {
            await _db.ExecuteStoredAsync(
                "sp_bbps_payment_sms_update",
                new { p_txn_ref_id = txnRefId }
            );
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
                    p_supports_adhoc = biller.SupportsAdhoc ? 1 : 0,
                    p_biller_status = biller.BillerStatus ?? "ACTIVE"
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
        public async Task<List<BbpsBillerInputParamDto>> GetBillerParamsFromDb(string billerId)
        {
            var param = new DynamicParameters();
            param.Add("p_biller_id", billerId);

            var result = await _db.GetData<BbpsBillerInputParamDto>(
                "sp_bbps_get_biller_params",
                param
            );

            return result.ToList();
        }

        // ✅ NEW: Save biller input params to DB so we never call live MDM again for this biller
        public async Task SaveBillerParams(string billerId, List<BbpsBillerInputParamDto> parameters)
        {
            foreach (var p in parameters)
            {
                await _db.ExecuteStoredAsync(
                    "sp_bbps_save_biller_param",
                    new
                    {
                        p_biller_id = billerId,
                        p_param_name = p.ParamName,
                        p_data_type = p.DataType,
                        p_is_optional = p.IsOptional ? 1 : 0,
                        p_min_length = p.MinLength,
                        p_max_length = p.MaxLength,
                        p_visibility = p.Visibility ? 1 : 0
                    });
            }
        }
        public async Task<string?> GetBillRequestIdByTxnRef(string txnRefId)
        {
            var rows = await _db.GetData<string>(
                "sp_bbps_get_bill_request_id_by_txn_ref",
                new { p_txn_ref_id = txnRefId }
            );

            return rows.FirstOrDefault();
        }

        public async Task UpdateComplaintSmsStatus(
        string complaintId,
        string status)
        {
            await _db.ExecuteStoredAsync(
                "sp_bbps_complaint_sms_update",
                new
                {
                    p_complaint_id = complaintId,
                    p_sms_status = status
                });
        }

        public async Task<List<BbpsTxnReportDto>> SearchTransactions(
        string txnRefId,
        string mobile,
        DateTime? from,
        DateTime? to)
        {
            // Use dynamic to avoid Dapper column-name mismatch when SP version differs from DTO.
            var raw = await _db.GetData<dynamic>(
                "sp_bbps_transaction_search",
                new
                {
                    p_txn_ref_id = txnRefId,
                    p_mobile = mobile,
                    p_from_date = from,
                    p_to_date = to
                });

            return raw.Select(r =>
            {
                var d = (IDictionary<string, object>)r;
                // REPLACE WITH:
                T Get<T>(string key)
                {
                    foreach (var k in d.Keys)
                        if (string.Equals(k, key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (d[k] == null || d[k] is DBNull) return default!;
                            try { return (T)Convert.ChangeType(d[k], typeof(T)); }
                            catch { return default!; }
                        }
                    return default!;
                }
                return new BbpsTxnReportDto
                {
                    BillerId = Get<string>("billerId"),
                    BillerName = Get<string>("billerName") ?? Get<string>("biller_name"),
                    BillerCategory = Get<string>("billerCategory"),
                    Amount = Get<decimal>("amount"),
                    TxnDate = Get<DateTime>("txnDate"),
                    TxnReferenceId = Get<string>("txnReferenceId"),
                    TxnStatus = Get<string>("txnStatus") ?? Get<string>("status"),
                    ResponseCode = Get<string>("responseCode"),
                    PaymentMode = Get<string>("paymentMode"),
                    RequestId = Get<string>("requestId"),
                    AgentName = Get<string>("agentName"),
                    AgentMobile = Get<string>("agentMobile"),
                };
            }).ToList();
        }

        public async Task<dynamic?> GetReceiptRaw(string txnRefId)
        {
            var result = await _db.GetData<dynamic>(
                "sp_bbps_get_receipt",
                new { p_txn_ref_id = txnRefId }
            );

            return result.FirstOrDefault();
        }
        public async Task<string?> GetAgentIdByRequestId(string requestId)
        {
            var rows = await _db.GetData<string>(
                "sp_bbps_get_agent_id_by_request_id",
                new DynamicParameters(new { p_request_id = requestId })
            );
            return rows.FirstOrDefault();
        }
    }
}
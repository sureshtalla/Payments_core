using Payments_core.Helpers;
using Payments_core.Services.DataLayer;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Payments_core.Services.BBPSService
{
    public class BbpsComplaintService : IBbpsComplaintService
    {
        private readonly IConfiguration _config;
        private readonly IBillAvenueClient _client;
        private readonly IDapperContext _db;

        public BbpsComplaintService(
            IConfiguration config,
            IBillAvenueClient client,
            IDapperContext db)
        {
            _config = config;
            _client = client;
            _db = db;
        }

        // ==========================================================
        // REGISTER COMPLAINT
        // ==========================================================
        public async Task RegisterComplaint(
       string txnRefId,
       string complaintType,
       string description)
        {
            var cfg = _config.GetSection("BillAvenue");
            var requestId = BillAvenueRequestId.Generate();

            string xml =
            $@"<complaintRegistrationRequest>
            <instituteId>{cfg["InstituteId"]}</instituteId>
            <requestId>{requestId}</requestId>
            <txnRefId>{txnRefId}</txnRefId>
            <disposition>{complaintType}</disposition>
            <description>{description}</description>
            </complaintRegistrationRequest>";

            string encRequest =
                BillAvenueCrypto.Encrypt(xml, cfg["WorkingKey"]);

            string response = await _client.PostFormAsync(
                $"{cfg["BaseUrl"]}/billpay/extComplaints/register/xml",
                new Dictionary<string, string>
                {
            { "accessCode", cfg["AccessCode"] },
            { "requestId", requestId },
            { "ver", "2.0" },
            { "instituteId", cfg["InstituteId"] },
            { "encRequest", encRequest }
                }
            );

            // 🔥 Insert into your existing table
            await _db.ExecuteStoredAsync(
                "sp_bbps_complaint_insert",
                new
                {
                    p_txn_ref_id = txnRefId,
                    p_complaint_id = requestId,
                    p_description = description,
                    p_status = "REGISTERED",
                    p_request_xml = xml,
                    p_response_xml = response
                });
        }

        // ==========================================================
        // TRACK COMPLAINT
        // ==========================================================
        public async Task<object> TrackComplaint(string complaintId)
        {
            var cfg = _config.GetSection("BillAvenue");
            var requestId = BillAvenueRequestId.Generate();

            string xml =
                $@"<complaintTrackingRequest>
                <instituteId>{cfg["InstituteId"]}</instituteId>
                <requestId>{requestId}</requestId>
                <complaintId>{complaintId}</complaintId>
                </complaintTrackingRequest>";

            string encRequest =
                BillAvenueCrypto.Encrypt(xml, cfg["WorkingKey"]);

            string response = await _client.PostFormAsync(
                $"{cfg["BaseUrl"]}/billpay/extComplaints/track/xml",
                new Dictionary<string, string>
                {
                    { "accessCode", cfg["AccessCode"] },
                    { "requestId", requestId },
                    { "ver", "2.0" },
                    { "instituteId", cfg["InstituteId"] },
                    { "encRequest", encRequest }
                }
            );

            return new
            {
                success = true,
                status = "IN_PROGRESS",
                message = "Complaint tracking request submitted."
            };
        }
    }
}
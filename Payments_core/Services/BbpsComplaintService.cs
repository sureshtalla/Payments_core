using Payments_core.Helpers;
using Payments_core.Services.DataLayer;

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

        public async Task RegisterComplaint(
            string txnRefId,
            string billerId,
            string complaintType,
            string description)
        {
            var cfg = _config.GetSection("BillAvenue");
            var requestId = BillAvenueRequestId.Generate();

            var xml = $@"
<complaintRegistrationRequest>
    <instituteId>{cfg["InstituteId"]}</instituteId>
    <requestId>{requestId}</requestId>
    <txnRefId>{txnRefId}</txnRefId>
    <billerId>{billerId}</billerId>
    <complaintType>{complaintType}</complaintType>
    <description>{description}</description>
</complaintRegistrationRequest>";

            var encRequest = BillAvenueCrypto.Encrypt(xml, cfg["WorkingKey"]);

            var response = await _client.PostFormAsync(
                $"{cfg["BaseUrl"]}/complaintRegistration",
                new Dictionary<string, string>
                {
                    { "accessCode", cfg["AccessCode"] },
                    { "requestId", requestId },
                    { "ver", "2.0" },
                    { "instituteId", cfg["InstituteId"] },
                    { "encRequest", encRequest }
                }
            );

            await _db.ExecuteStoredAsync("sp_bbps_complaint_insert", new
            {
                p_txn_ref_id = txnRefId,
                p_complaint_id = requestId,
                p_complaint_type = complaintType,
                p_description = description,
                p_status = "REGISTERED",
                p_biller_id = billerId,
                p_request_xml = xml,
                p_response_xml = response
            });
        }

        public async Task TrackComplaint(string complaintId)
        {
            var cfg = _config.GetSection("BillAvenue");
            var requestId = BillAvenueRequestId.Generate();

            var xml = $@"
            <complaintTrackingRequest>
                <instituteId>{cfg["InstituteId"]}</instituteId>
                <requestId>{requestId}</requestId>
                <complaintId>{complaintId}</complaintId>
            </complaintTrackingRequest>";

            var encRequest = BillAvenueCrypto.Encrypt(xml, cfg["WorkingKey"]);

            var response = await _client.PostFormAsync(
                $"{cfg["BaseUrl"]}/complaintTracking",
                new Dictionary<string, string>
                {
                    { "accessCode", cfg["AccessCode"] },
                    { "requestId", requestId },
                    { "ver", "2.0" },
                    { "instituteId", cfg["InstituteId"] },
                    { "encRequest", encRequest }
                }
            );

            // Optional: update status in DB
        }
    }
}
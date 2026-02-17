using Payments_core.Helpers;
using Payments_core.Services.DataLayer;
using Microsoft.Extensions.Configuration;
using System.Xml.Linq;
using Payments_core.Services.BBPSService.Repository;
using Payments_core.Services.OTPService;
using Payments_core.Services.UserDataService;

namespace Payments_core.Services.BBPSService
{
    public class BbpsComplaintService : IBbpsComplaintService
    {
        private readonly IConfiguration _config;
        private readonly IBillAvenueClient _client;
        private readonly IDapperContext _db;
        private readonly IMSG91OTPService _msgService;
        private readonly IBbpsRepository _repo;
        private readonly IUserDataService _userDataService;

        public BbpsComplaintService(
            IConfiguration config,
            IBillAvenueClient client,
            IDapperContext db,
            IMSG91OTPService msgService,
            IBbpsRepository repo,
            IUserDataService userDataService)
        {
            _config = config;
            _client = client;
            _db = db;
            _msgService = msgService;
            _repo = repo;
            _userDataService = userDataService;
        }

        // =====================================================
        // REGISTER COMPLAINT
        // =====================================================
        public async Task<object> RegisterComplaint(
     string txnRefId,
     string complaintDisposition,
     string description)
        {
            var cfg = _config.GetSection("BillAvenue");
            var requestId = BillAvenueRequestId.Generate();

            // ===============================
            // 1️⃣ CORRECT XML AS PER DOC
            // ===============================
            string xml =
            $@"<complaintRegistrationReq>
        <txnRefId>{txnRefId}</txnRefId>
        <complaintDesc>{description}</complaintDesc>
        <complaintDisposition>{complaintDisposition}</complaintDisposition>
      </complaintRegistrationReq>";

            string encRequest =
                BillAvenueCrypto.Encrypt(xml, cfg["WorkingKey"]);

            // ===============================
            // 2️⃣ IMPORTANT: ver = 2.0
            // ===============================
            string rawResponse = await _client.PostFormAsync(
                $"{cfg["BaseUrl"]}/billpay/extComplaints/register/xml",
                new Dictionary<string, string>
                {
            { "accessCode", cfg["AccessCode"] },
            { "requestId", requestId },
            { "ver", "2.0" }, // 🔥 MUST BE 2.0
            { "instituteId", cfg["InstituteId"] },
            { "encRequest", encRequest }
                }
            );

            string decryptedXml =
                BillAvenueCrypto.Decrypt(rawResponse, cfg["WorkingKey"]);

            Console.WriteLine("===== COMPLAINT REGISTER RESPONSE XML =====");
            Console.WriteLine(decryptedXml);

            // ===============================
            // 3️⃣ PARSE RESPONSE PROPERLY
            // ===============================
            var doc = XDocument.Parse(decryptedXml);

            var root = doc.Element("complaintRegistrationResp");

            var complaintId = root?.Element("complaintId")?.Value ?? "";
            var responseCode = root?.Element("responseCode")?.Value ?? "";
            var responseReason = root?.Element("responseReason")?.Value ?? "";
            var complaintAssigned = root?.Element("complaintAssigned")?.Value ?? "";

            // ===============================
            // 4️⃣ SAVE FULL XML
            // ===============================
            await _db.ExecuteStoredAsync(
                "sp_bbps_complaint_insert",
                new
                {
                    p_txn_ref_id = txnRefId,
                    p_complaint_id = complaintId,
                    p_description = description,
                    p_status = complaintAssigned,
                    p_request_xml = xml,
                    p_response_xml = decryptedXml
                });

            // ===============================
            // 5️⃣ RETURN FULL RESPONSE
            // ===============================
            return new
            {
                success = responseCode == "000",
                complaintId,
                complaintAssigned,
                responseCode,
                responseReason,
                rawXml = decryptedXml
            };
        }

        // =====================================================
        // TRACK COMPLAINT (UPDATED - FULL RESPONSE + REQUESTID)
        // =====================================================
        public async Task<object> TrackComplaint(string complaintId)
        {
            var cfg = _config.GetSection("BillAvenue");

            // 🔥 Generate 35-character requestId (MANDATORY for BBPS)
            var requestId = BillAvenueRequestId.Generate();

            string xml =
            $@"<complaintTrackingRequest>
            <instituteId>{cfg["InstituteId"]}</instituteId>
            <requestId>{requestId}</requestId>
            <complaintId>{complaintId}</complaintId>
            </complaintTrackingRequest>";

            Console.WriteLine("===== COMPLAINT TRACK REQUEST =====");
            Console.WriteLine($"TrackingRequestId: {requestId}");
            Console.WriteLine($"ComplaintId: {complaintId}");
            Console.WriteLine(xml);

            string encRequest =
                BillAvenueCrypto.Encrypt(xml, cfg["WorkingKey"]);

            string rawResponse = await _client.PostFormAsync(
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

            string decryptedXml =
                BillAvenueCrypto.Decrypt(rawResponse, cfg["WorkingKey"]);

            Console.WriteLine("===== COMPLAINT TRACK RESPONSE =====");
            Console.WriteLine(decryptedXml);

            var doc = XDocument.Parse(decryptedXml);

            var responseCode = doc.Root?
                .Element("responseCode")?.Value;

            var responseReason = doc.Root?
                .Element("responseReason")?.Value;

            var status = doc.Root?
                .Element("complaintStatus")?.Value;

            var remarks = doc.Root?
                .Element("complaintRemarks")?.Value;

            var complaintAssigned = doc.Root?
                .Element("complaintAssigned")?.Value;

            return new
            {
                success = responseCode == "000",
                complaintId = complaintId,
                complaintAssigned,
                status,
                remarks,
                responseCode,
                responseReason,
                trackingRequestId = requestId,   
                rawXml = decryptedXml
            };
        }
    }
}
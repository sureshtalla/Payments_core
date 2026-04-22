using Newtonsoft.Json;
using Payments_core.Services.KycVerificationService;
using System.Text;

namespace Payments_core.Integrations.Cashfree
{
    public class CashfreeVerificationClient
    {
        private readonly HttpClient _http;
        private readonly IConfiguration _config;
        private readonly KycApiCredentialService _credentialService;

        public CashfreeVerificationClient(
            HttpClient http,
            IConfiguration config,
            KycApiCredentialService credentialService)
        {
            _http = http;
            _config = config;
            _credentialService = credentialService;
        }

        private async Task AddHeaders()
        {
            Console.WriteLine("STEP 1: Loading Cashfree credentials from DB");
            var config = await _credentialService.GetCashfreeCredentials();
            Console.WriteLine("STEP 2: DB response received");

            if (config == null)
            {
                Console.WriteLine("ERROR: Cashfree credentials not found in DB");
                throw new Exception("Cashfree credentials not found in DB");
            }

            Console.WriteLine("STEP 3: Setting headers");
            _http.DefaultRequestHeaders.Clear();
            _http.DefaultRequestHeaders.Add("x-client-id", config.client_id.ToString());
            _http.DefaultRequestHeaders.Add("x-client-secret", config.client_secret.ToString());
            _http.DefaultRequestHeaders.Add("Accept", "application/json");
            Console.WriteLine("STEP 4: Headers added successfully");
        }

        private async Task<dynamic> HandleResponse(HttpResponseMessage response)
        {
            var json = await response.Content.ReadAsStringAsync();
            Console.WriteLine("Cashfree Response: " + json);

            if (!response.IsSuccessStatusCode)
                throw new Exception("Cashfree API Error: " + json);

            return JsonConvert.DeserializeObject<dynamic>(json);
        }

        // ── PAN ──────────────────────────────────────────────────────────
        // POST /pan
        public async Task<dynamic> VerifyPan(string pan)
        {
            Console.WriteLine("STEP A: Starting PAN verification");
            await AddHeaders();
            Console.WriteLine("STEP B: Headers ready, calling Cashfree API");

            var response = await _http.PostAsync(
                $"{_config["Cashfree:BaseUrl"]}/pan",
                new StringContent(
                    JsonConvert.SerializeObject(new { pan }),
                    Encoding.UTF8, "application/json"));

            Console.WriteLine("STEP C: Cashfree API called");
            return await HandleResponse(response);
        }

        // ── BANK ────────────────────────────────────────────
        // GET /bank-account?bank_account=&ifsc=&name=&reference_id=
        public async Task<dynamic> VerifyBank(string account, string ifsc, string name)
        {
            await AddHeaders();

            var body = new
            {
                bank_account = account,
                ifsc = ifsc,
                name = name,
                user_id = $"ben_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}"
            };

            Console.WriteLine("[VerifyBank] POST " + $"{_config["Cashfree:BaseUrl"]}/bank-account/async");
            Console.WriteLine("[VerifyBank] Body: " + JsonConvert.SerializeObject(body));

            var response = await _http.PostAsync(
                $"{_config["Cashfree:BaseUrl"]}/bank-account/async",
                new StringContent(
                    JsonConvert.SerializeObject(body),
                    Encoding.UTF8,
                    "application/json"));

            return await HandleResponse(response);
        }




        public async Task<dynamic> GetBankVerificationStatus(string referenceId)
        {
            await AddHeaders();

            var encodedReferenceId = Uri.EscapeDataString(referenceId);

            var url = $"{_config["Cashfree:BaseUrl"]}/bank-account"
                    + $"?reference_id={encodedReferenceId}";

            Console.WriteLine($"[GetBankVerificationStatus] GET {url}");

            var response = await _http.GetAsync(url);

            return await HandleResponse(response);
        }


        // ── DIGILOCKER STEP 1 ─────────────────────────────────────────────
        // POST /verification/digilocker
        //
        // FIX: Your original endpoint was /digilocker/verify-account
        // Correct Cashfree endpoint is /verification/digilocker
        // Also: aadhaar_number IS accepted here — kept as-is
        public async Task<dynamic> VerifyAccount(string verificationId, string aadhaar)
        {
            await AddHeaders();

            var body = new
            {
                verification_id = verificationId,
                document_requested = new[] { "AADHAAR" },
                redirect_url = _config["Cashfree:DigiLockerRedirectUrl"]
                                     ?? "https://merchant.fastcashfnx.in",
                user_flow = "signup"
            };

            Console.WriteLine("[VerifyAccount] Sending to Cashfree: " + JsonConvert.SerializeObject(body));

            var response = await _http.PostAsync(
                $"{_config["Cashfree:BaseUrl"]}/digilocker",
                new StringContent(
                    JsonConvert.SerializeObject(body),
                    Encoding.UTF8, "application/json"));

            return await HandleResponse(response);
        }

        // ── DIGILOCKER STEP 2 ─────────────────────────────────────────────
        // POST /verification/digilocker/link
        public async Task<dynamic> CreateLink(string verificationId)
        {
            await AddHeaders();

            var body = new { verification_id = verificationId };

            var response = await _http.PostAsync(
                $"{_config["Cashfree:BaseUrl"]}/digilocker/link",
                new StringContent(
                    JsonConvert.SerializeObject(body),
                    Encoding.UTF8, "application/json"));

            return await HandleResponse(response);
        }

        // ── DIGILOCKER STATUS ─────────────────────────────────────────────
        // GET /verification/digilocker/{verificationId}
        public async Task<dynamic> GetStatus(string verificationId)
        {
            await AddHeaders();

            var response = await _http.GetAsync(
                $"{_config["Cashfree:BaseUrl"]}/digilocker?verification_id={verificationId}");

            return await HandleResponse(response);
        }

        // ── DIGILOCKER DOCUMENT ───────────────────────────────────────────
        // GET /verification/digilocker/{verificationId}/aadhaar
        public async Task<dynamic> GetDocument(string verificationId)
        {
            await AddHeaders();

            var response = await _http.GetAsync(
                $"{_config["Cashfree:BaseUrl"]}/digilocker/fetch-data?verification_id={verificationId}&document_type=AADHAAR");

            return await HandleResponse(response);
        }
    }
}
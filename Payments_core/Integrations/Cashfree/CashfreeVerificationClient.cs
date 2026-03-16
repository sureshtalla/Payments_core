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

        // PAN VERIFICATION
        public async Task<dynamic> VerifyPan(string pan)
        {
            Console.WriteLine("STEP A: Starting PAN verification");

            await AddHeaders();

            Console.WriteLine("STEP B: Headers ready, calling Cashfree API");

            var body = new { pan };

            var response = await _http.PostAsync(
                $"{_config["Cashfree:BaseUrl"]}/pan",
                new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json"));

            Console.WriteLine("STEP C: Cashfree API called");

            return await HandleResponse(response);
        }

        // BANK VERIFICATION
        public async Task<dynamic> VerifyBank(string account, string ifsc, string name)
        {
            await AddHeaders();   // ✔ fixed

            var body = new
            {
                bank_account = account,
                ifsc = ifsc,
                name = name
            };

            var response = await _http.PostAsync(
                $"{_config["Cashfree:BaseUrl"]}/bank-account",
                new StringContent(
                    JsonConvert.SerializeObject(body),
                    Encoding.UTF8,
                    "application/json"));

            return await HandleResponse(response);
        }

        // DIGILOCKER STEP 1
        public async Task<dynamic> VerifyAccount(string verificationId, string aadhaar)
        {
            await AddHeaders();   // ✔ fixed

            var body = new
            {
                verification_id = verificationId,
                aadhaar_number = aadhaar
            };

            var response = await _http.PostAsync(
                $"{_config["Cashfree:BaseUrl"]}/digilocker/verify-account",
                new StringContent(
                    JsonConvert.SerializeObject(body),
                    Encoding.UTF8,
                    "application/json"));

            return await HandleResponse(response);
        }

        // DIGILOCKER STEP 2
        public async Task<dynamic> CreateLink(string verificationId)
        {
            await AddHeaders();   // ✔ fixed

            var body = new { verification_id = verificationId };

            var response = await _http.PostAsync(
                $"{_config["Cashfree:BaseUrl"]}/digilocker/create-link",
                new StringContent(
                    JsonConvert.SerializeObject(body),
                    Encoding.UTF8,
                    "application/json"));

            return await HandleResponse(response);
        }

        // DIGILOCKER STATUS
        public async Task<dynamic> GetStatus(string verificationId)
        {
            await AddHeaders();   // ✔ fixed

            var response = await _http.GetAsync(
                $"{_config["Cashfree:BaseUrl"]}/digilocker/status/{verificationId}");

            return await HandleResponse(response);
        }

        // DIGILOCKER DOCUMENT
        public async Task<dynamic> GetDocument(string verificationId)
        {
            await AddHeaders();   // ✔ fixed

            var response = await _http.GetAsync(
                $"{_config["Cashfree:BaseUrl"]}/digilocker/document/{verificationId}");

            return await HandleResponse(response);
        }
    }
}
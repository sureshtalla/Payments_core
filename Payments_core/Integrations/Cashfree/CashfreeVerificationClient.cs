using Newtonsoft.Json;
using System.Text;

public class CashfreeVerificationClient
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;

    public CashfreeVerificationClient(HttpClient http, IConfiguration config)
    {
        _http = http;
        _config = config;
    }

    //private void AddHeaders()
    //{
    //    _http.DefaultRequestHeaders.Clear();

    //    _http.DefaultRequestHeaders.Add(
    //        "x-client-id",
    //        _config["Cashfree:ClientId"]);

    //    _http.DefaultRequestHeaders.Add(
    //        "x-client-secret",
    //        _config["Cashfree:ClientSecret"]);
    //}

    private void AddHeaders()
    {
        _http.DefaultRequestHeaders.Clear();

        _http.DefaultRequestHeaders.Add("x-client-id", _config["Cashfree:ClientId"]);
        _http.DefaultRequestHeaders.Add("x-client-secret", _config["Cashfree:ClientSecret"]);
        _http.DefaultRequestHeaders.Add("Accept", "application/json");
        _http.DefaultRequestHeaders.Add("User-Agent", "FINX-KYC-SERVICE");
    }

    public async Task<dynamic> VerifyPan(string pan)
    {
        AddHeaders();

        var body = new
        {
            pan = pan
        };

        var response = await _http.PostAsync(
            $"{_config["Cashfree:BaseUrl"]}/pan",
            new StringContent(
                JsonConvert.SerializeObject(body),
                Encoding.UTF8,
                "application/json"));

        var json = await response.Content.ReadAsStringAsync();

        return JsonConvert.DeserializeObject<dynamic>(json);
    }

    public async Task<dynamic> VerifyBank(string account, string ifsc, string name)
    {
        AddHeaders();

        var body = new
        {
            bank_account = account,
            ifsc = ifsc,
            name = name
        };

        var res = await _http.PostAsync(
            $"{_config["Cashfree:BaseUrl"]}/bank-account",
            new StringContent(
                JsonConvert.SerializeObject(body),
                Encoding.UTF8,
                "application/json"));

        return JsonConvert.DeserializeObject(
            await res.Content.ReadAsStringAsync());
    }

    // Step 1 Verify DigiLocker account
    public async Task<dynamic> VerifyAccount(string verificationId, string aadhaar)
    {
        AddHeaders();

        var body = new
        {
            verification_id = verificationId,
            aadhaar_number = aadhaar
        };

        var res = await _http.PostAsync(
            $"{_config["Cashfree:BaseUrl"]}/digilocker/verify-account",
            new StringContent(
                JsonConvert.SerializeObject(body),
                Encoding.UTF8,
                "application/json"));

        return JsonConvert.DeserializeObject(await res.Content.ReadAsStringAsync());
    }

    // Step 2 Create DigiLocker link
    public async Task<dynamic> CreateLink(string verificationId)
    {
        AddHeaders();

        var body = new
        {
            verification_id = verificationId
        };

        var res = await _http.PostAsync(
            $"{_config["Cashfree:BaseUrl"]}/digilocker/create-link",
            new StringContent(
                JsonConvert.SerializeObject(body),
                Encoding.UTF8,
                "application/json"));

        return JsonConvert.DeserializeObject(await res.Content.ReadAsStringAsync());
    }

    // Step 3 Get verification status
    public async Task<dynamic> GetStatus(string verificationId)
    {
        AddHeaders();

        var res = await _http.GetAsync(
            $"{_config["Cashfree:BaseUrl"]}/digilocker/status/{verificationId}");

        return JsonConvert.DeserializeObject(await res.Content.ReadAsStringAsync());
    }

    // Step 4 Fetch Aadhaar document
    public async Task<dynamic> GetDocument(string verificationId)
    {
        AddHeaders();

        var res = await _http.GetAsync(
            $"{_config["Cashfree:BaseUrl"]}/digilocker/document/{verificationId}");

        return JsonConvert.DeserializeObject(await res.Content.ReadAsStringAsync());
    }
}
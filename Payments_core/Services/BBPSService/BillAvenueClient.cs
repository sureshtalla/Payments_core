using System.Net.Http;
using System.Text;

namespace Payments_core.Services.BBPSService
{
    public class BillAvenueClient : IBillAvenueClient
    {
        private readonly HttpClient _http;

        public BillAvenueClient(HttpClient http)
        {
            _http = http;
        }

        //public async Task<string> PostFormAsync(
        //    string url,
        //    Dictionary<string, string> formData)
        //{
        //    var response = await _http.PostAsync(
        //        url,
        //        new FormUrlEncodedContent(formData)
        //    );

        //    response.EnsureSuccessStatusCode();
        //    return await response.Content.ReadAsStringAsync();
        //}

   

        public async Task<string> PostFormAsync( string url, Dictionary<string, string> form)
        {
            var content = new FormUrlEncodedContent(form);
            var response = await _http.PostAsync(url, content);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        public async Task<string> PostRawAsync(
        string url,
        string body,
        string contentType)
        {
            var content = new StringContent(body, Encoding.UTF8, contentType);
            var response = await _http.PostAsync(url, content);
            return await response.Content.ReadAsStringAsync();
        }

    }
}
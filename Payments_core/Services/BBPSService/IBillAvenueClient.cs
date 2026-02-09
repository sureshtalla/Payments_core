using System.Collections.Generic;
using System.Threading.Tasks;

namespace Payments_core.Services.BBPSService
{
    public interface IBillAvenueClient
    {
        Task<string> PostFormAsync(
            string url,
            Dictionary<string, string> form
        );

        //Task<string> PostRawAsync(string url, string rawBody);
        Task<string> PostRawAsync(string url, string body, string contentType);

    }
}
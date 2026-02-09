using System;
using System.Linq;

namespace Payments_core.Helpers
{
    public static class BillAvenueRequestId
    {
        private const string AlphaNum = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

        // For Fetch / Pay / Status (BillAvenue is lenient)
        public static string Generate()
        {
            return Guid.NewGuid().ToString("N");
        }

        // ✅ STRICTLY FOR BBPS MDM
        // Format: <27 alphanumeric><YDDDhhmm>
        public static string GenerateForMDM()
        {
            var rnd = new Random();

            // 27-char alphanumeric
            var random27 = new string(
                Enumerable.Range(0, 27)
                          .Select(_ => AlphaNum[rnd.Next(AlphaNum.Length)])
                          .ToArray()
            );

            var now = DateTime.UtcNow;

            // YDDDhhmm
            var y = (now.Year % 10).ToString();      // last digit of year
            var ddd = now.DayOfYear.ToString("D3");  // day of year
            var hhmm = now.ToString("HHmm");         // 24-hour time

            return random27 + y + ddd + hhmm; // EXACTLY 35 chars
        }
    }
}
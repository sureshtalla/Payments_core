using System;
using System.Linq;

namespace Payments_core.Helpers
{
    public static class BillAvenueRequestId1
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


    public static class BillAvenueRequestId
    {
        private const string AlphaNum = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

        // ✅ FIXED: For Fetch / Pay / Status (NOW 35 chars)
        public static string Generate()
        {
            var guid32 = Guid.NewGuid().ToString("N").ToUpper(); // 32 chars

            var rnd = new Random();
            var extra = new string(
                Enumerable.Range(0, 3)
                          .Select(_ => AlphaNum[rnd.Next(AlphaNum.Length)])
                          .ToArray()
            );

            return guid32 + extra; // 35 chars
        }

        // ✅ STRICTLY FOR BBPS MDM (ALREADY PERFECT)
        // Format: <27 alphanumeric><YDDDhhmm> = 35 chars
        public static string GenerateForMDM()
        {
            var rnd = new Random();

            var random27 = new string(
                Enumerable.Range(0, 27)
                          .Select(_ => AlphaNum[rnd.Next(AlphaNum.Length)])
                          .ToArray()
            );

            var now = DateTime.UtcNow;

            var y = (now.Year % 10).ToString();
            var ddd = now.DayOfYear.ToString("D3");
            var hhmm = now.ToString("HHmm");

            return random27 + y + ddd + hhmm; // EXACTLY 35 chars
        }
    }
}
using System;

namespace Payments_core.Models.BBPS
{
    public class BbpsBillerMaster
    {
        // DB auto-increment (optional, not used in inserts)
        public long? Id { get; set; }

        // BBPS Biller Master Fields
        public string BillerId { get; set; }              // e.g. OTME00005XXZ43
        public string BillerName { get; set; }            // Biller display name
        public string Category { get; set; }              // Electricity, Telecom, etc.

        /// <summary>
        /// BBPS Fetch Requirement:
        /// Y = Fetch required
        /// N = Fetch not required
        /// M = Mandatory fetch
        /// </summary>
        public string FetchRequirement { get; set; }

        /// <summary>
        /// BBPS Payment Amount Exactness:
        /// EXACT = Exact amount only
        /// ANY   = Any amount allowed
        /// </summary>
        public string PaymentAmountExactness { get; set; }

        /// <summary>
        /// Supports Adhoc Payments
        /// true  = Supported
        /// false = Not supported
        /// </summary>
        public bool SupportsAdhoc { get; set; }

        // Managed by DB
        public DateTime? CreatedOn { get; set; }
        public DateTime? UpdatedOn { get; set; }
    }
}
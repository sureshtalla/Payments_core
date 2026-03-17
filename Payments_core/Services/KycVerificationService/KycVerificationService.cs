using Newtonsoft.Json;
using Payments_core.Integrations.Cashfree;
using Payments_core.Services.DataLayer;

namespace Payments_core.Services.KycVerificationService
{
    public class KycVerificationService : IKycVerificationService
    {
        private readonly CashfreeVerificationClient _client;
        private readonly IDapperContext _db;

        public KycVerificationService(
            CashfreeVerificationClient client,
            IDapperContext db)
        {
            _client = client;
            _db = db;
        }

        // ── PAN ───────────────────────────────────────────────────────────
        public async Task<dynamic> VerifyPan(long userId, string pan)
        {
            Console.WriteLine("STEP A: Starting PAN verification");

            var result = await _client.VerifyPan(pan);

            bool valid = false;
            string name = null;

            if (result != null)
            {
                Console.WriteLine("Cashfree Raw Response: " + result.ToString());

                if (result.valid != null)
                    valid = (bool)result.valid;

                if (result.registered_name != null)
                    name = result.registered_name?.ToString()?.Trim();
            }

            Console.WriteLine("STEP B: Calling sp_verify_pan");

            await _db.ExecuteStoredAsync(
                "sp_verify_pan",
                new
                {
                    p_user_id = userId,
                    p_pan = pan,
                    p_name = name,
                    p_verified = valid ? 1 : 0,
                    p_response = JsonConvert.SerializeObject(result)
                });

            Console.WriteLine("STEP C: DB Updated");

            return new
            {
                userId = userId,
                verified = valid,
                pan_holder_name = name
            };
        }

        public async Task<dynamic> GetBankVerificationStatus(string referenceId)
        {
            Console.WriteLine($"[GetBankVerificationStatus] ReferenceId={referenceId}");

            var result = await _client.GetBankVerificationStatus(referenceId);

            Console.WriteLine("[GetBankVerificationStatus] Cashfree raw result: " +
                JsonConvert.SerializeObject(result, Formatting.Indented));

            string accountStatus = result?.account_status?.ToString() ?? "";
            string statusCode = result?.account_status_code?.ToString() ?? "";

            bool verified = false;
            bool pending = false;

            if (statusCode == "VALIDATION_IN_PROGRESS" || accountStatus == "RECEIVED")
            {
                pending = true;
            }
            else if (
                accountStatus.Equals("VALID", StringComparison.OrdinalIgnoreCase) ||
                statusCode.Equals("ACCOUNT_IS_VALID", StringComparison.OrdinalIgnoreCase)
            )
            {
                verified = true;
                pending = false;
            }
            else
            {
                verified = false;
                pending = false;
            }

            string finalReferenceId = result?.reference_id?.ToString() ?? "";
            string finalUserId = result?.user_id?.ToString() ?? "";

            return new
            {
                verified,
                pending,
                referenceId = finalReferenceId,
                userId = finalUserId,
                status = accountStatus,
                statusCode = statusCode,
                raw = JsonConvert.DeserializeObject<object>(JsonConvert.SerializeObject(result))
            };
        }




        // ── AADHAAR STEP 1: Start DigiLocker session ──────────────────────
        public async Task<dynamic> StartAadhaarVerification(long userId, string aadhaar)
        {
            Console.WriteLine($"[StartAadhaar] userId={userId}");

            string verificationId = "FINX" + Guid.NewGuid().ToString("N")[..10];

            var verifyAccount = await _client.VerifyAccount(verificationId, aadhaar);

            Console.WriteLine("[StartAadhaar] VerifyAccount: " + JsonConvert.SerializeObject(verifyAccount));

            // ✅ URL comes directly from VerifyAccount response — no separate CreateLink call
            // Cashfree returns: { "url": "https://...", "status": "PENDING", "reference_id": ... }
            string redirectUrl = verifyAccount?.url?.ToString()
                              ?? verifyAccount?.verification_url?.ToString()
                              ?? verifyAccount?.redirect_url?.ToString()
                              ?? "";

            Console.WriteLine($"[StartAadhaar] redirect_url={redirectUrl}");

            // ✅ Cast JValue fields to string — MySqlConnector does not support JValue directly
            await _db.ExecuteStoredAsync(
                "sp_insert_aadhaar_verification",
                new
                {
                    p_user_id = userId,
                    p_verification_id = verificationId,
                    p_reference_id = verifyAccount?.reference_id?.ToString(),
                    p_digilocker_id = verifyAccount?.digilocker_id?.ToString(),
                    p_status = verifyAccount?.status?.ToString(),
                    p_response = JsonConvert.SerializeObject(verifyAccount)
                });

            return new
            {
                verification_id = verificationId,
                redirect_url = redirectUrl
            };
        }

        // ── AADHAAR STEP 2: Poll status (frontend calls every 5s) ─────────
        public async Task<dynamic> CompleteAadhaarVerification(long userId, string verificationId)
        {
            Console.WriteLine($"[CompleteAadhaar] userId={userId}, id={verificationId}");

            var status = await _client.GetStatus(verificationId);

            Console.WriteLine("[CompleteAadhaar] Status: " + JsonConvert.SerializeObject(status));

            string statusStr = (status?.status?.ToString()?.Trim() ?? "PENDING").ToUpper();

            if (statusStr != "VERIFIED")
            {
                Console.WriteLine($"[CompleteAadhaar] Not verified yet — {statusStr}");
                return new { status = statusStr };
            }

            Console.WriteLine("[CompleteAadhaar] VERIFIED — updating DB");

            // ✅ Matches your sp_verify_aadhaar exactly — only p_user_id and p_verified
            await _db.ExecuteStoredAsync(
                "sp_verify_aadhaar",
                new
                {
                    p_user_id = userId,
                    p_verified = 1
                });

            Console.WriteLine("[CompleteAadhaar] DB updated — aadhaar_verified = 1");

            return new
            {
                status = "VERIFIED",
                userId = userId,
                aadhaar_verified = true
            };
        }

        // ── BANK ──────────────────────────────────────────────────────────
        public async Task<dynamic> VerifyBank(int beneficiaryId)
        {
            Console.WriteLine($"[VerifyBank] Fetching beneficiary {beneficiaryId}");

            var rows = await _db.GetData<dynamic>(
                "sp_get_beneficiary",
                new { p_id = beneficiaryId });

            var ben = rows.FirstOrDefault();

            if (ben == null)
                throw new Exception($"Beneficiary not found for id={beneficiaryId}");

            string account = ben.AccountNumber?.ToString() ?? string.Empty;
            string ifsc = ben.IFSCCode?.ToString() ?? string.Empty;
            string name = ben.BeneficiaryName?.ToString() ?? string.Empty;

            Console.WriteLine($"[VerifyBank] Account={account}, IFSC={ifsc}, Name={name}");

            if (string.IsNullOrWhiteSpace(account) || string.IsNullOrWhiteSpace(ifsc))
                throw new Exception("Beneficiary account or IFSC is empty");

            Console.WriteLine("[VerifyBank] Calling Cashfree bank-account API");

            try
            {
                var result = await _client.VerifyBank(account, ifsc, name);

                Console.WriteLine("[VerifyBank] Cashfree raw result: " +
                    JsonConvert.SerializeObject(result, Formatting.Indented));

                string accountStatus = result?.account_status?.ToString() ?? "";
                string statusCode = result?.account_status_code?.ToString() ?? "";

                bool pending =
                    statusCode == "VALIDATION_IN_PROGRESS" ||
                    accountStatus == "RECEIVED";

                bool verified =
                    accountStatus.Equals("VALID", StringComparison.OrdinalIgnoreCase) ||
                    statusCode.Equals("ACCOUNT_IS_VALID", StringComparison.OrdinalIgnoreCase);

                string referenceId = result?.reference_id?.ToString() ?? "";
                string userId = result?.user_id?.ToString() ?? "";

                await _db.ExecuteStoredAsync(
                    "sp_verify_beneficiary_api",
                    new
                    {
                        p_id = beneficiaryId,
                        p_verified = verified ? 1 : 0,
                        p_reference = referenceId
                    });

                return new
                {
                    verified,
                    pending,
                    referenceId = referenceId,
                    userId = userId,
                    status = accountStatus,
                    statusCode = statusCode,
                    raw = JsonConvert.DeserializeObject<object>(JsonConvert.SerializeObject(result))
                };
            }
            catch (Exception apiEx)
            {
                Console.WriteLine($"[VerifyBank] Cashfree API failed: {apiEx.Message}");
                throw new Exception("Cashfree API error: " + apiEx.Message);
            }
        }
    }
}
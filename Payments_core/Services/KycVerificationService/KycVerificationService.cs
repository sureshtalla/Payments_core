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
                    name = result.registered_name.ToString();
            }

            Console.WriteLine("STEP B: Calling DB Stored Procedure");

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

        public async Task<dynamic> StartAadhaarVerification(long userId, string aadhaar)
        {
            string verificationId = "FINX" + Guid.NewGuid().ToString("N")[..10];

            var verifyAccount = await _client.VerifyAccount(
                verificationId,
                aadhaar);

            await _db.ExecuteStoredAsync(
                "sp_insert_aadhaar_verification",
                new
                {
                    p_user_id = userId,
                    p_verification_id = verificationId,
                    p_reference_id = verifyAccount?.reference_id,
                    p_digilocker_id = verifyAccount?.digilocker_id,
                    p_status = verifyAccount?.status,
                    p_response = JsonConvert.SerializeObject(verifyAccount)
                });

            var link = await _client.CreateLink(verificationId);

            return new
            {
                verification_id = verificationId,
                redirect_url = link?.verification_url?.ToString()
            };
        }

        public async Task<dynamic> CompleteAadhaarVerification(long userId, string verificationId)
        {
            var status = await _client.GetStatus(verificationId);

            if (status?.status?.ToString() != "VERIFIED")
                return status;

            var document = await _client.GetDocument(verificationId);

            string name = document?.name?.ToString();
            string last4 = document?.aadhaar_last4?.ToString();

            await _db.ExecuteStoredAsync(
                "sp_update_aadhaar_verified",
                new
                {
                    p_user_id = userId,
                    p_name = name,
                    p_last4 = last4
                });

            return document;
        }

        public async Task<bool> VerifyBank(int beneficiaryId)
        {
            var ben = (await _db.GetData<dynamic>(
                "sp_get_beneficiary",
                new { p_id = beneficiaryId })).FirstOrDefault();

            if (ben == null)
                throw new Exception("Beneficiary not found");

            var result = await _client.VerifyBank(
                ben.AccountNumber.ToString(),
                ben.IFSCCode.ToString(),
                ben.BeneficiaryName.ToString());

            bool valid = result?.account_status?.ToString() == "VALID";

            await _db.ExecuteStoredAsync(
                "sp_verify_beneficiary_api",
                new
                {
                    p_id = beneficiaryId,
                    p_verified = valid,
                    p_reference = result?.reference_id
                });

            return valid;
        }
    }
}
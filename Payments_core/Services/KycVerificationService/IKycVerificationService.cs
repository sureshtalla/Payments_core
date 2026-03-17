namespace Payments_core.Services.KycVerificationService
{
    public interface IKycVerificationService
    {
        // PAN verification
        Task<dynamic> VerifyPan(long userId, string pan);

        // Aadhaar DigiLocker flow
        Task<dynamic> StartAadhaarVerification(long userId, string aadhaar);

        Task<dynamic> CompleteAadhaarVerification(long userId, string verificationId);

        // Bank account verification
        Task<dynamic> VerifyBank(int beneficiaryId);
        Task<dynamic> GetBankVerificationStatus(string referenceId, int beneficiaryId);
    }
}
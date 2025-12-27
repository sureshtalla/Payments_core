using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Util.Store;

namespace Payments_core.Helpers
{
    public class GoogleDriveAuth
    {
        public static async Task<UserCredential> GetCredentialAsync()
        {
            using var stream = new FileStream("credentials.json", FileMode.Open, FileAccess.Read);

            return await GoogleWebAuthorizationBroker.AuthorizeAsync(
                GoogleClientSecrets.FromStream(stream).Secrets,
                new[] { DriveService.Scope.Drive },
                "system-user",
                CancellationToken.None,
                new FileDataStore("DriveToken", true)
            );
        }
    }
}

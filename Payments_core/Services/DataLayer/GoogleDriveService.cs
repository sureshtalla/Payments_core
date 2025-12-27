using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Payments_core.Helpers;

namespace Payments_core.Services.DataLayer
{
    public class GoogleDriveService
    {
        private DriveService _drive;

        // Lazy async initialization
        private async Task InitializeAsync()
        {
            if (_drive != null) return;

            var credential = await GoogleDriveAuth.GetCredentialAsync();

            _drive = new DriveService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "Payments App"
            });
        }

        //public async Task<string> UploadAsync(IFormFile file, long userId, string docType)
        //{
        //    // Ensure Drive is initialized
        //    await InitializeAsync();

        //    string rootId = await EnsureFolder("KYC", null);
        //    string userFolderId = await EnsureFolder($"User_{userId}", rootId);
        //    string docFolderId = await EnsureFolder(docType, userFolderId);

        //    var meta = new Google.Apis.Drive.v3.Data.File
        //    {
        //        Name = $"{docType}_{DateTime.UtcNow:yyyyMMddHHmmss}_{file.FileName}",
        //        Parents = new[] { docFolderId }
        //    };

        //    using var stream = file.OpenReadStream();
        //    var req = _drive.Files.Create(meta, stream, file.ContentType);
        //    req.Fields = "id";

        //    await req.UploadAsync();

        //    return $"https://drive.google.com/file/d/{req.ResponseBody.Id}/view";
        //}
        public async Task<string> UploadAsync(IFormFile file, long userId, string docType)
        {
            // Ensure Drive is initialized
            await InitializeAsync();

            string rootId = await EnsureFolder("KYC", null);
            string userFolderId = await EnsureFolder($"User_{userId}", rootId);
            string docFolderId = await EnsureFolder(docType, userFolderId);

            var meta = new Google.Apis.Drive.v3.Data.File
            {
                Name = $"{docType}_{DateTime.UtcNow:yyyyMMddHHmmss}_{file.FileName}",
                Parents = new[] { docFolderId }
            };

            using var stream = file.OpenReadStream();
            var req = _drive.Files.Create(meta, stream, file.ContentType);
            req.Fields = "id";

            await req.UploadAsync();

            // ✅ Make the file public
            var permission = new Google.Apis.Drive.v3.Data.Permission
            {
                Role = "reader",
                Type = "anyone"
            };
            await _drive.Permissions.Create(permission, req.ResponseBody.Id).ExecuteAsync();

            return $"https://drive.google.com/file/d/{req.ResponseBody.Id}/view";
        }

        private async Task<string> EnsureFolder(string name, string parentId)
        {
            var listRequest = _drive.Files.List();
            listRequest.Q = $"mimeType='application/vnd.google-apps.folder' and name='{name}'" +
                            (parentId != null ? $" and '{parentId}' in parents" : "");
            listRequest.Fields = "files(id)";

            var res = await listRequest.ExecuteAsync();
            if (res.Files.Any())
                return res.Files[0].Id;

            var folder = new Google.Apis.Drive.v3.Data.File
            {
                Name = name,
                MimeType = "application/vnd.google-apps.folder",
                Parents = parentId != null ? new[] { parentId } : null
            };

            return (await _drive.Files.Create(folder).ExecuteAsync()).Id;
        }
    }
}

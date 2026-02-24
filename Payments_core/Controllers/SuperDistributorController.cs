using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payments_core.Models;
using Payments_core.Services.DataLayer;
using Payments_core.Services.SuperDistributorService;
using Payments_core.Services.FileStorage;
using System.Formats.Tar;

namespace Payments_core.Controllers
{
   // [Authorize]
    [ApiController]
    [Route("api/superdistributor")]
    public class SuperDistributorController : Controller
    {
        private readonly ISuperDistributorService _service;
        private readonly GoogleDriveService _googleDriveService;
        private readonly ILocalFileService _fileService;

        public SuperDistributorController(ISuperDistributorService service, GoogleDriveService googleDriveService, ILocalFileService fileService) 
        {   _service = service; 
            _fileService = fileService;
            _googleDriveService = googleDriveService ?? throw new ArgumentNullException(nameof(googleDriveService));
        }
        // 🔵 Full onboarding (User + Merchant + KYC + Docs)
        //[HttpPost("full")]
        //public async Task<IActionResult> CreateFull([FromBody] SuperDistributorRequest req)
        //{
        //    var (userId, merchantId) = await _service.CreateFullOnboardingAsync(req);

        //    return Ok(new SuperDistributorResponse
        //    {
        //        UserId = userId,
        //        MerchantId = merchantId,
        //        Message = "Full onboarding created successfully"
        //    });
        //}


        //[HttpPost("full")]
        //[Consumes("multipart/form-data")]
        //public async Task<IActionResult> UpsertFull([FromForm] SuperDistributorRequest req)
        //{

        //    if (req == null)
        //        throw new ArgumentNullException(nameof(req), "Request object is null");

        //    if (_googleDriveService == null)
        //        throw new ArgumentNullException(nameof(_googleDriveService), "_googleDriveService is null");

        //    string panUrl = null;
        //    string aadhaarUrl = null;


        //    if (req.PanFile != null && req.PanFile.Length > 0)
        //    {
        //        panUrl = await _googleDriveService.UploadAsync(
        //            req.PanFile,
        //            req.UserId ?? 0,
        //            "PAN"
        //        );
        //    }


        //    if (req.AadhaarFile != null && req.AadhaarFile.Length > 0)
        //    {
        //        aadhaarUrl = await _googleDriveService.UploadAsync(
        //            req.AadhaarFile,
        //            req.UserId ?? 0,
        //            "AADHAAR"
        //        );
        //    }

        //    var (userId, merchantId) =
        //        await _service.CreateFullOnboardingAsync(req, panUrl, aadhaarUrl);

        //    return Ok(new SuperDistributorResponse
        //    {
        //        UserId = userId,
        //        MerchantId = merchantId,
        //        PanUrl = panUrl,
        //        AadhaarUrl = aadhaarUrl,
        //        Message = "Full onboarding created/updated successfully"
        //    });
        //}

        [HttpPost("full")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UpsertFull([FromForm] SuperDistributorRequest req)
        {
            if (req == null)
                throw new ArgumentNullException(nameof(req));

            string panUrl = null;
            string aadhaarUrl = null;

            // If INSERT, first create minimal user to get userId
            if (req.UserId == null || req.UserId == 0)
            {
                // call SP once without files to generate user
                var tempResult = await _service.CreateFullOnboardingAsync(req, null, null);
                req.UserId = tempResult.userId;
                req.MerchantId = tempResult.merchantId;
            }

            // Save files locally
            if (req.PanFile != null)
                panUrl = await _fileService.SaveFileAsync(req.PanFile, req.UserId.Value, "PAN");

            if (req.AadhaarFile != null)
                aadhaarUrl = await _fileService.SaveFileAsync(req.AadhaarFile, req.UserId.Value, "AADHAAR");

            // Final SP call with URLs
            var (userId, merchantId) =
                await _service.CreateFullOnboardingAsync(req, panUrl, aadhaarUrl);

            return Ok(new SuperDistributorResponse
            {
                UserId = userId,
                MerchantId = merchantId,
                PanUrl = panUrl,
                AadhaarUrl = aadhaarUrl,
                Message = "Full onboarding created/updated successfully"
            });
        }


        [HttpGet("GetCards/{roleId}/{userId}")]
        public async Task<IActionResult> GetCards(int roleId, long userId)
        {
            var data = await _service.GetCardsAsync(roleId, userId);
            return Ok(data);
        }

        [HttpGet("GetCardDetailedInfo/{roleId}/{userId}")]
        public async Task<IActionResult> GetCardDetailedInfo( int roleId, long userId)
        {
            var data = await _service.GetCardDetailedInfo( roleId, userId);
            return Ok(data);
        }

        [HttpGet("GetKYCStatus/{userId}")]
        public async Task<IActionResult> GetKYCStatus(long userId)
        {
            var data = await _service.GetKYCStatus(userId);
            return Ok(data);
        }

        [HttpGet("GetFiles/{userId}")]
        public async Task<IActionResult> GetFiles(long userId)
        {
            var data = await _service.GetFiles(userId);
            return Ok(data);
        }

    }
}

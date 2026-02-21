using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payments_core.Models;
using Payments_core.Services.MasterDataService;

namespace Payments_core.Controllers
{
    //[Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class MasterDataController : Controller
    {
        IMasterDataService masterDataService;
        public MasterDataController(IMasterDataService _masterDataService)
        {
            masterDataService = _masterDataService;
        }

        [HttpGet]
        [Route("GetAllRoles")]
        public async Task<IActionResult> GetAllRoles()
        {
            try
            {
                var data = await masterDataService.GetAllRoles();
                return Ok(data);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet]
        [Route("GetProvidersAsync")]
        public async Task<IActionResult> GetProvidersAsync()
        {
            try
            {
                var data = await masterDataService.GetProvidersAsync();
                return Ok(data);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet]
        [Route("GetProvidersList")]
        public async Task<IActionResult> GetProvidersList()
        {
            try
            {
                var data = await masterDataService.GetProvidersList();
                return Ok(data);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet]
        [Route("GetMdrPricingAsync")]
        public async Task<IActionResult> GetMdrPricingAsync()
        {
            try
            {
                var data = await masterDataService.GetMdrPricingAsync();
                return Ok(data);

            }
            catch(Exception ex) 
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet]
        [Route("GetBillerAsync/{Category}")]
        public async Task<IActionResult> GetBillerAsync(string Category)
        {
            try
            {
                var data = await masterDataService.GetBillerAsync(Category);
                return Ok(data);

            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet]
        [Route("RolebasedBusineessName/{RoleId}")]
        public async Task<IActionResult> RolebasedBusineessName(int RoleId)
        {
            try
            {
                var data = await masterDataService.RolebasedBusineessName(RoleId);
                return Ok(data);

            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
        [HttpGet]
        [Route("RolebasedUserWise/{RoleId}/{UserId}")]
        public async Task<IActionResult> RolebasedUserWise(int RoleId,int UserId)
        {
            try
            {
                var data = await masterDataService.RolebasedUserWise(RoleId, UserId);
                return Ok(data);

            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet]
        [Route("GetPaymentModes")]
        public async Task<IActionResult> GetPaymentModes()
        {
            try
            {
                var data = await masterDataService.GetPaymentModes();
                return Ok(data);

            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet]
        [Route("GetProductCategories")]
        public async Task<IActionResult> GetProductCategories()
        {
            try
            {
                var data = await masterDataService.GetProductCategories();
                return Ok(data);

            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost("AddOrUpdateProvider")]
        public async Task<IActionResult> AddOrUpdateProvider(Provider request)
        {
            try
            {
                var data = await masterDataService.AddOrUpdateProvider(request);
                return Ok(data);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost("features/global")]
        public async Task<IActionResult> UpdateGlobalFeatures(
         [FromBody] RetailerFeatureItemSaveGlobal model)
        {
            long adminId = 1; // replace later with JWT claim
            var result = await masterDataService.UpdateGlobal(model, adminId);
            return Ok(result);
        }

        [HttpGet("features/global")]
        public async Task<IActionResult> GetGlobalFeatures()
        {
            return Ok(await masterDataService.GetGlobal());
        }

        [HttpGet("features/{userId}")]
        public async Task<IActionResult> GetUserFeatures(long userId)
        {
            return Ok(await masterDataService.GetUser(userId));
        }

        [HttpPost("features/individual/bulk")]
        public async Task<IActionResult> UpdateMultipleIndividuals(
            [FromBody] BulkRetailerFeatureUpdateRequest request)
        {
            var result = await masterDataService.UpdateMultipleIndividuals(request);
            return Ok(result);
        }

        [HttpPost("features/individual/Individual")]
        public async Task<IActionResult> UpdateIndividual(
           [FromBody] RetailerFeatureItemSave request)
        {
            long adminId = 1;
            var result = await masterDataService.UpdateIndividual(request, adminId);
            return Ok(result);
        }

    }
}

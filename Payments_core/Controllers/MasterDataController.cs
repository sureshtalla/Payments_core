using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payments_core.Services.MasterDataService;

namespace Payments_core.Controllers
{
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
    }
}

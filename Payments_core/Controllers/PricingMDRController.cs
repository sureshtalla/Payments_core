using Microsoft.AspNetCore.Mvc;

namespace Payments_core.Controllers
{
    public class PricingMDRController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}

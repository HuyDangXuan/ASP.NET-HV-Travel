using Microsoft.AspNetCore.Mvc;

namespace VietVoyage.Web.Controllers
{
    public class CustomersController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}

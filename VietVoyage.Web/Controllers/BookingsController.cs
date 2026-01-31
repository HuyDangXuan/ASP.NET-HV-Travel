using Microsoft.AspNetCore.Mvc;

namespace VietVoyage.Web.Controllers
{
    public class BookingsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}

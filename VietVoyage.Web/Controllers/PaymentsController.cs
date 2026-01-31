using Microsoft.AspNetCore.Mvc;

namespace VietVoyage.Web.Controllers
{
    public class PaymentsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}

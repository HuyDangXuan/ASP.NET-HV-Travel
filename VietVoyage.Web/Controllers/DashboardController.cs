using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VietVoyage.Application.Interfaces;

namespace VietVoyage.Web.Controllers
{
    [Authorize(Roles = "Admin,Staff")]
    [Route("Admin/[controller]")]
    public class DashboardController : Controller
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet("api/revenue")]
        public async Task<IActionResult> GetRevenueStats(string range = "30d")
        {
            var stats = await _dashboardService.GetRevenueStatsAsync(range);
            return Json(stats);
        }
    }
}

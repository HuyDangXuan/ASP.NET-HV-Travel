using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HVTravel.Application.Interfaces;
using HVTravel.Domain.Interfaces;
using HVTravel.Domain.Entities;
using HVTravel.Web.Models;
using HVTravel.Web.Security;

namespace HVTravel.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(AuthenticationSchemes = AuthSchemes.AdminScheme, Roles = "Admin,Manager,Staff")]
    // [Route("Admin/[controller]")]
    public class DashboardController : Controller
    {
        private readonly IRepository<Booking> _bookingRepository;
        private readonly IRepository<Tour> _tourRepository;
        private readonly IRepository<Customer> _customerRepository;
        private readonly IDashboardService _dashboardService;

        public DashboardController(
            IRepository<Booking> bookingRepository,
            IRepository<Tour> tourRepository,
            IRepository<Customer> customerRepository,
            IDashboardService dashboardService)
        {
            _bookingRepository = bookingRepository;
            _tourRepository = tourRepository;
            _customerRepository = customerRepository;
            _dashboardService = dashboardService;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var bookings = await _bookingRepository.GetAllAsync();
                var tours = await _tourRepository.GetAllAsync();
                var customers = await _customerRepository.GetAllAsync();

                var viewModel = new DashboardViewModel
                {
                    TotalRevenue = bookings.Sum(b => b.TotalAmount),
                    TotalBookings = bookings.Count(),
                    TotalTickets = bookings.Sum(b => b.ParticipantsCount),
                    TotalCustomers = customers.Count(),
                    TotalTours = tours.Count(),
                    RecentBookings = bookings.OrderByDescending(b => b.BookingDate).Take(10).ToList(),
                    PopularTours = tours.Take(5).ToList()
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Dashboard error: {ex.Message}");
                return View(new DashboardViewModel
                {
                    RecentBookings = new List<Booking>(),
                    PopularTours = new List<Tour>()
                });
            }
        }

        [HttpGet("api/dashboard/revenue")]
        public async Task<IActionResult> GetRevenueStats([FromQuery] string range = "week")
        {
            var result = await _dashboardService.GetRevenueStatsAsync(range);
            return Ok(result);
        }
    }
}



using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HVTravel.Application.Interfaces;
using HVTravel.Domain.Interfaces;
using HVTravel.Domain.Entities;
using HVTravel.Web.Models;

namespace HVTravel.Web.Controllers
{
    [Authorize(Roles = "Admin,Staff")]
    [Route("Admin/[controller]")]
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
                RecentBookings = bookings.OrderByDescending(b => b.BookingDate).Take(10).ToList(), // Increased to 10
                PopularTours = tours.Take(5).ToList() // Just take 5 for now
            };

            return View(viewModel);
        }

        [HttpGet("api/dashboard/revenue")]
        public async Task<IActionResult> GetRevenueStats([FromQuery] string range = "week")
        {
            var result = await _dashboardService.GetRevenueStatsAsync(range);
            return Ok(result);
        }
    }
}

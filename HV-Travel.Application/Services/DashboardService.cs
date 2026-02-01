using System.Linq;
using HVTravel.Application.Interfaces;
using HVTravel.Domain.Entities;
using HVTravel.Domain.Interfaces;

namespace HVTravel.Application.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly IRepository<Booking> _bookingRepository;
        private readonly IRepository<Tour> _tourRepository;

        public DashboardService(IRepository<Booking> bookingRepository, IRepository<Tour> tourRepository)
        {
            _bookingRepository = bookingRepository;
            _tourRepository = tourRepository;
        }

        public async Task<object> GetRevenueStatsAsync(string range)
        {
            var bookings = await _bookingRepository.GetAllAsync();
            // Filter by range logic here if needed
            
            var totalRevenue = bookings.Sum(b => b.TotalAmount);
            var ticketsSold = bookings.Sum(b => b.ParticipantsCount);
            var newBookings = bookings.Count();

            // Mock chart data
            var chartData = new {
                labels = new[] { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" },
                datasets = new[] {
                    new {
                        label = "Revenue",
                        data = new[] { 1200, 1900, 3000, 5000, 2000, 3000, 4500 },
                        borderColor = "#4facfe",
                        backgroundColor = "rgba(79, 172, 254, 0.2)"
                    }
                }
            };

            return new
            {
                TotalRevenue = totalRevenue,
                TicketsSold = ticketsSold,
                NewBookings = newBookings,
                ChartData = chartData
            };
        }

        public async Task<object> GetRecentBookingsAsync()
        {
            var bookings = await _bookingRepository.GetAllAsync();
            return bookings.OrderByDescending(b => b.BookingDate).Take(5);
        }
    }
}

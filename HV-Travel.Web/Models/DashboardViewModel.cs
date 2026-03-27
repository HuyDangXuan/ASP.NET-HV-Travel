using HVTravel.Application.Models;
using HVTravel.Domain.Entities;

namespace HVTravel.Web.Models
{
    public class DashboardViewModel
    {
        public decimal TotalRevenue { get; set; }
        public int TotalBookings { get; set; }
        public int TotalTickets { get; set; }
        public int TotalCustomers { get; set; }
        public int TotalTours { get; set; }
        public IEnumerable<Booking> RecentBookings { get; set; } = Array.Empty<Booking>();
        public IEnumerable<Tour> PopularTours { get; set; } = Array.Empty<Tour>();
        public DashboardRevenueStatsResult RevenueChart { get; set; } = new();

        public double? RevenueGrowth { get; set; }
        public double BookingsGrowth { get; set; } = 8.2;
        public double CustomersGrowth { get; set; } = 5.1;
    }
}

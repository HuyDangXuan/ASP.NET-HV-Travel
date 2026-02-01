using HVTravel.Domain.Entities;

namespace HVTravel.Web.Models
{
    public class DashboardViewModel
    {
        public decimal TotalRevenue { get; set; }
        public int TotalBookings { get; set; }
        public int TotalTickets { get; set; } // Add this
        public int TotalCustomers { get; set; }
        public int TotalTours { get; set; }
        public IEnumerable<Booking> RecentBookings { get; set; }
        public IEnumerable<Tour> PopularTours { get; set; } // Mock or simple logic
        
        // Growth stats (Mock for now)
        public double RevenueGrowth { get; set; } = 12.5;
        public double BookingsGrowth { get; set; } = 8.2;
        public double CustomersGrowth { get; set; } = 5.1;
    }
}

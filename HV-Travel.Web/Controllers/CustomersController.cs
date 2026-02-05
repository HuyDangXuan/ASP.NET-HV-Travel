using Microsoft.AspNetCore.Mvc;
using HVTravel.Domain.Interfaces;
using HVTravel.Domain.Entities;
using HVTravel.Web.Models;
using System.Threading.Tasks;

namespace HVTravel.Web.Controllers
{
    public class CustomersController : Controller
    {
        private readonly IRepository<Customer> _customerRepository;
        private readonly IRepository<Booking> _bookingRepository;

        public CustomersController(IRepository<Customer> customerRepository, IRepository<Booking> bookingRepository)
        {
            _customerRepository = customerRepository;
            _bookingRepository = bookingRepository;
        }

        public async Task<IActionResult> Index(
            string searchQuery,
            string[] segments,
            decimal? minSpending,
            int? minOrders,
            string sortOrder,
            int page = 1, int pageSize = 10)
        {
            ViewBag.CurrentSort = sortOrder;
            ViewBag.NameSortParm = String.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewBag.SpendingSortParm = sortOrder == "spending" ? "spending_desc" : "spending";
            ViewBag.OrdersSortParm = sortOrder == "orders" ? "orders_desc" : "orders";

            var customers = await _customerRepository.GetAllAsync();
            var allBookings = await _bookingRepository.GetAllAsync();

            // 1. Calculate Stats for ALL customers first (needed for filtering)
            var customerStats = allBookings
                .GroupBy(b => b.CustomerId)
                .ToDictionary(g => g.Key, g => new
                {
                    TotalOrders = g.Count(),
                    TotalSpending = g.Where(b => b.Status != "Cancelled" && b.Status != "Refunded").Sum(b => b.TotalAmount)
                });

            foreach (var customer in customers)
            {
                if (customerStats.ContainsKey(customer.Id))
                {
                    customer.Stats.TotalOrders = customerStats[customer.Id].TotalOrders;
                    customer.Stats.TotalSpending = customerStats[customer.Id].TotalSpending;
                }
                else
                {
                    customer.Stats.TotalOrders = 0;
                    customer.Stats.TotalSpending = 0;
                }
            }

            // Filter by Search Query
            if (!string.IsNullOrEmpty(searchQuery))
            {
                customers = customers.Where(c => 
                    c.FullName.Contains(searchQuery, StringComparison.OrdinalIgnoreCase) || 
                    c.Email.Contains(searchQuery, StringComparison.OrdinalIgnoreCase) || 
                    c.PhoneNumber.Contains(searchQuery, StringComparison.OrdinalIgnoreCase));
            }

            // Filter by Segments
            if (segments != null && segments.Any())
            {
                customers = customers.Where(c => segments.Contains(c.Segment));
            }

            // Filter by Spending and Orders
            if (minSpending.HasValue)
            {
                customers = customers.Where(c => c.Stats.TotalSpending >= minSpending.Value);
            }

            if (minOrders.HasValue)
            {
                customers = customers.Where(c => c.Stats.TotalOrders >= minOrders.Value);
            }


            // Persist Filter State
            ViewBag.CurrentFilter = searchQuery;
            ViewBag.SelectedSegments = segments ?? new string[0];
            ViewBag.MinSpending = minSpending;
            ViewBag.MinOrders = minOrders;

            // 4. Stats Summary (Calculated on Filtered List)


            var stats = new CustomerStatsSummary
            {
                TotalCustomers = customers.Count(),
                NewCustomersCount = customers.Count(c => c.Segment == "New"),
                SegmentPercentages = CalculateSegmentPercentages(customers)
            };

            // 4. Pagination
            var totalCount = customers.Count();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            var items = customers.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            var pagination = new PaginationMetadata
            {
                CurrentPage = page,
                TotalPages = totalPages,
                PageSize = pageSize,
                TotalCount = totalCount
            };

            var viewModel = new CustomerIndexViewModel
            {
                Customers = items,
                Pagination = pagination,
                Stats = stats
            };

            return View(viewModel);
        }

        private Dictionary<string, double> CalculateSegmentPercentages(IEnumerable<Customer> customers)
        {
            var total = customers.Count();
            if (total == 0) return new Dictionary<string, double>();

            var segments = new[] { "VIP", "New", "Standard", "ChurnRisk", "Nguy Cơ Rời Bỏ" }; // Add other segments as needed
            var result = new Dictionary<string, double>();

            foreach (var segment in segments)
            {
                var count = customers.Count(c => c.Segment == segment); // Case sensitive? Standardize data if needed
                // "Nguy Cơ Rời Bỏ" might map to "ChurnRisk" in code if using Enums, but here assuming string match
                if (segment == "Nguy Cơ Rời Bỏ") continue; // Skip display name, use internal code if cleaner
                
                // Grouping ChurnRisk/Nguy Cơ Rời Bỏ if needed
                if(segment == "ChurnRisk") {
                     count += customers.Count(c => c.Segment == "Nguy Cơ Rời Bỏ");
                }

                result[segment] = Math.Round((double)count / total * 100, 1);
            }
            
            // Catch-all or others?
            // For the requested "Standard", "VIP", "New", "Churn" chart:
            // Ensure we handle "Standard" explicitly if it exists in data, or calculate it as remainder?
            // Assuming data has "Standard" segment.

            return result;
        }


        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Customer customer)
        {
            if (ModelState.IsValid)
            {
                if (string.IsNullOrEmpty(customer.CustomerCode))
                {
                    customer.CustomerCode = $"KH-{DateTime.UtcNow.Ticks.ToString().Substring(12)}";
                }
                
                customer.CreatedAt = DateTime.UtcNow;
                customer.UpdatedAt = DateTime.UtcNow;
                customer.Stats = new CustomerStats(); // Initialize empty stats
                
                if (customer.Address == null)
                {
                    customer.Address = new Address();
                }

                await _customerRepository.AddAsync(customer);
                return RedirectToAction(nameof(Index));
            }
            return View(customer);
        }

        [HttpGet]
        public async Task<IActionResult> GetDetails(string id)
        {
            var customer = await _customerRepository.GetByIdAsync(id);
            if (customer == null) return NotFound();

            var bookings = await _bookingRepository.FindAsync(b => b.CustomerId == id);
            
            // Calculate dynamic stats
            var validBookings = bookings.Where(b => b.Status != "Cancelled" && b.Status != "Refunded");
            customer.Stats.TotalOrders = bookings.Count();
            customer.Stats.TotalSpending = validBookings.Sum(b => b.TotalAmount);
            
            // Map bookings to simplified history structure
            var history = bookings.OrderByDescending(b => b.CreatedAt).Select(b => new 
            {
                month = b.CreatedAt.Month,
                day = b.CreatedAt.Day,
                title = b.TourSnapshot?.Name ?? "Unknown Tour",
                status = GetStatusVietnamese(b.Status), // You might need to move the helper or duplicate it
                detail = b.TourSnapshot?.Duration ?? "N/A",
                sub = b.BookingCode
            });

            return Json(new 
            { 
                customer,
                history
            });
        }
        
        // Helper to keep consistency (simplified version of BookingsController helper)
        private string GetStatusVietnamese(string status)
        {
            if (string.IsNullOrEmpty(status)) return "N/A";
            switch(status.ToLower())
            {
                case "pending": return "Chờ xử lý";
                case "confirmed": return "Đã xác nhận";
                case "completed": return "Hoàn thành";
                case "cancelled": return "Đã hủy";
                case "refunded": return "Đã hoàn tiền";
                default: return status;
            }
        }
    }
}

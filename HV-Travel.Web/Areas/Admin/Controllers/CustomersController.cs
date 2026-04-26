using HVTravel.Application.Interfaces;
using HVTravel.Application.Models;
using HVTravel.Domain.Entities;
using HVTravel.Domain.Interfaces;
using HVTravel.Web.Models;
using HVTravel.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HVTravel.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(AuthenticationSchemes = AuthSchemes.AdminScheme, Roles = "Admin,Staff")]
    public class CustomersController : Controller
    {
        private readonly IRepository<Customer> _customerRepository;
        private readonly IRepository<Booking> _bookingRepository;
        private readonly IAdminCustomerSearchService? _adminCustomerSearchService;
        private readonly ISearchIndexingService? _searchIndexingService;

        public CustomersController(
            IRepository<Customer> customerRepository,
            IRepository<Booking> bookingRepository,
            IAdminCustomerSearchService? adminCustomerSearchService = null,
            ISearchIndexingService? searchIndexingService = null)
        {
            _customerRepository = customerRepository;
            _bookingRepository = bookingRepository;
            _adminCustomerSearchService = adminCustomerSearchService;
            _searchIndexingService = searchIndexingService;
        }

        [Authorize(Roles = "Admin,Manager,Staff,Guide")]
        public async Task<IActionResult> Index(
            string searchQuery,
            string[] segments,
            decimal? minSpending,
            int? minOrders,
            string sortOrder,
            int page = 1,
            int pageSize = 10)
        {
            page = Math.Max(page, 1);
            pageSize = pageSize <= 0 ? 10 : pageSize;

            ViewBag.CurrentSort = sortOrder;
            ViewBag.NameSortParm = string.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewBag.SpendingSortParm = sortOrder == "spending" ? "spending_desc" : "spending";
            ViewBag.OrdersSortParm = sortOrder == "orders" ? "orders_desc" : "orders";
            ViewBag.CurrentFilter = searchQuery;
            ViewBag.SelectedSegments = segments ?? Array.Empty<string>();
            ViewBag.MinSpending = minSpending;
            ViewBag.MinOrders = minOrders;
            ViewBag.CurrentPageSize = pageSize;

            if (_adminCustomerSearchService != null)
            {
                var result = await _adminCustomerSearchService.SearchAsync(new AdminCustomerSearchRequest
                {
                    SearchQuery = searchQuery ?? string.Empty,
                    Segments = segments ?? Array.Empty<string>(),
                    MinSpending = minSpending,
                    MinOrders = minOrders,
                    SortOrder = sortOrder ?? string.Empty,
                    Page = page,
                    PageSize = pageSize
                });

                return View(new CustomerIndexViewModel
                {
                    Customers = result.Page.Items,
                    Pagination = new PaginationMetadata
                    {
                        CurrentPage = result.Page.PageIndex,
                        TotalPages = result.Page.TotalPages,
                        PageSize = result.Page.PageSize,
                        TotalCount = result.Page.TotalCount
                    },
                    Stats = new CustomerStatsSummary
                    {
                        TotalCustomers = result.TotalCustomers,
                        NewCustomersCount = result.NewCustomersCount,
                        SegmentPercentages = result.SegmentPercentages.ToDictionary(item => item.Key, item => item.Value)
                    }
                });
            }

            var customers = await _customerRepository.GetAllAsync();
            var allBookings = await _bookingRepository.GetAllAsync();

            var customerStats = new Dictionary<string, (int TotalOrders, decimal TotalSpending)>();
            foreach (var booking in allBookings)
            {
                if (string.IsNullOrEmpty(booking.CustomerId))
                {
                    continue;
                }

                if (!customerStats.ContainsKey(booking.CustomerId))
                {
                    customerStats[booking.CustomerId] = (0, 0m);
                }

                var aggregate = customerStats[booking.CustomerId];
                aggregate.TotalOrders++;
                if (booking.Status != "Cancelled" && booking.Status != "Refunded")
                {
                    aggregate.TotalSpending += booking.TotalAmount;
                }

                customerStats[booking.CustomerId] = aggregate;
            }

            foreach (var customer in customers)
            {
                customer.Stats ??= new CustomerStats();
                if (customerStats.ContainsKey(customer.Id))
                {
                    customer.Stats.TotalOrders = customerStats[customer.Id].TotalOrders;
                    customer.Stats.TotalSpending = customerStats[customer.Id].TotalSpending;
                }
                else
                {
                    customer.Stats.TotalOrders = 0;
                    customer.Stats.TotalSpending = 0m;
                }
            }

            if (!string.IsNullOrEmpty(searchQuery))
            {
                customers = customers.Where(customer =>
                    customer.FullName.Contains(searchQuery, StringComparison.OrdinalIgnoreCase) ||
                    customer.Email.Contains(searchQuery, StringComparison.OrdinalIgnoreCase) ||
                    customer.PhoneNumber.Contains(searchQuery, StringComparison.OrdinalIgnoreCase));
            }

            if (segments != null && segments.Any())
            {
                customers = customers.Where(customer => segments.Contains(customer.Segment));
            }

            if (minSpending.HasValue)
            {
                customers = customers.Where(customer => customer.Stats.TotalSpending >= minSpending.Value);
            }

            if (minOrders.HasValue)
            {
                customers = customers.Where(customer => customer.Stats.TotalOrders >= minOrders.Value);
            }

            customers = sortOrder switch
            {
                "name_desc" => customers.OrderByDescending(customer => customer.FullName),
                "spending" => customers.OrderBy(customer => customer.Stats.TotalSpending),
                "spending_desc" => customers.OrderByDescending(customer => customer.Stats.TotalSpending),
                "orders" => customers.OrderBy(customer => customer.Stats.TotalOrders),
                "orders_desc" => customers.OrderByDescending(customer => customer.Stats.TotalOrders),
                _ => customers.OrderBy(customer => customer.FullName)
            };

            var filteredCustomers = customers.ToList();
            var totalCount = filteredCustomers.Count;
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            var items = filteredCustomers.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            return View(new CustomerIndexViewModel
            {
                Customers = items,
                Pagination = new PaginationMetadata
                {
                    CurrentPage = page,
                    TotalPages = totalPages,
                    PageSize = pageSize,
                    TotalCount = totalCount
                },
                Stats = new CustomerStatsSummary
                {
                    TotalCustomers = totalCount,
                    NewCustomersCount = filteredCustomers.Count(customer => customer.Segment == "New"),
                    SegmentPercentages = CalculateSegmentPercentages(filteredCustomers)
                }
            });
        }

        [Authorize(Roles = "Admin,Manager,Staff")]
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [Authorize(Roles = "Admin,Manager,Staff")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Customer customer)
        {
            if (!ModelState.IsValid)
            {
                return View(customer);
            }

            if (string.IsNullOrEmpty(customer.CustomerCode))
            {
                customer.CustomerCode = $"KH-{DateTime.UtcNow.Ticks.ToString().Substring(12)}";
            }

            customer.CreatedAt = DateTime.UtcNow;
            customer.UpdatedAt = DateTime.UtcNow;
            customer.Stats = new CustomerStats();
            customer.Address ??= new Address();

            await _customerRepository.AddAsync(customer);
            await (_searchIndexingService?.UpsertCustomerAsync(customer) ?? Task.CompletedTask);
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin,Manager,Staff,Guide")]
        [HttpGet]
        public async Task<IActionResult> GetDetails(string id)
        {
            var customer = await _customerRepository.GetByIdAsync(id);
            if (customer == null)
            {
                return NotFound();
            }

            var bookings = await _bookingRepository.FindAsync(booking => booking.CustomerId == id);
            var bookingList = bookings.ToList();
            var validBookings = bookingList.Where(booking => booking.Status != "Cancelled" && booking.Status != "Refunded");
            customer.Stats.TotalOrders = bookingList.Count;
            customer.Stats.TotalSpending = validBookings.Sum(booking => booking.TotalAmount);

            var history = bookingList
                .OrderByDescending(booking => booking.CreatedAt)
                .Select(booking => new
                {
                    month = booking.CreatedAt.Month,
                    day = booking.CreatedAt.Day,
                    title = booking.TourSnapshot?.Name ?? "Unknown Tour",
                    status = GetStatusVietnamese(booking.Status),
                    detail = booking.TourSnapshot?.Duration ?? "N/A",
                    sub = booking.BookingCode
                });

            return Json(new
            {
                customer,
                history
            });
        }

        private static Dictionary<string, double> CalculateSegmentPercentages(IEnumerable<Customer> customers)
        {
            var list = customers.ToList();
            var total = list.Count;
            if (total == 0)
            {
                return new Dictionary<string, double>();
            }

            var result = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            foreach (var segment in new[] { "VIP", "New", "Standard", "ChurnRisk" })
            {
                var count = list.Count(customer => customer.Segment == segment);
                if (segment == "ChurnRisk")
                {
                    count += list.Count(customer => customer.Segment == "Nguy Cơ Rời Bỏ");
                }

                result[segment] = Math.Round(count / (double)total * 100d, 1);
            }

            return result;
        }

        private static string GetStatusVietnamese(string status)
        {
            if (string.IsNullOrEmpty(status))
            {
                return "N/A";
            }

            return status.ToLowerInvariant() switch
            {
                "pending" => "Chờ xử lý",
                "confirmed" => "Đã xác nhận",
                "completed" => "Hoàn thành",
                "cancelled" => "Đã hủy",
                "refunded" => "Đã hoàn tiền",
                _ => status
            };
        }
    }
}

using HVTravel.Application.Interfaces;
using HVTravel.Application.Models;
using HVTravel.Domain.Entities;
using HVTravel.Domain.Interfaces;
using HVTravel.Domain.Models;
using HVTravel.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HVTravel.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(AuthenticationSchemes = AuthSchemes.AdminScheme, Roles = "Admin,Accountant")]
    public class PaymentsController : Controller
    {
        private readonly IRepository<Payment> _paymentRepository;
        private readonly IRepository<Booking> _bookingRepository;
        private readonly IAdminPaymentSearchService? _adminPaymentSearchService;

        public PaymentsController(
            IRepository<Payment> paymentRepository,
            IRepository<Booking> bookingRepository,
            IAdminPaymentSearchService? adminPaymentSearchService = null)
        {
            _paymentRepository = paymentRepository;
            _bookingRepository = bookingRepository;
            _adminPaymentSearchService = adminPaymentSearchService;
        }

        public async Task<IActionResult> Index(
            string sortOrder = "",
            string bookingStatusFilter = "",
            string paymentStatusFilter = "",
            string searchString = "",
            int page = 1,
            int pageSize = 10)
        {
            if (page < 1)
            {
                page = 1;
            }

            if (pageSize < 5)
            {
                pageSize = 10;
            }
            else if (pageSize > 100)
            {
                pageSize = 100;
            }

            ApplyViewState(sortOrder, bookingStatusFilter, paymentStatusFilter, searchString, pageSize);

            if (_adminPaymentSearchService != null)
            {
                var result = await _adminPaymentSearchService.SearchAsync(new AdminPaymentSearchRequest
                {
                    SortOrder = sortOrder,
                    BookingStatusFilter = bookingStatusFilter,
                    PaymentStatusFilter = paymentStatusFilter,
                    SearchString = searchString,
                    Page = page,
                    PageSize = pageSize
                });

                ViewBag.TotalRevenue = result.TotalRevenue;
                ViewBag.TotalRefunded = result.TotalRefunded;
                ViewBag.FilteredBookingsCount = result.FilteredBookingsCount;
                ViewBag.SuccessfulPaymentsCount = result.SuccessfulPaymentsCount;
                ViewBag.RefundBookings = result.RefundBookings;
                return View(result.Page);
            }

            var allBookings = (await _bookingRepository.GetAllAsync()).ToList();
            ViewBag.TotalRevenue = allBookings
                .Where(booking => booking.Status == "Completed" && booking.PaymentStatus == "Full")
                .Sum(booking => booking.TotalAmount);
            ViewBag.TotalRefunded = allBookings
                .Where(booking => booking.Status == "Refunded" && booking.PaymentStatus == "Refunded")
                .Sum(booking => booking.TotalAmount);

            IEnumerable<Booking> filteredBookings = allBookings;
            var normalizedSearch = searchString?.Trim();
            if (!string.IsNullOrWhiteSpace(normalizedSearch))
            {
                filteredBookings = filteredBookings.Where(booking =>
                    (!string.IsNullOrWhiteSpace(booking.Id) && booking.Id.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrWhiteSpace(booking.BookingCode) && booking.BookingCode.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase)));
            }

            if (!string.IsNullOrEmpty(paymentStatusFilter))
            {
                filteredBookings = filteredBookings.Where(booking => booking.PaymentStatus == paymentStatusFilter);
            }

            if (!string.IsNullOrEmpty(bookingStatusFilter))
            {
                filteredBookings = filteredBookings.Where(booking => booking.Status == bookingStatusFilter);
            }

            var sortedBookings = sortOrder switch
            {
                "id_desc" => filteredBookings.OrderByDescending(booking => booking.Id),
                "date" => filteredBookings.OrderBy(booking => booking.BookingDate),
                "date_desc" => filteredBookings.OrderByDescending(booking => booking.BookingDate),
                "booking" => filteredBookings.OrderBy(booking => booking.BookingCode),
                "booking_desc" => filteredBookings.OrderByDescending(booking => booking.BookingCode),
                "status" => filteredBookings.OrderBy(booking => booking.PaymentStatus),
                "status_desc" => filteredBookings.OrderByDescending(booking => booking.PaymentStatus),
                "booking_status" => filteredBookings.OrderBy(booking => booking.Status),
                "booking_status_desc" => filteredBookings.OrderByDescending(booking => booking.Status),
                "amount" => filteredBookings.OrderBy(booking => booking.TotalAmount),
                "amount_desc" => filteredBookings.OrderByDescending(booking => booking.TotalAmount),
                _ => filteredBookings.OrderByDescending(booking => booking.BookingDate)
            };

            var filteredBookingsList = sortedBookings.ToList();
            ViewBag.FilteredBookingsCount = filteredBookingsList.Count;
            ViewBag.SuccessfulPaymentsCount = filteredBookingsList.Count(booking =>
                booking.PaymentStatus == "Full" ||
                booking.PaymentStatus == "Success" ||
                booking.PaymentStatus == "Paid");
            ViewBag.RefundBookings = filteredBookingsList
                .Where(booking => booking.Status == "Refunded" && booking.PaymentStatus == "Refunded")
                .ToList();

            var pagedItems = filteredBookingsList.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            return View(new PaginatedResult<Booking>(pagedItems, filteredBookingsList.Count, page, pageSize));
        }

        private void ApplyViewState(string sortOrder, string bookingStatusFilter, string paymentStatusFilter, string searchString, int pageSize)
        {
            ViewBag.CurrentSort = sortOrder;
            ViewBag.IdSortParm = string.IsNullOrEmpty(sortOrder) ? "id_desc" : "";
            ViewBag.DateSortParm = sortOrder == "date" ? "date_desc" : "date";
            ViewBag.BookingIdSortParm = sortOrder == "booking" ? "booking_desc" : "booking";
            ViewBag.MethodSortParm = sortOrder == "method" ? "method_desc" : "method";
            ViewBag.StatusSortParm = sortOrder == "status" ? "status_desc" : "status";
            ViewBag.BookingStatusSortParm = sortOrder == "booking_status" ? "booking_status_desc" : "booking_status";
            ViewBag.AmountSortParm = sortOrder == "amount" ? "amount_desc" : "amount";
            ViewBag.CurrentBookingStatusFilter = bookingStatusFilter;
            ViewBag.CurrentPaymentStatusFilter = paymentStatusFilter;
            ViewData["CurrentSearch"] = searchString;
            ViewData["CurrentPageSize"] = pageSize;
        }
    }
}

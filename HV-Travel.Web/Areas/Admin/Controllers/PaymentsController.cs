using HVTravel.Domain.Entities;
using HVTravel.Domain.Interfaces;
using HVTravel.Domain.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using HVTravel.Web.Security;

namespace HVTravel.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(AuthenticationSchemes = AuthSchemes.AdminScheme, Roles = "Admin,Accountant")]
    public class PaymentsController : Controller
    {
        private readonly IRepository<Payment> _paymentRepository;
        private readonly IRepository<Booking> _bookingRepository;

        public PaymentsController(IRepository<Payment> paymentRepository, IRepository<Booking> bookingRepository)
        {
            _paymentRepository = paymentRepository;
            _bookingRepository = bookingRepository;
        }

        public async Task<IActionResult> Index(
            string sortOrder = "",
            string bookingStatusFilter = "",
            string paymentStatusFilter = "",
            string searchString = "",
            int page = 1,
            int pageSize = 10)
        {
            if (page < 1) page = 1;
            if (pageSize < 5) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

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

            var allBookings = (await _bookingRepository.GetAllAsync()).ToList();
            var totalRevenue = allBookings
                .Where(b => b.Status == "Completed" && b.PaymentStatus == "Full")
                .Sum(b => b.TotalAmount);
            ViewBag.TotalRevenue = totalRevenue;

            var totalRefunded = allBookings
                .Where(b => b.Status == "Refunded" && b.PaymentStatus == "Refunded")
                .Sum(b => b.TotalAmount);
            ViewBag.TotalRefunded = totalRefunded;

            IEnumerable<Booking> filteredBookings = allBookings;

            var normalizedSearch = searchString?.Trim();
            if (!string.IsNullOrWhiteSpace(normalizedSearch))
            {
                filteredBookings = filteredBookings.Where(b =>
                    (!string.IsNullOrWhiteSpace(b.Id) && b.Id.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrWhiteSpace(b.BookingCode) && b.BookingCode.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase)));
            }

            if (!string.IsNullOrEmpty(paymentStatusFilter))
            {
                filteredBookings = filteredBookings.Where(b => b.PaymentStatus == paymentStatusFilter);
            }

            if (!string.IsNullOrEmpty(bookingStatusFilter))
            {
                filteredBookings = filteredBookings.Where(b => b.Status == bookingStatusFilter);
            }

            var sortedBookings = sortOrder switch
            {
                "id_desc" => filteredBookings.OrderByDescending(p => p.Id),
                "date" => filteredBookings.OrderBy(p => p.BookingDate),
                "date_desc" => filteredBookings.OrderByDescending(p => p.BookingDate),
                "booking" => filteredBookings.OrderBy(p => p.BookingCode),
                "booking_desc" => filteredBookings.OrderByDescending(p => p.BookingCode),
                "status" => filteredBookings.OrderBy(p => p.PaymentStatus),
                "status_desc" => filteredBookings.OrderByDescending(p => p.PaymentStatus),
                "booking_status" => filteredBookings.OrderBy(p => p.Status),
                "booking_status_desc" => filteredBookings.OrderByDescending(p => p.Status),
                "amount" => filteredBookings.OrderBy(p => p.TotalAmount),
                "amount_desc" => filteredBookings.OrderByDescending(p => p.TotalAmount),
                _ => filteredBookings.OrderByDescending(p => p.BookingDate)
            };

            var filteredBookingsList = sortedBookings.ToList();
            var totalCount = filteredBookingsList.Count;
            var pagedItems = filteredBookingsList.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            var pagedBookings = new PaginatedResult<Booking>(pagedItems, totalCount, page, pageSize);

            ViewBag.FilteredBookingsCount = totalCount;
            ViewBag.SuccessfulPaymentsCount = filteredBookingsList.Count(b =>
                b.PaymentStatus == "Full" ||
                b.PaymentStatus == "Success" ||
                b.PaymentStatus == "Paid");
            ViewBag.RefundBookings = filteredBookingsList
                .Where(b => b.Status == "Refunded" && b.PaymentStatus == "Refunded")
                .ToList();

            return View(pagedBookings);
        }
    }
}

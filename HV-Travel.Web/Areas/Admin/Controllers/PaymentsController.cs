using HVTravel.Domain.Interfaces;
using HVTravel.Domain.Entities;
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

        public async Task<IActionResult> Index(string sortOrder = "", string bookingStatusFilter = "", string paymentStatusFilter = "", string searchString = "")
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

            var bookings = await _bookingRepository.GetAllAsync();
            var totalRevenue = bookings
                .Where(b => b.Status == "Completed" && b.PaymentStatus == "Full")
                .Sum(b => b.TotalAmount);
            ViewBag.TotalRevenue = totalRevenue;

            var totalRefunded = bookings
                .Where(b => b.Status == "Refunded" && b.PaymentStatus == "Refunded")
                .Sum(b => b.TotalAmount);
            ViewBag.TotalRefunded = totalRefunded;

            if (!string.IsNullOrEmpty(paymentStatusFilter))
            {
                bookings = bookings.Where(b => b.PaymentStatus == paymentStatusFilter);
            }

            if (!string.IsNullOrEmpty(bookingStatusFilter))
            {
                bookings = bookings.Where(b => b.Status == bookingStatusFilter);
            }

            bookings = sortOrder switch
            {
                "id_desc" => bookings.OrderByDescending(p => p.Id),
                "date" => bookings.OrderBy(p => p.BookingDate),
                "date_desc" => bookings.OrderByDescending(p => p.BookingDate),
                "booking" => bookings.OrderBy(p => p.BookingCode),
                "booking_desc" => bookings.OrderByDescending(p => p.BookingCode),
                "status" => bookings.OrderBy(p => p.PaymentStatus),
                "status_desc" => bookings.OrderByDescending(p => p.PaymentStatus),
                "booking_status" => bookings.OrderBy(p => p.Status),
                "booking_status_desc" => bookings.OrderByDescending(p => p.Status),
                "amount" => bookings.OrderBy(p => p.TotalAmount),
                "amount_desc" => bookings.OrderByDescending(p => p.TotalAmount),
                _ => bookings.OrderByDescending(p => p.BookingDate) // default to newest first
            };

            return View(bookings);
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using HVTravel.Domain.Interfaces;
using HVTravel.Domain.Entities;
using HVTravel.Domain.Models;
using System.Threading.Tasks;

namespace HVTravel.Web.Controllers
{
    public class BookingsController : Controller
    {
        private readonly IRepository<Booking> _bookingRepository;

        public BookingsController(IRepository<Booking> bookingRepository)
        {
            _bookingRepository = bookingRepository;
        }

        public async Task<IActionResult> Index(
            string status = "all",
            string searchString = "",
            string startDate = "",
            string endDate = "",
            string sortOrder = "",
            int page = 1, int pageSize = 10)
        {
            if (pageSize < 5) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            // Sort Params
            ViewBag.CurrentSort = sortOrder;
            ViewBag.DateSortParm = String.IsNullOrEmpty(sortOrder) ? "date_asc" : ""; // Default desc, toggle to asc
            ViewBag.TotalSortParm = sortOrder == "total" ? "total_desc" : "total";
            ViewBag.StatusSortParm = sortOrder == "status" ? "status_desc" : "status";

            var statusLower = status?.ToLower() ?? "all";
            string? targetStatus = null;

            if (statusLower == "paid") targetStatus = "Paid";
            else if (statusLower == "pending") targetStatus = "Pending";
            else if (statusLower == "cancelled") targetStatus = "Cancelled";
            else if (statusLower == "confirmed") targetStatus = "Confirmed";
            else if (statusLower == "completed") targetStatus = "Completed";
            else if (statusLower == "refunded") targetStatus = "Refunded";

            var search = !string.IsNullOrEmpty(searchString) ? searchString.Trim().ToLower() : null;
            DateTime? start = null; 
            DateTime? end = null;

            // Handle date
            if (!string.IsNullOrEmpty(startDate) && DateTime.TryParse(startDate, out var s)) start = s;
            if (!string.IsNullOrEmpty(endDate) && DateTime.TryParse(endDate, out var e)) end = e;
            
            if (end.HasValue) end = end.Value.Date.AddDays(1).AddTicks(-1);

            System.Linq.Expressions.Expression<Func<Booking, bool>> filter = b =>
                (targetStatus == null || b.Status == targetStatus || b.PaymentStatus == targetStatus) &&
                (search == null || 
                    (b.BookingCode != null && b.BookingCode.ToLower().Contains(search)) || 
                    (b.ContactInfo != null && (
                        (b.ContactInfo.Name != null && b.ContactInfo.Name.ToLower().Contains(search)) || 
                        (b.ContactInfo.Email != null && b.ContactInfo.Email.ToLower().Contains(search))
                    ))
                ) &&
                (start == null || b.CreatedAt >= start) &&
                (end == null || b.CreatedAt <= end);

            // Fetch ALL matching items to Sort in Memory
            var bookingsList = await _bookingRepository.FindAsync(filter);
            
            // Apply Sorting
            switch (sortOrder)
            {
                case "date_asc":
                    bookingsList = bookingsList.OrderBy(b => b.CreatedAt);
                    break;
                case "total":
                    bookingsList = bookingsList.OrderBy(b => b.TotalAmount);
                    break;
                case "total_desc":
                    bookingsList = bookingsList.OrderByDescending(b => b.TotalAmount);
                    break;
                case "status":
                    bookingsList = bookingsList.OrderBy(b => b.Status);
                    break;
                case "status_desc":
                    bookingsList = bookingsList.OrderByDescending(b => b.Status);
                    break;
                default: // Default: Date Descending
                    bookingsList = bookingsList.OrderByDescending(b => b.CreatedAt);
                    break;
            }

            // Pagination logic on the sorted list
            var totalCount = bookingsList.Count();
            var items = bookingsList.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            var pagedBookings = new PaginatedResult<Booking>(items, totalCount, page, pageSize);

            // KPI Calculation (Still needs global stats, likely unrelated to current filters or respecting them? 
            // Original code:
            // 1. Today's Bookings (Global)
            // 2. Pending Payments (Global)
            // 3. Refund Requests (Global)
            // It seems KPIs are Global/Dashboard-like, so independent of filters. 
            // However, "ViewBag.TodayBookingsCount" implies global.
            // Let's keep the ORIGINAL KPI logic which used separate repository calls, unaware of the main list filter.
            
            var today = DateTime.UtcNow.Date;
            
            // 1. Today's Bookings
            var todayBookings = await _bookingRepository.FindAsync(b => b.CreatedAt >= today);
            ViewBag.TodayBookingsCount = todayBookings?.Count() ?? 0;

            // 2. Pending Payments (Unpaid or Pending)
            var pendingPayments = await _bookingRepository.FindAsync(b => b.PaymentStatus == "Unpaid" || b.Status == "Pending");
            ViewBag.PendingPaymentCount = pendingPayments?.Count() ?? 0;
            ViewBag.PendingPaymentTotal = pendingPayments?.Sum(b => b.TotalAmount) ?? 0;

            // 3. Refund Requests (Refunded or Cancelled)
            var refundRequests = await _bookingRepository.FindAsync(b => b.Status == "Cancelled" || b.PaymentStatus == "Refunded");
            ViewBag.RefundRequestCount = refundRequests?.Count() ?? 0;
            
            ViewData["CurrentStatus"] = status;
            ViewData["CurrentSearch"] = searchString;
            ViewData["CurrentStartDate"] = startDate;
            ViewData["CurrentEndDate"] = endDate;
            ViewData["CurrentPageSize"] = pageSize;
            
            return View(pagedBookings);
        }

        [HttpGet("Details/{id}")]
        public async Task<IActionResult> Details(string id)
        {
            var booking = await _bookingRepository.GetByIdAsync(id);
            if (booking == null) return NotFound();
            return View(booking);
        }

        [HttpGet("Edit/{id}")]
        public async Task<IActionResult> Edit(string id)
        {
            var booking = await _bookingRepository.GetByIdAsync(id);
            if (booking == null) return NotFound();
            return View(booking);
        }

        [HttpPost("Edit/{id}")]
        public async Task<IActionResult> Edit(string id, Booking booking)
        {
            if (id != booking.Id) return BadRequest();

            // if (ModelState.IsValid)
            {
                await _bookingRepository.UpdateAsync(id, booking);
                return RedirectToAction(nameof(Index));
            }
            // return View(booking);
        }

        [HttpPost("Delete/{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            await _bookingRepository.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using HVTravel.Domain.Interfaces;
using HVTravel.Domain.Entities;
using HVTravel.Domain.Models;
using HVTravel.Web.Security;

namespace HVTravel.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(AuthenticationSchemes = AuthSchemes.AdminScheme)]
    public class BookingsController : Controller
    {
        private readonly IRepository<Booking> _bookingRepository;
        private static readonly HashSet<string> AllowedBulkStatuses = new(StringComparer.OrdinalIgnoreCase)
        {
            "Confirmed",
            "Completed",
            "Cancelled"
        };

        public BookingsController(IRepository<Booking> bookingRepository)
        {
            _bookingRepository = bookingRepository;
        }

        private static string EnsureBookingCode(Booking booking)
        {
            if (!string.IsNullOrWhiteSpace(booking.BookingCode))
            {
                return booking.BookingCode;
            }

            var suffix = !string.IsNullOrWhiteSpace(booking.Id) && booking.Id.Length >= 6
                ? booking.Id[^6..].ToUpperInvariant()
                : Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();

            booking.BookingCode = $"BK-LEGACY-{suffix}";
            return booking.BookingCode;
        }

        private static void PrepareBookingForPersistence(Booking booking, string id)
        {
            if (string.IsNullOrWhiteSpace(booking.Id))
            {
                booking.Id = id;
            }

            EnsureBookingCode(booking);
            booking.Passengers ??= new List<Passenger>();
            booking.HistoryLog ??= new List<BookingHistoryLog>();
            booking.ContactInfo ??= new ContactInfo();
            booking.Status ??= "Pending";
            booking.PaymentStatus ??= "Unpaid";

            if (booking.BookingDate == default)
            {
                booking.BookingDate = booking.CreatedAt != default ? booking.CreatedAt : DateTime.UtcNow;
            }

            if (booking.CreatedAt == default)
            {
                booking.CreatedAt = booking.BookingDate != default ? booking.BookingDate : DateTime.UtcNow;
            }
        }

        [Authorize(Roles = "Admin,Manager,Staff,Guide")]
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

            ViewBag.CurrentSort = sortOrder;
            ViewBag.DateSortParm = string.IsNullOrEmpty(sortOrder) ? "date_asc" : "";
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
            else if (statusLower == "deleted") targetStatus = "Deleted";

            var search = !string.IsNullOrEmpty(searchString) ? searchString.Trim().ToLower() : null;
            DateTime? start = null; 
            DateTime? end = null;

            if (!string.IsNullOrEmpty(startDate) && DateTime.TryParse(startDate, out var s)) start = s;
            if (!string.IsNullOrEmpty(endDate) && DateTime.TryParse(endDate, out var e)) end = e;
            
            if (end.HasValue) end = end.Value.Date.AddDays(1).AddTicks(-1);

            Expression<Func<Booking, bool>> filter = b =>
                (targetStatus == "Deleted" ? b.IsDeleted : !b.IsDeleted) &&
                (targetStatus == null || targetStatus == "Deleted" || b.Status == targetStatus || b.PaymentStatus == targetStatus) &&
                (search == null || 
                    (b.BookingCode != null && b.BookingCode.ToLower().Contains(search)) || 
                    (b.ContactInfo != null && (
                        (b.ContactInfo.Name != null && b.ContactInfo.Name.ToLower().Contains(search)) || 
                        (b.ContactInfo.Email != null && b.ContactInfo.Email.ToLower().Contains(search))
                    ))
                ) &&
                (start == null || b.CreatedAt >= start) &&
                (end == null || b.CreatedAt <= end);

            var bookingsList = await _bookingRepository.FindAsync(filter);
            
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
                default: 
                    bookingsList = bookingsList.OrderByDescending(b => b.CreatedAt);
                    break;
            }

            var totalCount = bookingsList.Count();
            var items = bookingsList.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            var pagedBookings = new PaginatedResult<Booking>(items, totalCount, page, pageSize);

            var today = DateTime.UtcNow.Date;
            var todayBookings = await _bookingRepository.FindAsync(b => b.CreatedAt >= today);
            ViewBag.TodayBookingsCount = todayBookings?.Count() ?? 0;

            var pendingPayments = await _bookingRepository.FindAsync(b => b.PaymentStatus == "Unpaid" || b.Status == "Pending");
            ViewBag.PendingPaymentCount = pendingPayments?.Count() ?? 0;
            ViewBag.PendingPaymentTotal = pendingPayments?.Sum(b => b.TotalAmount) ?? 0;

            var refundRequests = await _bookingRepository.FindAsync(b => b.Status == "Cancelled" || b.PaymentStatus == "Refunded");
            ViewBag.RefundRequestCount = refundRequests?.Count() ?? 0;
            
            ViewData["CurrentStatus"] = status;
            ViewData["CurrentSearch"] = searchString;
            ViewData["CurrentStartDate"] = startDate;
            ViewData["CurrentEndDate"] = endDate;
            ViewData["CurrentPageSize"] = pageSize;
            
            return View(pagedBookings);
        }

        [Authorize(Roles = "Admin,Manager,Staff,Guide")]
        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            var booking = await _bookingRepository.GetByIdAsync(id);
            if (booking == null) return NotFound();
            return View(booking);
        }

        [Authorize(Roles = "Admin,Manager,Staff")]
        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var booking = await _bookingRepository.GetByIdAsync(id);
            if (booking == null) return NotFound();
            return View(booking);
        }

        [Authorize(Roles = "Admin,Manager,Staff")]
        [HttpPost]
        public async Task<IActionResult> Edit(string id, Booking booking)
        {
            if (id != booking.Id) return BadRequest();

            var existingBooking = await _bookingRepository.GetByIdAsync(id);
            if (existingBooking == null) return NotFound();

            existingBooking.Status = booking.Status;
            existingBooking.PaymentStatus = booking.PaymentStatus;
            existingBooking.ParticipantsCount = booking.ParticipantsCount;
            existingBooking.TotalAmount = booking.TotalAmount;
            existingBooking.Notes = booking.Notes;

            if (booking.ContactInfo != null)
            {
                existingBooking.ContactInfo = booking.ContactInfo;
            }

            PrepareBookingForPersistence(existingBooking, id);
            existingBooking.UpdatedAt = DateTime.UtcNow;

            await _bookingRepository.UpdateAsync(id, existingBooking);
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin,Manager,Staff")]
        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            var booking = await _bookingRepository.GetByIdAsync(id);
            if (booking != null)
            {
                PrepareBookingForPersistence(booking, id);
                booking.IsDeleted = true;
                booking.DeletedBy = User.Identity?.Name ?? "Admin System";
                booking.DeletedAt = DateTime.UtcNow;
                booking.UpdatedAt = DateTime.UtcNow;
                await _bookingRepository.UpdateAsync(id, booking);
            }
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin,Manager,Staff")]
        [HttpPost]
        public async Task<IActionResult> BulkDelete([FromForm] List<string> ids)
        {
            if (ids == null || !ids.Any()) return RedirectToAction(nameof(Index));

            var validIds = ids.Where(id => !string.IsNullOrEmpty(id) && id.Length == 24).ToList();
            if (!validIds.Any()) return RedirectToAction(nameof(Index));

            foreach (var id in validIds)
            {
                var booking = await _bookingRepository.GetByIdAsync(id);
                if (booking != null)
                {
                    PrepareBookingForPersistence(booking, id);
                    booking.IsDeleted = true;
                    booking.DeletedBy = User.Identity?.Name ?? "Admin System";
                    booking.DeletedAt = DateTime.UtcNow;
                    booking.UpdatedAt = DateTime.UtcNow;
                    await _bookingRepository.UpdateAsync(id, booking);
                }
            }
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin,Manager,Staff")]
        [HttpPost]
        public async Task<IActionResult> BulkUpdateStatus([FromForm] List<string> ids, [FromForm] string newStatus)
        {
            if (ids == null || !ids.Any() || string.IsNullOrEmpty(newStatus)) return RedirectToAction(nameof(Index));

            if (!AllowedBulkStatuses.Contains(newStatus))
            {
                return RedirectToAction(nameof(Index));
            }

            var validIds = ids.Where(id => !string.IsNullOrEmpty(id) && id.Length == 24).ToList();
            if (!validIds.Any()) return RedirectToAction(nameof(Index));

            foreach (var id in validIds)
            {
                var booking = await _bookingRepository.GetByIdAsync(id);
                if (booking != null)
                {
                    PrepareBookingForPersistence(booking, id);
                    booking.Status = newStatus;
                    booking.UpdatedAt = DateTime.UtcNow;
                    await _bookingRepository.UpdateAsync(id, booking);
                }
            }
            return RedirectToAction(nameof(Index));
        }
    }
}

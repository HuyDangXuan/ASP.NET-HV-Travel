using System.Security.Claims;
using HVTravel.Domain.Entities;
using HVTravel.Domain.Interfaces;
using HVTravel.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HVTravel.Web.Security;
using HVTravel.Web.Services;

namespace HVTravel.Web.Controllers;

[Authorize(AuthenticationSchemes = AuthSchemes.CustomerScheme, Roles = "Customer")]
public class CustomerPortalController : Controller
{
    private readonly CustomerPortalService _portalService;
    private readonly IRepository<SavedTravellerProfile> _travellerRepository;
    private readonly IRepository<Review> _reviewRepository;
    private readonly IRepository<Booking> _bookingRepository;
    private readonly IRepository<Customer> _customerRepository;

    public CustomerPortalController(
        CustomerPortalService portalService,
        IRepository<SavedTravellerProfile> travellerRepository,
        IRepository<Review> reviewRepository,
        IRepository<Booking> bookingRepository,
        IRepository<Customer> customerRepository)
    {
        _portalService = portalService;
        _travellerRepository = travellerRepository;
        _reviewRepository = reviewRepository;
        _bookingRepository = bookingRepository;
        _customerRepository = customerRepository;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? bookingCode = null)
    {
        ViewData["Title"] = "Cổng khách hàng";
        ViewData["ActivePage"] = "CustomerPortal";

        var dashboard = await _portalService.BuildDashboardAsync(GetCustomerId());
        if (!string.IsNullOrWhiteSpace(bookingCode))
        {
            ViewData["FocusBookingCode"] = bookingCode.Trim();
        }

        return View(dashboard);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveTraveller(SavedTravellerInputViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return RedirectToAction(nameof(Index));
        }

        var customerId = GetCustomerId();
        if (model.IsDefault)
        {
            var existingProfiles = await _travellerRepository.FindAsync(p => p.CustomerId == customerId);
            foreach (var profile in existingProfiles.Where(p => p.IsDefault))
            {
                profile.IsDefault = false;
                profile.UpdatedAt = DateTime.UtcNow;
                await _travellerRepository.UpdateAsync(profile.Id, profile);
            }
        }

        await _travellerRepository.AddAsync(new SavedTravellerProfile
        {
            CustomerId = customerId,
            FullName = model.FullName.Trim(),
            DateOfBirth = model.DateOfBirth,
            Gender = model.Gender?.Trim() ?? string.Empty,
            PassportNumber = model.PassportNumber?.Trim() ?? string.Empty,
            Nationality = model.Nationality?.Trim() ?? string.Empty,
            Phone = model.Phone?.Trim() ?? string.Empty,
            Email = model.Email?.Trim() ?? string.Empty,
            IsDefault = model.IsDefault,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        TempData["PortalSuccess"] = "Đã lưu hồ sơ hành khách.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitReview(ReviewSubmissionViewModel model)
    {
        var customerId = GetCustomerId();
        var booking = await _bookingRepository.GetByIdAsync(model.BookingId);
        if (booking == null || booking.CustomerId != customerId || !string.Equals(booking.Status, "Completed", StringComparison.OrdinalIgnoreCase))
        {
            TempData["PortalError"] = "Không thể gửi đánh giá cho booking này.";
            return RedirectToAction(nameof(Index));
        }

        var existing = (await _reviewRepository.FindAsync(r => r.BookingId == model.BookingId)).FirstOrDefault();
        if (existing != null)
        {
            TempData["PortalError"] = "Booking này đã có đánh giá.";
            return RedirectToAction(nameof(Index));
        }

        var customer = await _customerRepository.GetByIdAsync(customerId);
        await _reviewRepository.AddAsync(new Review
        {
            TourId = model.TourId,
            CustomerId = customerId,
            BookingId = model.BookingId,
            Rating = Math.Clamp(model.Rating, 1, 5),
            Comment = model.Comment?.Trim() ?? string.Empty,
            DisplayName = customer?.FullName ?? "Khách hàng HV Travel",
            IsApproved = false,
            IsVerifiedBooking = true,
            ModerationStatus = "Pending",
            CreatedAt = DateTime.UtcNow
        });

        TempData["PortalSuccess"] = "Đánh giá đã được gửi và đang chờ duyệt.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestCancellation(string bookingId, string reason)
    {
        var customerId = GetCustomerId();
        var booking = await _bookingRepository.GetByIdAsync(bookingId);
        if (booking == null || booking.CustomerId != customerId)
        {
            TempData["PortalError"] = "Không tìm thấy booking để yêu cầu hủy.";
            return RedirectToAction(nameof(Index));
        }

        booking.CancellationRequest = new CancellationRequest
        {
            Status = "Requested",
            Reason = reason?.Trim() ?? string.Empty,
            RequestedAt = DateTime.UtcNow,
            RequestedBy = User.FindFirstValue("FullName") ?? User.Identity?.Name ?? "Khách hàng"
        };
        booking.Events ??= new List<BookingEvent>();
        booking.HistoryLog ??= new List<BookingHistoryLog>();
        booking.Events.Add(new BookingEvent
        {
            Type = "cancellation",
            Title = "Yêu cầu hủy booking",
            Description = booking.CancellationRequest.Reason,
            Actor = booking.CancellationRequest.RequestedBy,
            OccurredAt = DateTime.UtcNow,
            VisibleToCustomer = true
        });
        booking.HistoryLog.Add(new BookingHistoryLog
        {
            Action = "Yêu cầu hủy booking",
            Note = booking.CancellationRequest.Reason,
            User = booking.CancellationRequest.RequestedBy,
            Timestamp = DateTime.UtcNow
        });
        booking.UpdatedAt = DateTime.UtcNow;

        await _bookingRepository.UpdateAsync(booking.Id, booking);
        TempData["PortalSuccess"] = "Yêu cầu hủy đã được ghi nhận.";
        return RedirectToAction(nameof(Index));
    }

    private string GetCustomerId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
    }
}

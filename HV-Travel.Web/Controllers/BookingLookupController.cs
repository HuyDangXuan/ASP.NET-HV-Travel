using HVTravel.Domain.Entities;
using HVTravel.Domain.Interfaces;
using HVTravel.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace HVTravel.Web.Controllers;

public class BookingLookupController : Controller
{
    private readonly IRepository<Booking> _bookingRepository;

    public BookingLookupController(IRepository<Booking> bookingRepository)
    {
        _bookingRepository = bookingRepository;
    }

    [HttpGet]
    public IActionResult Index()
    {
        ViewData["Title"] = "Tra cứu booking";
        ViewData["ActivePage"] = "BookingLookup";
        return View(new BookingLookupViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Lookup(string bookingCode, string email, string phone)
    {
        ViewData["Title"] = "Tra cứu booking";
        ViewData["ActivePage"] = "BookingLookup";

        var model = new BookingLookupViewModel
        {
            QueryBookingCode = bookingCode?.Trim() ?? string.Empty,
            QueryEmail = email?.Trim() ?? string.Empty,
            QueryPhone = phone?.Trim() ?? string.Empty
        };

        if (string.IsNullOrWhiteSpace(model.QueryBookingCode) || (string.IsNullOrWhiteSpace(model.QueryEmail) && string.IsNullOrWhiteSpace(model.QueryPhone)))
        {
            ModelState.AddModelError(string.Empty, "Vui lòng nhập mã booking và email hoặc số điện thoại để tra cứu.");
            return View("Index", model);
        }

        var normalizedPhone = NormalizePhone(model.QueryPhone);
        var booking = (await _bookingRepository.FindAsync(b => b.BookingCode == model.QueryBookingCode))
            .FirstOrDefault(candidate =>
                (!string.IsNullOrWhiteSpace(model.QueryEmail) && string.Equals(candidate.ContactInfo?.Email?.Trim(), model.QueryEmail, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrWhiteSpace(normalizedPhone) && NormalizePhone(candidate.ContactInfo?.Phone) == normalizedPhone));

        if (booking == null)
        {
            ModelState.AddModelError(string.Empty, "Không tìm thấy booking phù hợp với thông tin bạn cung cấp.");
            return View("Index", model);
        }

        model.BookingCode = booking.BookingCode ?? string.Empty;
        model.BookingStatus = booking.Status ?? string.Empty;
        model.PaymentStatus = booking.PaymentStatus ?? string.Empty;
        model.TourName = booking.TourSnapshot?.Name ?? string.Empty;
        model.TourCode = booking.TourSnapshot?.Code ?? string.Empty;
        model.Duration = booking.TourSnapshot?.Duration ?? string.Empty;
        model.ContactName = booking.ContactInfo?.Name ?? string.Empty;
        model.ParticipantsCount = booking.ParticipantsCount;
        model.TotalAmount = booking.TotalAmount;
        model.StartDate = booking.TourSnapshot?.StartDate;
        model.BookingDate = booking.BookingDate;
        model.History = (booking.HistoryLog ?? new List<BookingHistoryLog>())
            .OrderByDescending(item => item.Timestamp)
            .Select(item => $"{item.Timestamp:HH:mm dd/MM/yyyy} · {item.Action}")
            .ToList();

        return View("Index", model);
    }

    private static string NormalizePhone(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : new string(value.Where(char.IsDigit).ToArray());
    }
}

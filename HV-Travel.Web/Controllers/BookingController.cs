using Microsoft.AspNetCore.Mvc;
using HVTravel.Domain.Interfaces;
using HVTravel.Domain.Entities;
using HVTravel.Web.Models;

namespace HVTravel.Web.Controllers;

public class BookingController : Controller
{
    private readonly IRepository<Tour> _tourRepository;
    private readonly IRepository<Booking> _bookingRepository;

    public BookingController(IRepository<Tour> tourRepository, IRepository<Booking> bookingRepository)
    {
        _tourRepository = tourRepository;
        _bookingRepository = bookingRepository;
    }

    // GET: /Booking/Create?tourId=xxx
    public async Task<IActionResult> Create(string tourId)
    {
        if (string.IsNullOrEmpty(tourId))
            return RedirectToAction("Index", "PublicTours");

        var tour = await _tourRepository.GetByIdAsync(tourId);
        if (tour == null)
            return NotFound();

        ViewData["Title"] = $"Đặt Tour - {tour.Name}";
        ViewData["ActivePage"] = "Tours";

        var vm = new BookingViewModel
        {
            TourId = tourId,
            Tour = tour
        };

        return View(vm);
    }

    // POST: /Booking/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BookingViewModel vm)
    {
        var tour = await _tourRepository.GetByIdAsync(vm.TourId);
        if (tour == null) return NotFound();

        vm.Tour = tour;

        if (!ModelState.IsValid)
        {
            ViewData["Title"] = $"Đặt Tour - {tour.Name}";
            return View(vm);
        }

        // Calculate total
        var adultPrice = tour.Price?.Adult ?? 0;
        var childPrice = tour.Price?.Child ?? 0;
        var infantPrice = tour.Price?.Infant ?? 0;
        var discount = tour.Price?.Discount ?? 0;

        var subtotal = (adultPrice * vm.AdultCount) + (childPrice * vm.ChildCount) + (infantPrice * vm.InfantCount);
        var total = discount > 0 ? subtotal * (1 - (decimal)(discount / 100.0)) : subtotal;

        // Create booking
        var booking = new Booking
        {
            BookingCode = $"HV{DateTime.UtcNow:yyyyMMddHHmmss}{new Random().Next(100, 999)}",
            TourId = vm.TourId,
            TourSnapshot = new TourSnapshot
            {
                Code = tour.Code,
                Name = tour.Name,
                StartDate = vm.SelectedStartDate ?? (tour.StartDates?.FirstOrDefault() ?? DateTime.UtcNow),
                Duration = tour.Duration?.Text ?? $"{tour.Duration?.Days} ngày {tour.Duration?.Nights} đêm"
            },
            ContactInfo = new ContactInfo
            {
                Name = vm.ContactName,
                Email = vm.ContactEmail,
                Phone = vm.ContactPhone
            },
            ParticipantsCount = vm.TotalParticipants,
            TotalAmount = total,
            Status = "Pending",
            PaymentStatus = "Unpaid",
            Notes = vm.SpecialRequests,
            BookingDate = DateTime.UtcNow,
            HistoryLog = new List<BookingHistoryLog>
            {
                new BookingHistoryLog
                {
                    Action = "Tạo đơn đặt tour",
                    Timestamp = DateTime.UtcNow,
                    User = vm.ContactName,
                    Note = $"Đặt {vm.AdultCount} người lớn, {vm.ChildCount} trẻ em, {vm.InfantCount} em bé"
                }
            }
        };

        await _bookingRepository.AddAsync(booking);

        return RedirectToAction("Payment", new { bookingId = booking.Id });
    }

    // GET: /Booking/Payment?bookingId=xxx
    public async Task<IActionResult> Payment(string bookingId)
    {
        if (string.IsNullOrEmpty(bookingId))
            return RedirectToAction("Index", "PublicTours");

        var booking = await _bookingRepository.GetByIdAsync(bookingId);
        if (booking == null) return NotFound();

        var tour = await _tourRepository.GetByIdAsync(booking.TourId);

        ViewData["Title"] = "Chọn Phương Thức Thanh Toán";
        ViewData["ActivePage"] = "Tours";

        return View(new BookingResultViewModel { Booking = booking, Tour = tour });
    }

    // POST: /Booking/ProcessPayment
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ProcessPayment(string bookingId, string paymentMethod)
    {
        var booking = await _bookingRepository.GetByIdAsync(bookingId);
        if (booking == null) return NotFound();

        try
        {
            booking.PaymentStatus = paymentMethod == "Cash" ? "Unpaid" : "Pending";
            booking.Status = paymentMethod == "Cash" ? "Confirmed" : "Paid";
            booking.HistoryLog ??= new List<BookingHistoryLog>();
            booking.HistoryLog.Add(new BookingHistoryLog
            {
                Action = $"Chọn thanh toán: {paymentMethod}",
                Timestamp = DateTime.UtcNow,
                User = booking.ContactInfo?.Name ?? "Khách",
                Note = $"Phương thức: {paymentMethod}"
            });
            booking.UpdatedAt = DateTime.UtcNow;

            await _bookingRepository.UpdateAsync(booking.Id, booking);

            return RedirectToAction("Success", new { bookingId = booking.Id });
        }
        catch
        {
            return RedirectToAction("Failed", new { bookingId = booking.Id });
        }
    }

    // GET: /Booking/Success?bookingId=xxx
    public async Task<IActionResult> Success(string bookingId)
    {
        var booking = await _bookingRepository.GetByIdAsync(bookingId);
        if (booking == null) return RedirectToAction("Index", "PublicTours");

        var tour = await _tourRepository.GetByIdAsync(booking.TourId);

        ViewData["Title"] = "Đặt Tour Thành Công";
        return View(new BookingResultViewModel { Booking = booking, Tour = tour });
    }

    // GET: /Booking/Failed?bookingId=xxx
    public async Task<IActionResult> Failed(string bookingId)
    {
        var booking = await _bookingRepository.GetByIdAsync(bookingId);
        var tour = booking != null ? await _tourRepository.GetByIdAsync(booking.TourId) : null;
        ViewData["Title"] = "Thanh Toán Thất Bại";

        return View(new BookingResultViewModel
        {
            Booking = booking ?? new Booking(),
            Tour = tour,
            ErrorMessage = "Thanh toán không thành công. Vui lòng thử lại hoặc chọn phương thức khác."
        });
    }

    // GET: /Booking/Error
    public IActionResult Error()
    {
        ViewData["Title"] = "Lỗi Hệ Thống";
        return View();
    }

    // GET: /Booking/Consultation
    public IActionResult Consultation()
    {
        ViewData["Title"] = "Tư Vấn Du Lịch";
        ViewData["ActivePage"] = "Consultation";
        return View(new ConsultationViewModel());
    }

    // POST: /Booking/Consultation
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Consultation(ConsultationViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            ViewData["Title"] = "Tư Vấn Du Lịch";
            return View(vm);
        }

        // TODO: Save to DB or send email notification
        TempData["ConsultationSuccess"] = true;
        return RedirectToAction("Consultation");
    }
}

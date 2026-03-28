using Microsoft.AspNetCore.Mvc;
using HVTravel.Domain.Interfaces;
using HVTravel.Domain.Entities;
using HVTravel.Web.Models;
using HVTravel.Web.Services;

namespace HVTravel.Web.Controllers;

public class BookingController : Controller
{
    private readonly ITourRepository _tourRepository;
    private readonly IRepository<Booking> _bookingRepository;
    private readonly BookingWorkflowService _bookingWorkflowService;

    public BookingController(ITourRepository tourRepository, IRepository<Booking> bookingRepository, BookingWorkflowService bookingWorkflowService)
    {
        _tourRepository = tourRepository;
        _bookingRepository = bookingRepository;
        _bookingWorkflowService = bookingWorkflowService;
    }

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

        if (vm.TotalParticipants > tour.RemainingSpots)
        {
            ModelState.AddModelError("", $"Tour này hiện chỉ còn {tour.RemainingSpots} chỗ trống. Vui lòng giảm số lượng hành khách.");
            ViewData["Title"] = $"Đặt Tour - {tour.Name}";
            return View(vm);
        }

        var adultPrice = tour.Price?.Adult ?? 0;
        var childPrice = tour.Price?.Child ?? 0;
        var infantPrice = tour.Price?.Infant ?? 0;
        var discount = tour.Price?.Discount ?? 0;

        var subtotal = (adultPrice * vm.AdultCount) + (childPrice * vm.ChildCount) + (infantPrice * vm.InfantCount);
        var total = discount > 0 ? subtotal * (1 - (decimal)(discount / 100.0)) : subtotal;

        var passengers = new List<Passenger>();
        for (int i = 0; i < vm.AdultCount; i++) passengers.Add(new Passenger { Type = "Adult", FullName = i == 0 ? vm.ContactName : $"Người lớn {i + 1}" });
        for (int i = 0; i < vm.ChildCount; i++) passengers.Add(new Passenger { Type = "Child", FullName = $"Trẻ em {i + 1}" });
        for (int i = 0; i < vm.InfantCount; i++) passengers.Add(new Passenger { Type = "Infant", FullName = $"Em bé {i + 1}" });

        var booking = new Booking
        {
            BookingCode = $"HV{DateTime.UtcNow:yyyyMMddHHmmss}{Random.Shared.Next(100, 999)}",
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
            Passengers = passengers,
            TotalAmount = total,
            Status = "Pending",
            PaymentStatus = "Unpaid",
            Notes = vm.SpecialRequests,
            BookingDate = DateTime.UtcNow,
            PublicLookupEnabled = true,
            HistoryLog = new List<BookingHistoryLog>(),
            Events = new List<BookingEvent>()
        };

        AddBookingEntry(booking, "booking", "Tạo đơn đặt tour", $"Đặt {vm.AdultCount} người lớn, {vm.ChildCount} trẻ em, {vm.InfantCount} em bé", vm.ContactName);

        var success = await _tourRepository.IncrementParticipantsAsync(vm.TourId, vm.TotalParticipants);
        if (!success)
        {
            ModelState.AddModelError("", "Rất tiếc, tour đã vừa hết chỗ trống khi bạn đang thực hiện đặt. Vui lòng thử lại với số lượng ít hơn hoặc chọn tour khác.");
            ViewData["Title"] = $"Đặt Tour - {tour.Name}";
            return View(vm);
        }

        await _bookingRepository.AddAsync(booking);
        return RedirectToAction("Payment", new { bookingId = booking.Id });
    }

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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ProcessPayment(string bookingId, string paymentMethod)
    {
        var booking = await _bookingRepository.GetByIdAsync(bookingId);
        if (booking == null) return NotFound();

        var actor = booking.ContactInfo?.Name ?? "Khách";
        booking.PaymentTransactions ??= new List<PaymentTransaction>();
        booking.Events ??= new List<BookingEvent>();
        booking.HistoryLog ??= new List<BookingHistoryLog>();

        switch (paymentMethod)
        {
            case "Cash":
                booking.PaymentStatus = "Unpaid";
                booking.Status = "Confirmed";
                booking.ConfirmedAt ??= DateTime.UtcNow;
                AddBookingEntry(booking, "payment", "Giữ chỗ thanh toán tiền mặt", "Booking đã được giữ chỗ, thanh toán tại quầy hoặc khi khởi hành.", actor);
                await _bookingRepository.UpdateAsync(booking.Id, booking);
                break;
            case "BankTransfer":
                booking.PaymentStatus = "Pending";
                booking.Status = "PendingPayment";
                booking.PaymentTransactions.Add(new PaymentTransaction
                {
                    Provider = "ManualTransfer",
                    Method = "BankTransfer",
                    TransactionId = $"BANK-{booking.BookingCode}-{DateTime.UtcNow:HHmmss}",
                    Reference = booking.BookingCode,
                    Amount = booking.TotalAmount,
                    Status = "Pending",
                    CreatedAt = DateTime.UtcNow,
                    ProcessedAt = DateTime.UtcNow
                });
                AddBookingEntry(booking, "payment", "Chờ xác nhận chuyển khoản", "Khách đã chọn chuyển khoản và chờ tải minh chứng thanh toán.", actor);
                await _bookingRepository.UpdateAsync(booking.Id, booking);
                break;
            default:
                booking.PaymentStatus = "Pending";
                booking.Status = "PendingPayment";
                AddBookingEntry(booking, "payment", "Khởi tạo thanh toán online", "Hệ thống đã tạo phiên thanh toán online và đang chờ callback.", actor);
                await _bookingRepository.UpdateAsync(booking.Id, booking);

                await _bookingWorkflowService.ProcessGatewayCallbackAsync(new PaymentGatewayWebhookModel
                {
                    BookingCode = booking.BookingCode,
                    TransactionId = $"TXN-{booking.BookingCode}-{DateTime.UtcNow:HHmmss}",
                    Provider = "HVPay",
                    Method = "CreditCard",
                    Status = "SUCCESS",
                    Amount = booking.TotalAmount,
                    Reference = $"HVPAY-{booking.BookingCode}",
                    Signature = "local-demo"
                });
                break;
        }

        return RedirectToAction("Success", new { bookingId = booking.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadTransferProof(string bookingId, IFormFile? proofFile, string? note)
    {
        var booking = await _bookingRepository.GetByIdAsync(bookingId);
        if (booking == null) return NotFound();

        if (proofFile == null || proofFile.Length == 0)
        {
            TempData["PaymentError"] = "Vui lòng chọn file minh chứng chuyển khoản.";
            return RedirectToAction(nameof(Payment), new { bookingId });
        }

        await using var memoryStream = new MemoryStream();
        await proofFile.CopyToAsync(memoryStream);

        booking.TransferProofFileName = proofFile.FileName;
        booking.TransferProofContentType = proofFile.ContentType;
        booking.TransferProofBase64 = Convert.ToBase64String(memoryStream.ToArray());
        booking.PaymentStatus = "Pending";
        booking.Status = "PendingPayment";
        booking.UpdatedAt = DateTime.UtcNow;
        AddBookingEntry(booking, "payment", "Đã tải minh chứng chuyển khoản", note ?? proofFile.FileName, booking.ContactInfo?.Name ?? "Khách");
        await _bookingRepository.UpdateAsync(booking.Id, booking);

        TempData["PaymentSuccess"] = "Đã tải minh chứng chuyển khoản. Bộ phận vận hành sẽ đối soát và cập nhật trạng thái.";
        return RedirectToAction(nameof(Payment), new { bookingId });
    }

    [HttpPost]
    public async Task<IActionResult> Webhook([FromForm] PaymentGatewayWebhookModel model)
    {
        var handled = await _bookingWorkflowService.ProcessGatewayCallbackAsync(model);
        if (!handled)
        {
            return NotFound();
        }

        return Ok(new { success = true, bookingCode = model.BookingCode, transactionId = model.TransactionId });
    }

    public IActionResult RetryPayment(string bookingId)
    {
        return RedirectToAction(nameof(Payment), new { bookingId });
    }

    public async Task<IActionResult> Success(string bookingId)
    {
        var booking = await _bookingRepository.GetByIdAsync(bookingId);
        if (booking == null) return RedirectToAction("Index", "PublicTours");

        var tour = await _tourRepository.GetByIdAsync(booking.TourId);
        ViewData["Title"] = "Đặt Tour Thành Công";
        return View(new BookingResultViewModel { Booking = booking, Tour = tour });
    }

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

    public IActionResult Error()
    {
        ViewData["Title"] = "Lỗi Hệ Thống";
        return View();
    }

    public IActionResult Consultation()
    {
        ViewData["Title"] = "Tư Vấn Du Lịch";
        ViewData["ActivePage"] = "Consultation";
        return View(new ConsultationViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Consultation(ConsultationViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            ViewData["Title"] = "Tư Vấn Du Lịch";
            return View(vm);
        }

        TempData["ConsultationSuccess"] = true;
        return RedirectToAction("Consultation");
    }

    private static void AddBookingEntry(Booking booking, string type, string title, string note, string actor)
    {
        booking.Events ??= new List<BookingEvent>();
        booking.HistoryLog ??= new List<BookingHistoryLog>();
        booking.Events.Add(new BookingEvent
        {
            Type = type,
            Title = title,
            Description = note,
            Actor = actor,
            OccurredAt = DateTime.UtcNow,
            VisibleToCustomer = true
        });
        booking.HistoryLog.Add(new BookingHistoryLog
        {
            Action = title,
            Note = note,
            User = actor,
            Timestamp = DateTime.UtcNow
        });
        booking.UpdatedAt = DateTime.UtcNow;
    }
}

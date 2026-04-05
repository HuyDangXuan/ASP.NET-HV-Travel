using HVTravel.Application.Interfaces;
using HVTravel.Application.Models;
using HVTravel.Domain.Entities;
using HVTravel.Domain.Interfaces;
using HVTravel.Web.Models;
using HVTravel.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace HVTravel.Web.Controllers;

public class BookingController : Controller
{
    private readonly ITourRepository _tourRepository;
    private readonly IRepository<Booking> _bookingRepository;
    private readonly BookingWorkflowService _bookingWorkflowService;
    private readonly ICheckoutService _checkoutService;
    private readonly IPricingService _pricingService;
    private readonly IRepository<CheckoutSession> _checkoutSessionRepository;

    public BookingController(
        ITourRepository tourRepository,
        IRepository<Booking> bookingRepository,
        BookingWorkflowService bookingWorkflowService,
        ICheckoutService checkoutService,
        IPricingService pricingService,
        IRepository<CheckoutSession> checkoutSessionRepository)
    {
        _tourRepository = tourRepository;
        _bookingRepository = bookingRepository;
        _bookingWorkflowService = bookingWorkflowService;
        _checkoutService = checkoutService;
        _pricingService = pricingService;
        _checkoutSessionRepository = checkoutSessionRepository;
    }

    public IRepository<Booking> Bookings => _bookingRepository;

    public async Task<IActionResult> Create(string tourId)
    {
        if (string.IsNullOrEmpty(tourId))
        {
            return RedirectToAction("Index", "PublicTours");
        }

        var tour = await _tourRepository.GetByIdAsync(tourId);
        if (tour == null)
        {
            return NotFound();
        }

        tour = PublicTextSanitizer.NormalizeTourForDisplay(tour);

        ViewData["Title"] = $"Đặt Tour - {tour.Name}";
        ViewData["ActivePage"] = "Tours";

        var suggestedDeparture = tour.ResolveDeparture(null);
        var vm = new BookingViewModel
        {
            TourId = tourId,
            Tour = tour,
            DepartureId = suggestedDeparture?.Id ?? string.Empty,
            SelectedStartDate = suggestedDeparture?.StartDate
        };

        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Quote(
        string tourId,
        string? departureId,
        int adultCount = 1,
        int childCount = 0,
        int infantCount = 0,
        string? couponCode = null,
        string? paymentPlanType = "Full")
    {
        if (string.IsNullOrWhiteSpace(tourId))
        {
            return Json(new QuotePreviewResponse
            {
                IsAvailable = false,
                ErrorMessage = "Không tìm thấy tour để xem báo giá."
            });
        }

        var tour = await _tourRepository.GetByIdAsync(tourId);
        if (tour == null)
        {
            return Json(new QuotePreviewResponse
            {
                IsAvailable = false,
                ErrorMessage = "Không tìm thấy tour để xem báo giá."
            });
        }

        var response = await BuildQuotePreviewAsync(
            tour,
            departureId,
            null,
            adultCount,
            childCount,
            infantCount,
            couponCode,
            paymentPlanType);

        return Json(response);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BookingViewModel vm)
    {
        var tour = await _tourRepository.GetByIdAsync(vm.TourId);
        if (tour == null)
        {
            return NotFound();
        }

        tour = PublicTextSanitizer.NormalizeTourForDisplay(tour);
        vm.Tour = tour;
        if (vm.SelectedStartDate == null)
        {
            vm.SelectedStartDate = tour.ResolveDeparture(vm.DepartureId)?.StartDate;
        }

        tour = PublicTextSanitizer.NormalizeTourForDisplay(tour);

        ViewData["Title"] = $"Đặt Tour - {tour.Name}";
        ViewData["ActivePage"] = "Tours";

        if (!ModelState.IsValid)
        {
            return View(vm);
        }

        var preview = await BuildQuotePreviewAsync(
            tour,
            vm.DepartureId,
            vm.SelectedStartDate,
            vm.AdultCount,
            vm.ChildCount,
            vm.InfantCount,
            vm.CouponCode,
            vm.PaymentPlanType);

        if (!preview.IsAvailable)
        {
            ModelState.AddModelError(string.Empty, preview.ErrorMessage);
            return View(vm);
        }

        if (!string.IsNullOrWhiteSpace(vm.CouponCode) && string.IsNullOrWhiteSpace(preview.AppliedCouponCode))
        {
            ModelState.AddModelError(nameof(vm.CouponCode), preview.ErrorMessage);
            return View(vm);
        }

        try
        {
            var checkout = await _checkoutService.CreateCheckoutAsync(new CreateCheckoutRequest
            {
                TourId = vm.TourId,
                DepartureId = vm.DepartureId,
                SelectedStartDate = vm.SelectedStartDate,
                ContactName = vm.ContactName,
                ContactEmail = vm.ContactEmail,
                ContactPhone = vm.ContactPhone,
                AdultCount = vm.AdultCount,
                ChildCount = vm.ChildCount,
                InfantCount = vm.InfantCount,
                CouponCode = vm.CouponCode,
                PaymentPlanType = vm.PaymentPlanType,
                SpecialRequests = vm.SpecialRequests
            });

            return RedirectToAction("Payment", new { bookingId = checkout.Booking.Id });
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(vm);
        }
    }

    public async Task<IActionResult> Payment(string bookingId)
    {
        if (string.IsNullOrEmpty(bookingId))
        {
            return RedirectToAction("Index", "PublicTours");
        }

        var booking = await _bookingRepository.GetByIdAsync(bookingId);
        if (booking == null)
        {
            return NotFound();
        }

        var tour = await _tourRepository.GetByIdAsync(booking.TourId);
        booking = PublicTextSanitizer.NormalizeBookingForDisplay(booking);
        if (tour != null)
        {
            tour = PublicTextSanitizer.NormalizeTourForDisplay(tour);
        }
        ViewData["Title"] = "Chọn Phương Thức Thanh Toán";
        ViewData["ActivePage"] = "Tours";

        return View(new BookingResultViewModel { Booking = booking, Tour = tour });
    }

    public async Task<IActionResult> Resume(string checkoutSessionId)
    {
        if (string.IsNullOrWhiteSpace(checkoutSessionId))
        {
            return RedirectToAction("Index", "PublicTours");
        }

        var session = await _checkoutSessionRepository.GetByIdAsync(checkoutSessionId);
        if (session == null || string.IsNullOrWhiteSpace(session.BookingId))
        {
            return RedirectToAction("Index", "PublicTours");
        }

        return RedirectToAction(nameof(Payment), new { bookingId = session.BookingId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ProcessPayment(string bookingId, string paymentMethod)
    {
        var booking = await _bookingRepository.GetByIdAsync(bookingId);
        if (booking == null)
        {
            return NotFound();
        }

        var actor = booking.ContactInfo?.Name ?? "Khách";
        booking.PaymentTransactions ??= new List<PaymentTransaction>();
        booking.Events ??= new List<BookingEvent>();
        booking.HistoryLog ??= new List<BookingHistoryLog>();
        booking.PaymentSessions ??= new List<PaymentSession>();

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
                    Amount = booking.PaymentPlan?.AmountDueNow > 0m ? booking.PaymentPlan.AmountDueNow : booking.TotalAmount,
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
                var pendingSession = booking.PaymentSessions.FirstOrDefault(session => string.Equals(session.Status, "Pending", StringComparison.OrdinalIgnoreCase));
                if (pendingSession != null)
                {
                    pendingSession.Reference = $"HVPAY-{booking.BookingCode}";
                    pendingSession.Provider = "HVPay";
                    pendingSession.UpdatedAt = DateTime.UtcNow;
                }

                AddBookingEntry(booking, "payment", "Khởi tạo thanh toán online", "Hệ thống đã tạo phiên thanh toán online và đang chờ callback.", actor);
                await _bookingRepository.UpdateAsync(booking.Id, booking);

                await _bookingWorkflowService.ProcessGatewayCallbackAsync(new PaymentGatewayWebhookModel
                {
                    BookingCode = booking.BookingCode,
                    TransactionId = $"TXN-{booking.BookingCode}-{DateTime.UtcNow:HHmmss}",
                    Provider = "HVPay",
                    Method = "CreditCard",
                    Status = "SUCCESS",
                    Amount = booking.PaymentPlan?.AmountDueNow > 0m ? booking.PaymentPlan.AmountDueNow : booking.TotalAmount,
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
        if (booking == null)
        {
            return NotFound();
        }

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

    public IActionResult RetryPayment(string bookingId, string? checkoutSessionId = null)
    {
        if (!string.IsNullOrWhiteSpace(checkoutSessionId))
        {
            return RedirectToAction(nameof(Resume), new { checkoutSessionId });
        }

        return RedirectToAction(nameof(Payment), new { bookingId });
    }

    public async Task<IActionResult> Success(string bookingId)
    {
        var booking = await _bookingRepository.GetByIdAsync(bookingId);
        if (booking == null)
        {
            return RedirectToAction("Index", "PublicTours");
        }

        var tour = await _tourRepository.GetByIdAsync(booking.TourId);
        booking = PublicTextSanitizer.NormalizeBookingForDisplay(booking);
        if (tour != null)
        {
            tour = PublicTextSanitizer.NormalizeTourForDisplay(tour);
        }
        ViewData["Title"] = "Đặt Tour Thành Công";
        return View(new BookingResultViewModel { Booking = booking, Tour = tour });
    }

    public async Task<IActionResult> Failed(string bookingId)
    {
        var booking = await _bookingRepository.GetByIdAsync(bookingId);
        var tour = booking != null ? await _tourRepository.GetByIdAsync(booking.TourId) : null;
        if (booking != null)
        {
            booking = PublicTextSanitizer.NormalizeBookingForDisplay(booking);
        }
        if (tour != null)
        {
            tour = PublicTextSanitizer.NormalizeTourForDisplay(tour);
        }
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

    private async Task<QuotePreviewResponse> BuildQuotePreviewAsync(
        Tour tour,
        string? departureId,
        DateTime? selectedStartDate,
        int adultCount,
        int childCount,
        int infantCount,
        string? couponCode,
        string? paymentPlanType)
    {
        adultCount = Math.Max(1, adultCount);
        childCount = Math.Max(0, childCount);
        infantCount = Math.Max(0, infantCount);
        var travellerCount = adultCount + childCount + infantCount;

        try
        {
            var quote = await _pricingService.BuildQuoteAsync(new PricingQuoteRequest
            {
                Tour = tour,
                DepartureId = departureId,
                SelectedStartDate = selectedStartDate,
                AdultCount = adultCount,
                ChildCount = childCount,
                InfantCount = infantCount,
                CouponCode = couponCode,
                PaymentPlanType = paymentPlanType ?? "Full"
            });

            var remainingCapacity = quote.SelectedDeparture?.RemainingCapacity ?? 0;
            var isAvailable = travellerCount > 0 && remainingCapacity >= travellerCount;
            var errorMessage = string.Empty;

            if (!isAvailable)
            {
                errorMessage = "Đợt khởi hành đã chọn không còn đủ chỗ cho số lượng khách này.";
            }
            else if (!string.IsNullOrWhiteSpace(couponCode) && string.IsNullOrWhiteSpace(quote.AppliedCouponCode))
            {
                errorMessage = "Mã giảm giá không hợp lệ, đã hết hạn hoặc chưa đủ điều kiện áp dụng.";
            }

            return new QuotePreviewResponse
            {
                Subtotal = quote.Breakdown.Subtotal,
                DiscountTotal = quote.Breakdown.DiscountTotal,
                GrandTotal = quote.Breakdown.GrandTotal,
                AmountDueNow = quote.PaymentPlan.AmountDueNow,
                BalanceDue = quote.PaymentPlan.BalanceDue,
                AppliedCouponCode = quote.AppliedCouponCode,
                RemainingCapacity = remainingCapacity,
                Badges = quote.Badges,
                IsAvailable = isAvailable,
                ErrorMessage = errorMessage
            };
        }
        catch (InvalidOperationException ex)
        {
            var departure = tour.ResolveDeparture(departureId, selectedStartDate);
            return new QuotePreviewResponse
            {
                RemainingCapacity = departure?.RemainingCapacity ?? 0,
                Badges = BuildBadges(tour, departure, false),
                IsAvailable = false,
                ErrorMessage = ex.Message
            };
        }
    }

    private static IReadOnlyList<string> BuildBadges(Tour tour, TourDeparture? departure, bool hasDeal)
    {
        var badges = new List<string>();

        var confirmation = departure?.ConfirmationType;
        if (string.IsNullOrWhiteSpace(confirmation))
        {
            confirmation = tour.ConfirmationType;
        }

        if (!string.IsNullOrWhiteSpace(confirmation))
        {
            badges.Add(confirmation);
        }

        if (tour.CancellationPolicy?.IsFreeCancellation == true)
        {
            badges.Add("FreeCancellation");
        }

        if (departure?.RemainingCapacity is > 0 and <= 5)
        {
            badges.Add("LowAvailability");
        }

        if (hasDeal)
        {
            badges.Add("Deal");
        }

        return badges.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
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


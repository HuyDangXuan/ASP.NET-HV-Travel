using HVTravel.Application.Interfaces;
using HVTravel.Application.Models;
using HVTravel.Domain.Entities;
using HVTravel.Domain.Interfaces;

namespace HVTravel.Application.Services;

public class CheckoutService : ICheckoutService
{
    private readonly ITourRepository _tourRepository;
    private readonly IRepository<Booking> _bookingRepository;
    private readonly IRepository<CheckoutSession> _checkoutSessionRepository;
    private readonly IPricingService _pricingService;
    private readonly IInventoryService _inventoryService;
    private readonly IAnalyticsTracker _analyticsTracker;
    private readonly ISearchIndexingService? _searchIndexingService;

    public CheckoutService(
        ITourRepository tourRepository,
        IRepository<Booking> bookingRepository,
        IRepository<CheckoutSession> checkoutSessionRepository,
        IPricingService pricingService,
        IInventoryService inventoryService,
        IAnalyticsTracker analyticsTracker,
        ISearchIndexingService? searchIndexingService = null)
    {
        _tourRepository = tourRepository;
        _bookingRepository = bookingRepository;
        _checkoutSessionRepository = checkoutSessionRepository;
        _pricingService = pricingService;
        _inventoryService = inventoryService;
        _analyticsTracker = analyticsTracker;
        _searchIndexingService = searchIndexingService;
    }

    public async Task<CreateCheckoutResult> CreateCheckoutAsync(CreateCheckoutRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var tour = await _tourRepository.GetByIdAsync(request.TourId)
            ?? throw new InvalidOperationException("Tour could not be found for checkout creation.");

        var quote = await _pricingService.BuildQuoteAsync(new PricingQuoteRequest
        {
            Tour = tour,
            DepartureId = request.DepartureId,
            SelectedStartDate = request.SelectedStartDate,
            AdultCount = request.AdultCount,
            ChildCount = request.ChildCount,
            InfantCount = request.InfantCount,
            CouponCode = request.CouponCode,
            CustomerSegment = request.CustomerSegment,
            PaymentPlanType = request.PaymentPlanType
        });

        var selectedDeparture = quote.SelectedDeparture
            ?? throw new InvalidOperationException("A departure is required before checkout can be created.");

        var travellerCount = Math.Max(1, request.AdultCount + request.ChildCount + request.InfantCount);
        var reserved = await _inventoryService.ReserveDepartureAsync(tour.Id, selectedDeparture.Id, travellerCount);
        if (!reserved)
        {
            throw new InvalidOperationException("The selected departure no longer has enough seats available.");
        }

        var now = DateTime.UtcNow;
        var booking = new Booking
        {
            BookingCode = BuildBookingCode(),
            TourId = tour.Id,
            DepartureId = selectedDeparture.Id,
            CustomerId = request.CustomerId ?? null!,
            TourSnapshot = new TourSnapshot
            {
                Code = tour.Code,
                Name = tour.Name,
                StartDate = selectedDeparture.StartDate,
                Duration = tour.Duration?.Text ?? $"{tour.Duration?.Days} ng\u00E0y {tour.Duration?.Nights} \u0111\u00EAm"
            },
            ContactInfo = new ContactInfo
            {
                Name = request.ContactName,
                Email = request.ContactEmail,
                Phone = request.ContactPhone
            },
            ParticipantsCount = travellerCount,
            Passengers = BuildPassengers(request),
            TotalAmount = quote.Breakdown.GrandTotal,
            PricingBreakdown = quote.Breakdown,
            CouponCode = quote.AppliedCouponCode,
            VoucherRedemptions = quote.AppliedRedemptions.ToList(),
            PaymentPlan = quote.PaymentPlan,
            PaymentSessions =
            [
                new PaymentSession
                {
                    Id = Guid.NewGuid().ToString("N"),
                    Provider = "Checkout",
                    Status = "Pending",
                    IdempotencyKey = $"chk_{Guid.NewGuid():N}",
                    Reference = $"PAY-{DateTime.UtcNow:yyyyMMddHHmmssfff}",
                    Amount = quote.PaymentPlan.AmountDueNow > 0m ? quote.PaymentPlan.AmountDueNow : quote.Breakdown.GrandTotal,
                    CreatedAt = now,
                    UpdatedAt = now
                }
            ],
            FulfillmentStatus = string.Equals(selectedDeparture.ConfirmationType, "Instant", StringComparison.OrdinalIgnoreCase) ? "Reserved" : "PendingSupplier",
            FulfillmentItems =
            [
                new FulfillmentItem
                {
                    ItemType = "TourDeparture",
                    Status = "Reserved",
                    SupplierId = tour.SupplierRef?.SupplierId ?? string.Empty,
                    Reference = selectedDeparture.Id
                }
            ],
            BookingDate = now,
            Status = "PendingPayment",
            PaymentStatus = "Pending",
            Notes = request.SpecialRequests ?? string.Empty,
            PublicLookupEnabled = true,
            HistoryLog = new List<BookingHistoryLog>(),
            Events = new List<BookingEvent>(),
            CreatedAt = now,
            UpdatedAt = now
        };

        AddTimelineEntry(booking, "checkout", "Kh\u1EDFi t\u1EA1o checkout", $"Checkout \u0111\u01B0\u1EE3c t\u1EA1o cho \u0111\u1EE3t kh\u1EDFi h\u00E0nh {selectedDeparture.StartDate:dd/MM/yyyy}.", request.ContactName, now);
        AddTimelineEntry(booking, "inventory", "Gi\u1EEF ch\u1ED7 t\u1EA1m th\u1EDDi", $"\u0110\u00E3 gi\u1EEF {travellerCount} ch\u1ED7 tr\u00EAn departure {selectedDeparture.Id}.", "System", now);

        await _bookingRepository.AddAsync(booking);
        await (_searchIndexingService?.UpsertBookingAsync(booking) ?? Task.CompletedTask);

        var session = new CheckoutSession
        {
            BookingId = booking.Id,
            TourId = tour.Id,
            DepartureId = selectedDeparture.Id,
            ContactEmail = request.ContactEmail,
            CouponCode = quote.AppliedCouponCode,
            PaymentPlanType = quote.PaymentPlan.PlanType,
            Status = "Open",
            ExpiresAt = now.AddMinutes(30),
            CreatedAt = now,
            UpdatedAt = now
        };

        await _checkoutSessionRepository.AddAsync(session);

        booking.CheckoutSessionId = session.Id;
        await _bookingRepository.UpdateAsync(booking.Id, booking);
        await (_searchIndexingService?.UpsertBookingAsync(booking) ?? Task.CompletedTask);

        await _analyticsTracker.TrackAsync("checkout_created", new Dictionary<string, string?>
        {
            ["tourId"] = tour.Id,
            ["departureId"] = selectedDeparture.Id,
            ["bookingCode"] = booking.BookingCode,
            ["paymentPlanType"] = quote.PaymentPlan.PlanType,
            ["couponCode"] = quote.AppliedCouponCode,
            ["travellers"] = travellerCount.ToString()
        });

        return new CreateCheckoutResult
        {
            Booking = booking,
            Session = session,
            Quote = quote
        };
    }

    private static string BuildBookingCode()
    {
        return $"HV{DateTime.UtcNow:yyyyMMddHHmmss}{Random.Shared.Next(100, 999)}";
    }

    private static List<Passenger> BuildPassengers(CreateCheckoutRequest request)
    {
        var passengers = new List<Passenger>();

        for (var index = 0; index < request.AdultCount; index++)
        {
            passengers.Add(new Passenger
            {
                Type = "Adult",
                FullName = index == 0 ? request.ContactName : $"Ng\u01B0\u1EDDi l\u1EDBn {index + 1}"
            });
        }

        for (var index = 0; index < request.ChildCount; index++)
        {
            passengers.Add(new Passenger
            {
                Type = "Child",
                FullName = $"Tr\u1EBB em {index + 1}"
            });
        }

        for (var index = 0; index < request.InfantCount; index++)
        {
            passengers.Add(new Passenger
            {
                Type = "Infant",
                FullName = $"Em b\u00E9 {index + 1}"
            });
        }

        return passengers;
    }

    private static void AddTimelineEntry(Booking booking, string type, string title, string description, string actor, DateTime occurredAt)
    {
        booking.Events.Add(new BookingEvent
        {
            Type = type,
            Title = title,
            Description = description,
            Actor = actor,
            OccurredAt = occurredAt,
            VisibleToCustomer = true
        });

        booking.HistoryLog.Add(new BookingHistoryLog
        {
            Action = title,
            Note = description,
            User = actor,
            Timestamp = occurredAt
        });
    }
}

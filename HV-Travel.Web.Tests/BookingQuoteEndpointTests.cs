using System.Reflection;
using HVTravel.Application.Services;
using HVTravel.Domain.Entities;
using HVTravel.Web.Controllers;
using HVTravel.Web.Models;
using HVTravel.Web.Services;
using HV_Travel.Web.Tests.TestSupport;
using Microsoft.AspNetCore.Mvc;

namespace HV_Travel.Web.Tests;

public class BookingQuoteEndpointTests
{
    [Fact]
    public async Task Quote_ReturnsPricingBreakdownAndDoesNotMutateInventory()
    {
        var nextMonth = DateTime.UtcNow.AddMonths(1);
        var tour = BuildTour(nextMonth, capacity: 10, bookedCount: 4);
        var controller = CreateController([tour], [BuildPromotion("WELCOME10", 10d, 8000000m)]);
        var beforeBooked = tour.Departures.Single().BookedCount;

        var result = await controller.Quote(tour.Id, "dep-1", 2, 0, 0, "WELCOME10", "Deposit");

        var json = Assert.IsType<JsonResult>(result);
        var payload = Assert.IsType<QuotePreviewResponse>(json.Value);
        Assert.True(payload.IsAvailable);
        Assert.Equal(9000000m, payload.Subtotal);
        Assert.Equal(900000m, payload.DiscountTotal);
        Assert.Equal(8100000m, payload.GrandTotal);
        Assert.Equal(2430000m, payload.AmountDueNow);
        Assert.Equal(5670000m, payload.BalanceDue);
        Assert.Equal("WELCOME10", payload.AppliedCouponCode);
        Assert.Contains("Instant", payload.Badges);
        Assert.Equal(beforeBooked, tour.Departures.Single().BookedCount);
    }

    [Fact]
    public async Task Quote_ReturnsCouponErrorWithoutMutatingInventory()
    {
        var nextMonth = DateTime.UtcNow.AddMonths(1);
        var tour = BuildTour(nextMonth, capacity: 10, bookedCount: 2);
        var controller = CreateController([tour]);
        var beforeBooked = tour.Departures.Single().BookedCount;

        var result = await controller.Quote(tour.Id, "dep-1", 2, 0, 0, "BADCODE", "Full");

        var json = Assert.IsType<JsonResult>(result);
        var payload = Assert.IsType<QuotePreviewResponse>(json.Value);
        Assert.True(payload.IsAvailable);
        Assert.Equal(string.Empty, payload.AppliedCouponCode);
        Assert.False(string.IsNullOrWhiteSpace(payload.ErrorMessage));
        Assert.Equal(beforeBooked, tour.Departures.Single().BookedCount);
    }

    [Fact]
    public async Task Quote_ReturnsUnavailableWhenDepartureDoesNotHaveEnoughSeats()
    {
        var nextMonth = DateTime.UtcNow.AddMonths(1);
        var tour = BuildTour(nextMonth, capacity: 5, bookedCount: 5);
        var controller = CreateController([tour]);

        var result = await controller.Quote(tour.Id, "dep-1", 1, 0, 0, null, "Full");

        var json = Assert.IsType<JsonResult>(result);
        var payload = Assert.IsType<QuotePreviewResponse>(json.Value);
        Assert.False(payload.IsAvailable);
        Assert.Equal(0, payload.RemainingCapacity);
        Assert.False(string.IsNullOrWhiteSpace(payload.ErrorMessage));
    }

    [Fact]
    public async Task Create_WhenDepartureBecomesUnavailable_ReturnsFormWithSameState()
    {
        var nextMonth = DateTime.UtcNow.AddMonths(1);
        var tour = BuildTour(nextMonth, capacity: 2, bookedCount: 2);
        var controller = CreateController([tour], [BuildPromotion("SAVE5", 5d, 1000000m)]);

        var result = await controller.Create(new BookingViewModel
        {
            TourId = tour.Id,
            DepartureId = "dep-1",
            SelectedStartDate = tour.Departures.Single().StartDate,
            ContactName = "Nguyen Lan",
            ContactEmail = "lan@example.com",
            ContactPhone = "0909000999",
            AdultCount = 2,
            CouponCode = "SAVE5",
            PaymentPlanType = "Deposit"
        });

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<BookingViewModel>(view.Model);
        Assert.Equal("dep-1", model.DepartureId);
        Assert.Equal("SAVE5", model.CouponCode);
        Assert.Equal("Deposit", model.PaymentPlanType);
        Assert.False(controller.ModelState.IsValid);
    }

    [Fact]
    public async Task Resume_RedirectsOpenCheckoutSessionToPayment()
    {
        var nextMonth = DateTime.UtcNow.AddMonths(1);
        var tour = BuildTour(nextMonth, capacity: 10, bookedCount: 3);
        var controller = CreateController([tour]);
        var createResult = await controller.Create(new BookingViewModel
        {
            TourId = tour.Id,
            DepartureId = "dep-1",
            SelectedStartDate = tour.Departures.Single().StartDate,
            ContactName = "Nguyen Lan",
            ContactEmail = "lan@example.com",
            ContactPhone = "0909000999",
            AdultCount = 2,
            PaymentPlanType = "Deposit"
        });

        var redirectToPayment = Assert.IsType<RedirectToActionResult>(createResult);
        var bookingId = Assert.IsType<string>(redirectToPayment.RouteValues!["bookingId"]);
        var booking = await controller.Bookings.GetByIdAsync(bookingId);

        var result = await controller.Resume(booking.CheckoutSessionId);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Payment", redirect.ActionName);
        Assert.Equal(bookingId, redirect.RouteValues!["bookingId"]);
    }

    private static BookingController CreateController(IEnumerable<Tour> tours, IEnumerable<Promotion>? promotions = null)
    {
        var tourRepository = new InMemoryTourRepository(tours);
        var bookingRepository = new InMemoryRepository<Booking>();
        var checkoutSessionRepository = new InMemoryRepository<CheckoutSession>();
        var promotionRepository = new InMemoryRepository<Promotion>(promotions);
        var pricingService = new PricingService(new PromotionEngine(promotionRepository));
        var checkoutService = new CheckoutService(
            tourRepository,
            bookingRepository,
            checkoutSessionRepository,
            pricingService,
            new InventoryService(tourRepository),
            new NoopAnalyticsTracker());
        var bookingWorkflowService = new BookingWorkflowService(
            bookingRepository,
            new InMemoryRepository<Customer>(),
            new InMemoryRepository<Payment>(),
            new InMemoryRepository<LoyaltyLedgerEntry>(),
            new InMemoryRepository<Notification>());

        return new BookingController(
            tourRepository,
            bookingRepository,
            bookingWorkflowService,
            checkoutService,
            pricingService,
            checkoutSessionRepository);
    }

    private static Tour BuildTour(DateTime nextMonth, int capacity, int bookedCount)
    {
        return new Tour
        {
            Id = "tour-1",
            Code = "SG-01",
            Name = "Singapore Fun",
            Description = "Hanh trinh quoc te",
            ShortDescription = "Gia dinh va cap doi",
            Status = "Active",
            Destination = new Destination { City = "Singapore", Country = "Singapore", Region = "SEA" },
            Price = new TourPrice { Adult = 4200000m, Child = 2500000m, Infant = 500000m },
            Duration = new TourDuration { Days = 4, Nights = 3, Text = "4 ngay 3 dem" },
            CancellationPolicy = new TourCancellationPolicy
            {
                Summary = "Free cancellation before 48 hours",
                IsFreeCancellation = true,
                FreeCancellationBeforeHours = 48
            },
            ConfirmationType = "Instant",
            Departures =
            [
                new TourDeparture
                {
                    Id = "dep-1",
                    StartDate = new DateTime(nextMonth.Year, nextMonth.Month, 12),
                    AdultPrice = 4500000m,
                    ChildPrice = 2800000m,
                    InfantPrice = 500000m,
                    Capacity = capacity,
                    BookedCount = bookedCount,
                    ConfirmationType = "Instant"
                }
            ]
        };
    }

    private static Promotion BuildPromotion(string code, double discountPercentage, decimal minimumSpend)
    {
        return new Promotion
        {
            Id = Guid.NewGuid().ToString("N"),
            Code = code,
            Title = code,
            Description = code,
            CampaignType = "Voucher",
            DiscountPercentage = discountPercentage,
            MinimumSpend = minimumSpend,
            ValidFrom = DateTime.UtcNow.AddDays(-1),
            ValidTo = DateTime.UtcNow.AddDays(10),
            IsActive = true
        };
    }
}

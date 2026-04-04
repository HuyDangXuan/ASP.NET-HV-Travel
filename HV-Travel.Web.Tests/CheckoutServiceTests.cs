using HVTravel.Application.Models;
using HVTravel.Application.Services;
using HVTravel.Domain.Entities;
using HV_Travel.Web.Tests.TestSupport;

namespace HV_Travel.Web.Tests;

public class CheckoutServiceTests
{
    [Fact]
    public async Task CreateCheckoutAsync_ReservesDepartureAndCreatesPendingBookingAndSession()
    {
        var nextMonth = DateTime.UtcNow.AddMonths(1);
        var tour = new Tour
        {
            Id = "tour-1",
            Code = "SG-01",
            Name = "Singapore Fun",
            Description = "Hành trình quốc tế",
            ShortDescription = "Gia đình và cặp đôi",
            Status = "Active",
            Destination = new Destination { City = "Singapore", Country = "Singapore", Region = "SEA" },
            Price = new TourPrice { Adult = 4200000m, Child = 2500000m, Infant = 500000m },
            Duration = new TourDuration { Days = 4, Nights = 3, Text = "4 ngày 3 đêm" },
            Departures =
            [
                new TourDeparture
                {
                    Id = "dep-1",
                    StartDate = new DateTime(nextMonth.Year, nextMonth.Month, 12),
                    AdultPrice = 4500000m,
                    ChildPrice = 2800000m,
                    InfantPrice = 500000m,
                    Capacity = 10,
                    BookedCount = 3,
                    ConfirmationType = "Instant"
                }
            ],
            CancellationPolicy = new TourCancellationPolicy
            {
                Summary = "Hủy miễn phí trước 48 giờ",
                IsFreeCancellation = true,
                FreeCancellationBeforeHours = 48
            },
            ConfirmationType = "Instant"
        };

        var tours = new InMemoryTourRepository([tour]);
        var bookings = new InMemoryRepository<Booking>();
        var sessions = new InMemoryRepository<CheckoutSession>();
        var promotions = new InMemoryRepository<Promotion>(
        [
            new Promotion
            {
                Id = "promo-1",
                Code = "WELCOME5",
                Title = "Welcome 5",
                Description = "Giảm 5%",
                CampaignType = "Voucher",
                DiscountPercentage = 5d,
                MinimumSpend = 5000000m,
                ValidFrom = DateTime.UtcNow.AddDays(-1),
                ValidTo = DateTime.UtcNow.AddDays(10),
                IsActive = true
            }
        ]);

        var service = new CheckoutService(
            tours,
            bookings,
            sessions,
            new PricingService(new PromotionEngine(promotions)),
            new InventoryService(tours),
            new NoopAnalyticsTracker());

        var result = await service.CreateCheckoutAsync(new CreateCheckoutRequest
        {
            TourId = tour.Id,
            DepartureId = "dep-1",
            ContactName = "Nguyễn Lan",
            ContactEmail = "lan@example.com",
            ContactPhone = "0909000999",
            AdultCount = 2,
            ChildCount = 0,
            InfantCount = 0,
            CouponCode = "WELCOME5",
            PaymentPlanType = "Deposit"
        });

        Assert.Equal("dep-1", result.Booking.DepartureId);
        Assert.Equal("WELCOME5", result.Booking.CouponCode);
        Assert.Equal("PendingPayment", result.Booking.Status);
        Assert.Equal("Pending", result.Booking.PaymentStatus);
        Assert.Equal(result.Booking.Id, result.Session.BookingId);
        Assert.Equal(result.Session.Id, result.Booking.CheckoutSessionId);
        Assert.Equal("Open", result.Session.Status);
        Assert.Equal(5, tour.Departures.Single().BookedCount);
        Assert.Single(await bookings.GetAllAsync());
        Assert.Single(await sessions.GetAllAsync());
    }
}

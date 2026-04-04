using HVTravel.Application.Models;
using HVTravel.Application.Services;
using HVTravel.Domain.Entities;
using HV_Travel.Web.Tests.TestSupport;

namespace HV_Travel.Web.Tests;

public class PricingServiceTests
{
    [Fact]
    public async Task BuildQuoteAsync_AppliesEligibleCouponAndDepositPlanAgainstSelectedDeparture()
    {
        var nextMonth = DateTime.UtcNow.AddMonths(1);
        var tour = new Tour
        {
            Id = "tour-1",
            Code = "JP-2026",
            Name = "Nhật Bản mùa hoa",
            Description = "Tour mùa xuân",
            ShortDescription = "Tokyo - Kyoto",
            Status = "Active",
            Destination = new Destination { City = "Tokyo", Country = "Japan", Region = "North Asia" },
            Price = new TourPrice { Adult = 5200000m, Child = 3200000m, Infant = 600000m },
            Duration = new TourDuration { Days = 5, Nights = 4, Text = "5 ngày 4 đêm" },
            ConfirmationType = "Instant",
            CancellationPolicy = new TourCancellationPolicy
            {
                Summary = "Miễn phí hủy trước 72 giờ",
                IsFreeCancellation = true,
                FreeCancellationBeforeHours = 72
            },
            Departures =
            [
                new TourDeparture
                {
                    Id = "dep-1",
                    StartDate = new DateTime(nextMonth.Year, nextMonth.Month, 10),
                    AdultPrice = 5000000m,
                    ChildPrice = 3000000m,
                    InfantPrice = 500000m,
                    Capacity = 20,
                    BookedCount = 3,
                    ConfirmationType = "Instant"
                }
            ]
        };

        var promotionRepository = new InMemoryRepository<Promotion>(
        [
            new Promotion
            {
                Id = "promo-1",
                Code = "VIPSPRING",
                Title = "VIP Spring",
                Description = "Giảm 10% cho khách VIP",
                CampaignType = "Voucher",
                DiscountPercentage = 10d,
                MinimumSpend = 10000000m,
                ValidFrom = DateTime.UtcNow.AddDays(-1),
                ValidTo = DateTime.UtcNow.AddDays(30),
                IsActive = true,
                EligibleSegments = ["VIP"]
            }
        ]);

        var service = new PricingService(new PromotionEngine(promotionRepository));

        var quote = await service.BuildQuoteAsync(new PricingQuoteRequest
        {
            Tour = tour,
            DepartureId = "dep-1",
            AdultCount = 2,
            ChildCount = 1,
            InfantCount = 0,
            CouponCode = "VIPSPRING",
            CustomerSegment = "VIP",
            PaymentPlanType = "Deposit"
        });

        Assert.Equal("dep-1", quote.SelectedDeparture?.Id);
        Assert.Equal(13000000m, quote.Breakdown.Subtotal);
        Assert.Equal(1300000m, quote.Breakdown.DiscountTotal);
        Assert.Equal(11700000m, quote.Breakdown.GrandTotal);
        Assert.Equal("VIPSPRING", quote.AppliedCouponCode);
        Assert.Equal("Deposit", quote.PaymentPlan.PlanType);
        Assert.Equal(3510000m, quote.PaymentPlan.AmountDueNow);
        Assert.Equal(8190000m, quote.PaymentPlan.BalanceDue);
        Assert.Contains("Instant", quote.Badges);
        Assert.Contains("FreeCancellation", quote.Badges);
    }
}

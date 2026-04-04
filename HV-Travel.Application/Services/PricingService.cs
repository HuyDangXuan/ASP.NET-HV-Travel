using HVTravel.Application.Interfaces;
using HVTravel.Application.Models;
using HVTravel.Domain.Entities;

namespace HVTravel.Application.Services;

public class PricingService : IPricingService
{
    private const decimal DepositRatio = 0.30m;
    private readonly IPromotionEngine _promotionEngine;

    public PricingService(IPromotionEngine promotionEngine)
    {
        _promotionEngine = promotionEngine;
    }

    public async Task<PricingQuoteResult> BuildQuoteAsync(PricingQuoteRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Tour);

        var tour = request.Tour;
        var departure = tour.ResolveDeparture(request.DepartureId, request.SelectedStartDate)
            ?? throw new InvalidOperationException("Selected departure could not be resolved for the requested tour.");

        var adultPrice = departure.AdultPrice > 0m ? departure.AdultPrice : tour.Price?.Adult ?? 0m;
        var childPrice = departure.ChildPrice > 0m ? departure.ChildPrice : tour.Price?.Child ?? 0m;
        var infantPrice = departure.InfantPrice > 0m ? departure.InfantPrice : tour.Price?.Infant ?? 0m;

        var subtotal = (adultPrice * request.AdultCount) + (childPrice * request.ChildCount) + (infantPrice * request.InfantCount);
        var promotion = await _promotionEngine.ResolveCouponAsync(
            request.CouponCode,
            request.CustomerSegment,
            subtotal,
            tour.Destination?.City ?? tour.Destination?.Country);

        var discountTotal = promotion?.Redemption.DiscountAmount ?? 0m;
        var grandTotal = Math.Max(0m, subtotal - discountTotal);
        var paymentPlan = BuildPaymentPlan(request.PaymentPlanType, grandTotal);

        var badges = new List<string>();
        if (!string.IsNullOrWhiteSpace(departure.ConfirmationType))
        {
            badges.Add(departure.ConfirmationType);
        }

        if (tour.CancellationPolicy?.IsFreeCancellation == true)
        {
            badges.Add("FreeCancellation");
        }

        if (departure.RemainingCapacity is > 0 and <= 5)
        {
            badges.Add("LowAvailability");
        }

        if (discountTotal > 0m)
        {
            badges.Add("Deal");
        }

        return new PricingQuoteResult
        {
            Tour = tour,
            SelectedDeparture = departure,
            Breakdown = new PricingBreakdown
            {
                Subtotal = subtotal,
                DiscountTotal = discountTotal,
                GrandTotal = grandTotal
            },
            AppliedCouponCode = promotion?.Promotion.Code ?? string.Empty,
            AppliedRedemptions = promotion == null ? Array.Empty<CouponRedemption>() : [promotion.Redemption],
            PaymentPlan = paymentPlan,
            Badges = badges.Distinct(StringComparer.OrdinalIgnoreCase).ToList()
        };
    }

    private static PaymentPlan BuildPaymentPlan(string? requestedPlan, decimal grandTotal)
    {
        var planType = string.Equals(requestedPlan, "Deposit", StringComparison.OrdinalIgnoreCase)
            ? "Deposit"
            : "Full";

        if (planType == "Deposit")
        {
            var amountDueNow = Math.Round(grandTotal * DepositRatio, 0, MidpointRounding.AwayFromZero);
            return new PaymentPlan
            {
                PlanType = planType,
                DepositPercentage = DepositRatio * 100m,
                AmountDueNow = amountDueNow,
                BalanceDue = Math.Max(0m, grandTotal - amountDueNow)
            };
        }

        return new PaymentPlan
        {
            PlanType = "Full",
            DepositPercentage = 0m,
            AmountDueNow = grandTotal,
            BalanceDue = 0m
        };
    }
}

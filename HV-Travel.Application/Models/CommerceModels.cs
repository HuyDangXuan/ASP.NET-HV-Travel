using HVTravel.Domain.Entities;

namespace HVTravel.Application.Models;

public class PricingQuoteRequest
{
    public Tour? Tour { get; set; }
    public string? DepartureId { get; set; }
    public DateTime? SelectedStartDate { get; set; }
    public int AdultCount { get; set; }
    public int ChildCount { get; set; }
    public int InfantCount { get; set; }
    public string? CouponCode { get; set; }
    public string? CustomerSegment { get; set; }
    public string PaymentPlanType { get; set; } = "Full";
}

public class PricingQuoteResult
{
    public Tour Tour { get; set; } = new();
    public TourDeparture? SelectedDeparture { get; set; }
    public PricingBreakdown Breakdown { get; set; } = new();
    public string AppliedCouponCode { get; set; } = string.Empty;
    public IReadOnlyList<CouponRedemption> AppliedRedemptions { get; set; } = Array.Empty<CouponRedemption>();
    public PaymentPlan PaymentPlan { get; set; } = new();
    public IReadOnlyList<string> Badges { get; set; } = Array.Empty<string>();
}

public class PromotionMatchResult
{
    public Promotion Promotion { get; set; } = new();
    public CouponRedemption Redemption { get; set; } = new();
}

public class CreateCheckoutRequest
{
    public string TourId { get; set; } = string.Empty;
    public string? DepartureId { get; set; }
    public DateTime? SelectedStartDate { get; set; }
    public string ContactName { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;
    public int AdultCount { get; set; }
    public int ChildCount { get; set; }
    public int InfantCount { get; set; }
    public string? CouponCode { get; set; }
    public string? CustomerSegment { get; set; }
    public string PaymentPlanType { get; set; } = "Full";
    public string? SpecialRequests { get; set; }
    public string? CustomerId { get; set; }
}

public class CreateCheckoutResult
{
    public Booking Booking { get; set; } = new();
    public CheckoutSession Session { get; set; } = new();
    public PricingQuoteResult Quote { get; set; } = new();
}

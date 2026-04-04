using HVTravel.Application.Models;
using HVTravel.Domain.Models;

namespace HVTravel.Application.Interfaces;

public interface ITourSearchService
{
    Task<TourSearchResult> SearchAsync(TourSearchRequest request);
}

public interface IPricingService
{
    Task<PricingQuoteResult> BuildQuoteAsync(PricingQuoteRequest request);
}

public interface ICheckoutService
{
    Task<CreateCheckoutResult> CreateCheckoutAsync(CreateCheckoutRequest request);
}

public interface IPromotionEngine
{
    Task<PromotionMatchResult?> ResolveCouponAsync(string? couponCode, string? customerSegment, decimal subtotal, string? destination);
}

public interface IInventoryService
{
    Task<bool> ReserveDepartureAsync(string tourId, string departureId, int travellerCount);
}

public interface IAnalyticsTracker
{
    Task TrackAsync(string eventName, IReadOnlyDictionary<string, string?> properties);
}

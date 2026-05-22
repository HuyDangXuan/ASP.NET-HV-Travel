using HVTravel.Application.Models;
using HVTravel.Domain.Entities;
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

public interface IRouteInsightService
{
    RouteInsightResult Build(HVTravel.Domain.Entities.Tour? tour);
}

public interface IRouteTravelEstimator
{
    RouteTravelEstimate Estimate(
        HVTravel.Domain.Entities.TourRouteStop? fromStop,
        HVTravel.Domain.Entities.TourRouteStop? toStop,
        string? profile,
        int departureMinuteOfDay);

    string ResolveProfile(HVTravel.Domain.Entities.TourRouteStop? fromStop, HVTravel.Domain.Entities.TourRouteStop? toStop);
}

public interface IRouteOptimizationService
{
    RouteOptimizationResult Optimize(HVTravel.Domain.Entities.Tour? tour);

    RouteOptimizationResult Optimize(HVTravel.Domain.Entities.Tour? tour, RouteOptimizationRequest request);
}

public interface IRouteRecommendationService
{
    RouteRecommendationResult Recommend(IEnumerable<HVTravel.Domain.Entities.Tour>? tours, RouteRecommendationRequest? request);
}

public interface ITripPlannerService
{
    TripPlannerResult Build(TripPlannerRequest? request);
}

public interface IAnalyticsTracker
{
    Task TrackAsync(string eventName, IReadOnlyDictionary<string, string?> properties);
}

public interface ITourSearchBackend
{
    string Name { get; }

    int Priority { get; }

    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);

    Task<TourSearchResult> SearchAsync(TourSearchRequest request, CancellationToken cancellationToken = default);
}

public interface ITourSearchIndexingService
{
    Task UpsertTourAsync(Tour? tour, CancellationToken cancellationToken = default);

    Task DeleteTourAsync(string? tourId, CancellationToken cancellationToken = default);

    Task RebuildAsync(CancellationToken cancellationToken = default);
}

public interface IMeilisearchTourIndexClient
{
    Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);

    Task EnsureConfiguredAsync(CancellationToken cancellationToken = default);

    Task<MeilisearchTourSearchResponse> SearchAsync(MeilisearchTourSearchCommand command, CancellationToken cancellationToken = default);

    Task UpsertDocumentsAsync(IReadOnlyCollection<TourSearchDocument> documents, CancellationToken cancellationToken = default);

    Task DeleteDocumentsAsync(IReadOnlyCollection<string> ids, CancellationToken cancellationToken = default);

    Task ReplaceAllDocumentsAsync(IReadOnlyCollection<TourSearchDocument> documents, CancellationToken cancellationToken = default);
}

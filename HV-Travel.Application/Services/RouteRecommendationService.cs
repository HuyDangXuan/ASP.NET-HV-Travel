using HVTravel.Application.Interfaces;
using HVTravel.Application.Models;
using HVTravel.Domain.Entities;

namespace HVTravel.Application.Services;

public class RouteRecommendationService : IRouteRecommendationService
{
    private readonly IRouteInsightService _routeInsightService;

    public RouteRecommendationService()
        : this(new RouteInsightService())
    {
    }

    public RouteRecommendationService(IRouteInsightService routeInsightService)
    {
        _routeInsightService = routeInsightService;
    }

    public RouteRecommendationResult Recommend(IEnumerable<Tour>? tours, RouteRecommendationRequest? request)
    {
        request ??= new RouteRecommendationRequest();
        var routeStyle = RouteRecommendationStyles.Normalize(request.RouteStyle);
        var allCandidates = (tours ?? Array.Empty<Tour>())
            .Where(tour => tour != null)
            .ToList();

        if (allCandidates.Count == 0)
        {
            return new RouteRecommendationResult();
        }

        var currentTour = !string.IsNullOrWhiteSpace(request.CurrentTourId)
            ? allCandidates.FirstOrDefault(tour => string.Equals(tour.Id, request.CurrentTourId, StringComparison.Ordinal))
            : null;

        var currentCity = string.IsNullOrWhiteSpace(request.CurrentCity) ? currentTour?.Destination?.City : request.CurrentCity;
        var currentRegion = string.IsNullOrWhiteSpace(request.CurrentRegion) ? currentTour?.Destination?.Region : request.CurrentRegion;
        var currentDurationDays = currentTour?.Duration?.Days;

        var candidates = allCandidates
            .Where(tour => !string.Equals(tour.Id, request.CurrentTourId, StringComparison.Ordinal))
            .ToList();

        if (candidates.Count == 0)
        {
            return new RouteRecommendationResult();
        }

        var scoreInputs = candidates
            .Select(tour => BuildRawMetrics(tour, request.Travellers))
            .ToList();

        var travelCostMin = scoreInputs.Min(item => item.TravelCostRaw);
        var travelCostMax = scoreInputs.Max(item => item.TravelCostRaw);
        var highlightMin = scoreInputs.Min(item => item.HighlightRaw);
        var highlightMax = scoreInputs.Max(item => item.HighlightRaw);
        var priceMin = scoreInputs.Min(item => item.StartingAdultPrice);
        var priceMax = scoreInputs.Max(item => item.StartingAdultPrice);
        var durationMin = scoreInputs.Min(item => item.DurationDays);
        var durationMax = scoreInputs.Max(item => item.DurationDays);
        var availabilityMin = scoreInputs.Min(item => item.AvailabilityRaw);
        var availabilityMax = scoreInputs.Max(item => item.AvailabilityRaw);
        var coverageMin = scoreInputs.Min(item => item.RouteCoverageRaw);
        var coverageMax = scoreInputs.Max(item => item.RouteCoverageRaw);

        var weights = ResolveWeights(routeStyle);
        var scores = scoreInputs
            .Select(item =>
            {
                var travelEfficiency = NormalizeDescending(item.TravelCostRaw, travelCostMin, travelCostMax);
                var highlightStrength = NormalizeAscending(item.HighlightRaw, highlightMin, highlightMax);
                var priceAffordability = NormalizeDescending((double)item.StartingAdultPrice, (double)priceMin, (double)priceMax);
                var durationCompactness = NormalizeDescending(item.DurationDays, durationMin, durationMax);
                var availabilityFit = NormalizeAscending(item.AvailabilityRaw, availabilityMin, availabilityMax);
                var routeCoverage = NormalizeAscending(item.RouteCoverageRaw, coverageMin, coverageMax);
                var similarityBonus = CalculateSimilarityBonus(item.Tour, currentCity, currentRegion, currentDurationDays);

                return new RouteRecommendationScore
                {
                    TourId = item.Tour.Id ?? string.Empty,
                    TravelEfficiency = travelEfficiency,
                    HighlightStrength = highlightStrength,
                    PriceAffordability = priceAffordability,
                    DurationCompactness = durationCompactness,
                    AvailabilityFit = availabilityFit,
                    RouteCoverage = routeCoverage,
                    SimilarityBonus = similarityBonus,
                    Score =
                        (travelEfficiency * weights.TravelEfficiency)
                        + (highlightStrength * weights.HighlightStrength)
                        + (priceAffordability * weights.PriceAffordability)
                        + (durationCompactness * weights.DurationCompactness)
                        + (availabilityFit * weights.AvailabilityFit)
                        + (routeCoverage * weights.RouteCoverage)
                        + similarityBonus
                };
            })
            .ToList();

        var scoreLookup = scores.ToDictionary(item => item.TourId, StringComparer.Ordinal);
        var rankedTours = candidates
            .OrderByDescending(tour => scoreLookup[tour.Id].Score)
            .ThenByDescending(tour => tour.Rating)
            .ThenBy(tour => GetStartingAdultPrice(tour))
            .ThenBy(tour => tour.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new RouteRecommendationResult
        {
            Items = rankedTours,
            Scores = rankedTours
                .Select(tour => scoreLookup[tour.Id])
                .ToList()
        };
    }

    private RawMetrics BuildRawMetrics(Tour tour, int travellers)
    {
        var routeInsight = _routeInsightService.Build(tour);
        var durationDays = Math.Max(1, tour.Duration?.Days ?? 1);
        var startingAdultPrice = GetStartingAdultPrice(tour);
        var availabilityRaw = CalculateAvailabilityFit(tour, travellers);
        var hasRouting = routeInsight.HasRouting && routeInsight.StopCount > 0;

        var travelCostRaw = hasRouting
            ? (routeInsight.TotalTravelMinutes + (routeInsight.TotalDistanceKm * 1.5d)) / Math.Max(routeInsight.StopCount, 1)
            : durationDays * 45d;

        var highlightRaw = hasRouting
            ? (routeInsight.AverageAttractionScore ?? Math.Min(10d, tour.Rating * 2d)) + Math.Min(2d, routeInsight.StopCount / 4d)
            : Math.Min(10d, tour.Rating * 1.8d);

        var routeCoverageRaw = hasRouting
            ? Math.Min(1d, routeInsight.StopCount / Math.Max(durationDays * 2d, 1d))
            : 0.15d;

        return new RawMetrics
        {
            Tour = tour,
            DurationDays = durationDays,
            StartingAdultPrice = startingAdultPrice,
            AvailabilityRaw = availabilityRaw,
            TravelCostRaw = travelCostRaw,
            HighlightRaw = highlightRaw,
            RouteCoverageRaw = routeCoverageRaw
        };
    }

    private static Weights ResolveWeights(string routeStyle)
    {
        return routeStyle switch
        {
            RouteRecommendationStyles.Compact => new Weights
            {
                TravelEfficiency = 0.35d,
                DurationCompactness = 0.20d,
                PriceAffordability = 0.15d,
                HighlightStrength = 0.10d,
                AvailabilityFit = 0.10d,
                RouteCoverage = 0.10d
            },
            RouteRecommendationStyles.Highlights => new Weights
            {
                HighlightStrength = 0.40d,
                TravelEfficiency = 0.20d,
                PriceAffordability = 0.10d,
                DurationCompactness = 0.10d,
                AvailabilityFit = 0.10d,
                RouteCoverage = 0.10d
            },
            _ => new Weights
            {
                TravelEfficiency = 0.25d,
                HighlightStrength = 0.20d,
                PriceAffordability = 0.20d,
                DurationCompactness = 0.15d,
                AvailabilityFit = 0.10d,
                RouteCoverage = 0.10d
            }
        };
    }

    private static double CalculateAvailabilityFit(Tour tour, int travellers)
    {
        var departures = tour.EffectiveDepartures?.ToList() ?? new List<TourDeparture>();
        if (departures.Count == 0)
        {
            return 0d;
        }

        var requiredTravellers = Math.Max(1, travellers);
        var matchingDepartures = departures
            .Where(departure => departure.RemainingCapacity >= requiredTravellers)
            .ToList();

        if (matchingDepartures.Count == 0)
        {
            return 0d;
        }

        return matchingDepartures
            .Select(departure => departure.Capacity > 0
                ? departure.RemainingCapacity / (double)departure.Capacity
                : 0d)
            .DefaultIfEmpty(0d)
            .Max();
    }

    private static double CalculateSimilarityBonus(Tour tour, string? currentCity, string? currentRegion, int? currentDurationDays)
    {
        var bonus = 0d;

        if (!string.IsNullOrWhiteSpace(currentCity)
            && string.Equals(tour.Destination?.City, currentCity, StringComparison.OrdinalIgnoreCase))
        {
            bonus += 0.20d;
        }

        if (!string.IsNullOrWhiteSpace(currentRegion)
            && string.Equals(tour.Destination?.Region, currentRegion, StringComparison.OrdinalIgnoreCase))
        {
            bonus += 0.10d;
        }

        if (currentDurationDays.HasValue
            && Math.Abs((tour.Duration?.Days ?? currentDurationDays.Value) - currentDurationDays.Value) <= 1)
        {
            bonus += 0.05d;
        }

        return bonus;
    }

    private static decimal GetStartingAdultPrice(Tour tour)
    {
        var departurePrices = tour.EffectiveDepartures
            .Select(item => item.AdultPrice)
            .Where(value => value > 0m)
            .ToList();

        return departurePrices.Count != 0 ? departurePrices.Min() : tour.Price?.Adult ?? 0m;
    }

    private static double NormalizeAscending(double value, double min, double max)
    {
        return Math.Abs(max - min) < double.Epsilon ? 1d : (value - min) / (max - min);
    }

    private static double NormalizeDescending(double value, double min, double max)
    {
        return Math.Abs(max - min) < double.Epsilon ? 1d : (max - value) / (max - min);
    }

    private sealed class RawMetrics
    {
        public required Tour Tour { get; init; }

        public double TravelCostRaw { get; init; }

        public double HighlightRaw { get; init; }

        public double AvailabilityRaw { get; init; }

        public double RouteCoverageRaw { get; init; }

        public decimal StartingAdultPrice { get; init; }

        public int DurationDays { get; init; }
    }

    private sealed class Weights
    {
        public double TravelEfficiency { get; init; }

        public double HighlightStrength { get; init; }

        public double PriceAffordability { get; init; }

        public double DurationCompactness { get; init; }

        public double AvailabilityFit { get; init; }

        public double RouteCoverage { get; init; }
    }
}

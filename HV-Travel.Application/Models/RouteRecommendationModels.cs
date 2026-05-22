using HVTravel.Domain.Entities;

namespace HVTravel.Application.Models;

public static class RouteRecommendationStyles
{
    public const string Compact = "compact";
    public const string Balanced = "balanced";
    public const string Highlights = "highlights";

    public static string Normalize(string? value)
    {
        return value?.Trim().ToLowerInvariant() switch
        {
            Compact => Compact,
            Highlights => Highlights,
            _ => Balanced
        };
    }
}

public class RouteRecommendationRequest
{
    public string RouteStyle { get; set; } = RouteRecommendationStyles.Balanced;

    public int Travellers { get; set; }

    public string? CurrentTourId { get; set; }

    public string? CurrentCity { get; set; }

    public string? CurrentRegion { get; set; }
}

public class RouteRecommendationResult
{
    public IReadOnlyList<Tour> Items { get; set; } = Array.Empty<Tour>();

    public IReadOnlyList<RouteRecommendationScore> Scores { get; set; } = Array.Empty<RouteRecommendationScore>();
}

public class RouteRecommendationScore
{
    public string TourId { get; set; } = string.Empty;

    public double Score { get; set; }

    public double TravelEfficiency { get; set; }

    public double HighlightStrength { get; set; }

    public double PriceAffordability { get; set; }

    public double DurationCompactness { get; set; }

    public double AvailabilityFit { get; set; }

    public double RouteCoverage { get; set; }

    public double SimilarityBonus { get; set; }
}

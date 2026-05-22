using HVTravel.Domain.Entities;

namespace HVTravel.Domain.Models;

public class TourSearchRequest
{
    public string? Search { get; set; }
    public string? Sort { get; set; }
    public string? RouteStyle { get; set; }
    public string? Region { get; set; }
    public string? Destination { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public int? DepartureMonth { get; set; }
    public int? MaxDays { get; set; }
    public string? Collection { get; set; }
    public bool AvailableOnly { get; set; }
    public bool PromotionOnly { get; set; }
    public int Travellers { get; set; }
    public string? ConfirmationType { get; set; }
    public string? CancellationType { get; set; }
    public bool UseRecommendationRanking { get; set; }
    public bool PublicOnly { get; set; } = true;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 9;
}

public class TourSearchResult
{
    public List<Tour> Items { get; set; } = new();
    public int TotalItems { get; set; }
    public int TotalPages { get; set; }
    public int CurrentPage { get; set; } = 1;
    public IReadOnlyList<TourFacetOption> Regions { get; set; } = Array.Empty<TourFacetOption>();
    public IReadOnlyList<TourFacetOption> Destinations { get; set; } = Array.Empty<TourFacetOption>();
    public IReadOnlyList<TourFacetOption> ConfirmationTypes { get; set; } = Array.Empty<TourFacetOption>();
    public IReadOnlyList<TourFacetOption> CancellationTypes { get; set; } = Array.Empty<TourFacetOption>();
}

public class TourFacetOption
{
    public string Value { get; set; } = string.Empty;
    public int Count { get; set; }
    public bool Selected { get; set; }
}

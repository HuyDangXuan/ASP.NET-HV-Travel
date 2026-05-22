using HVTravel.Domain.Entities;

namespace HVTravel.Application.Models;

public class TripPlannerRequest
{
    public IEnumerable<Tour> SelectedTours { get; set; } = Array.Empty<Tour>();

    public IEnumerable<Tour> CandidateTours { get; set; } = Array.Empty<Tour>();

    public string RouteStyle { get; set; } = RouteRecommendationStyles.Balanced;

    public int Travellers { get; set; } = 2;

    public int? MaxDays { get; set; } = 7;
}

public class TripPlannerResult
{
    public IReadOnlyList<TripPlannerItem> SelectedItems { get; set; } = Array.Empty<TripPlannerItem>();

    public IReadOnlyList<TripPlannerItem> SuggestedItems { get; set; } = Array.Empty<TripPlannerItem>();

    public IReadOnlyList<TripPlannerDay> Days { get; set; } = Array.Empty<TripPlannerDay>();

    public int TotalDays { get; set; }

    public decimal TotalStartingAdultPrice { get; set; }

    public int TotalVisitMinutes { get; set; }

    public int TotalTravelMinutes { get; set; }

    public int TotalJourneyMinutes { get; set; }

    public double TotalDistanceKm { get; set; }

    public IReadOnlyList<TripPlannerWarning> Warnings { get; set; } = Array.Empty<TripPlannerWarning>();
}

public class TripPlannerItem
{
    public string TourId { get; set; } = string.Empty;

    public string DetailIdentifier { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string DestinationLabel { get; set; } = string.Empty;

    public string DurationText { get; set; } = string.Empty;

    public int DurationDays { get; set; }

    public decimal StartingAdultPrice { get; set; }

    public double Rating { get; set; }

    public string Source { get; set; } = "selected";

    public string ImageUrl { get; set; } = string.Empty;

    public bool HasRouting { get; set; }

    public int StopCount { get; set; }

    public int VisitMinutes { get; set; }

    public int TravelMinutes { get; set; }

    public int JourneyMinutes { get; set; }

    public double DistanceKm { get; set; }
}

public class TripPlannerDay
{
    public int DayNumber { get; set; }

    public string TourId { get; set; } = string.Empty;

    public string TourName { get; set; } = string.Empty;

    public int TourDay { get; set; }

    public string Title { get; set; } = string.Empty;

    public bool HasRouting { get; set; }

    public int StopCount { get; set; }

    public int VisitMinutes { get; set; }

    public int TravelMinutes { get; set; }

    public int JourneyMinutes { get; set; }
}

public class TripPlannerWarning
{
    public string Code { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string TourId { get; set; } = string.Empty;
}

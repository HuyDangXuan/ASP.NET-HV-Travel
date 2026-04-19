namespace HVTravel.Application.Models;

public class RouteOptimizationResult
{
    public bool CanOptimize { get; set; }

    public RouteInsightResult CurrentInsight { get; set; } = RouteInsightResult.Empty;

    public RouteInsightResult SuggestedInsight { get; set; } = RouteInsightResult.Empty;

    public IReadOnlyList<RouteOptimizationAssignment> Assignments { get; set; } = Array.Empty<RouteOptimizationAssignment>();

    public IReadOnlyList<RouteOptimizationDayResult> Days { get; set; } = Array.Empty<RouteOptimizationDayResult>();

    public IReadOnlyList<RouteOptimizationWarning> Warnings { get; set; } = Array.Empty<RouteOptimizationWarning>();

    public string UnchangedReason { get; set; } = string.Empty;
}

public class RouteOptimizationDayResult
{
    public int Day { get; set; }

    public bool Changed { get; set; }

    public int StopCount { get; set; }

    public int CurrentTravelMinutes { get; set; }

    public int SuggestedTravelMinutes { get; set; }

    public int CurrentJourneyMinutes { get; set; }

    public int SuggestedJourneyMinutes { get; set; }

    public double CurrentDistanceKm { get; set; }

    public double SuggestedDistanceKm { get; set; }

    public IReadOnlyList<RouteOptimizationStopPreview> CurrentStops { get; set; } = Array.Empty<RouteOptimizationStopPreview>();

    public IReadOnlyList<RouteOptimizationStopPreview> SuggestedStops { get; set; } = Array.Empty<RouteOptimizationStopPreview>();
}

public class RouteOptimizationAssignment
{
    public string ClientKey { get; set; } = string.Empty;

    public int Day { get; set; }

    public int Order { get; set; }
}

public class RouteOptimizationStopPreview
{
    public string ClientKey { get; set; } = string.Empty;

    public int Day { get; set; }

    public int Order { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public int VisitMinutes { get; set; }

    public double AttractionScore { get; set; }

    public string Note { get; set; } = string.Empty;
}

public class RouteOptimizationWarning
{
    public string Code { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public int? Day { get; set; }

    public string ClientKey { get; set; } = string.Empty;
}

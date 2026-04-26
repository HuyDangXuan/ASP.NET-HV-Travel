using HVTravel.Application.Models;

namespace HVTravel.Web.Models;

public class RouteOptimizationPreviewRequest
{
    public string Profile { get; set; } = RouteOptimizationProfiles.Balanced;

    public List<RouteOptimizationPreviewScheduleItem> Schedule { get; set; } = new();

    public List<RouteOptimizationPreviewStop> Stops { get; set; } = new();
}

public class RouteOptimizationPreviewScheduleItem
{
    public int Day { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;
}

public class RouteOptimizationPreviewStop
{
    public string ClientKey { get; set; } = string.Empty;

    public int Day { get; set; }

    public int Order { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public RouteOptimizationPreviewGeoPoint Coordinates { get; set; } = new();

    public int VisitMinutes { get; set; }

    public double AttractionScore { get; set; }

    public string Note { get; set; } = string.Empty;
}

public class RouteOptimizationPreviewGeoPoint
{
    public double? Lat { get; set; }

    public double? Lng { get; set; }
}

public class RouteOptimizationPreviewResponse
{
    public bool CanOptimize { get; set; }

    public string Profile { get; set; } = RouteOptimizationProfiles.Balanced;

    public string ProfileLabel { get; set; } = RouteOptimizationProfiles.GetLabel(RouteOptimizationProfiles.Balanced);

    public string ProfileDescription { get; set; } = RouteOptimizationProfiles.GetDescription(RouteOptimizationProfiles.Balanced);

    public double CurrentObjectiveScore { get; set; }

    public double SuggestedObjectiveScore { get; set; }

    public RouteInsightResult CurrentInsight { get; set; } = RouteInsightResult.Empty;

    public RouteInsightResult SuggestedInsight { get; set; } = RouteInsightResult.Empty;

    public IReadOnlyList<RouteOptimizationAssignment> Assignments { get; set; } = Array.Empty<RouteOptimizationAssignment>();

    public IReadOnlyList<RouteOptimizationDayResult> Days { get; set; } = Array.Empty<RouteOptimizationDayResult>();

    public IReadOnlyList<RouteOptimizationWarning> Warnings { get; set; } = Array.Empty<RouteOptimizationWarning>();

    public string UnchangedReason { get; set; } = string.Empty;
}

public class RouteOptimizationPreviewErrorResponse
{
    public IReadOnlyList<string> Errors { get; set; } = Array.Empty<string>();
}

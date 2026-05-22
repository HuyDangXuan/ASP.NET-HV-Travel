namespace HVTravel.Application.Models;

public class RouteOptimizationRequest
{
    public string Profile { get; set; } = RouteOptimizationProfiles.Balanced;
}

public static class RouteOptimizationProfiles
{
    public const string Balanced = "balanced";
    public const string DistanceFirst = "distance_first";
    public const string HighlightsFirst = "highlights_first";

    public static string Normalize(string? profile)
    {
        return profile?.Trim().ToLowerInvariant() switch
        {
            DistanceFirst => DistanceFirst,
            HighlightsFirst => HighlightsFirst,
            _ => Balanced
        };
    }

    public static string GetLabel(string? profile)
    {
        return Normalize(profile) switch
        {
            DistanceFirst => "Ưu tiên quãng đường",
            HighlightsFirst => "Ưu tiên điểm nổi bật",
            _ => "Cân bằng"
        };
    }

    public static string GetDescription(string? profile)
    {
        return Normalize(profile) switch
        {
            DistanceFirst => "Ưu tiên đường đi ngắn hơn và chi phí di chuyển thấp hơn nhưng vẫn giữ lộ trình ổn định.",
            HighlightsFirst => "Đưa các điểm nổi bật lên sớm hơn trong ngày mà không phá vỡ hiệu quả di chuyển theo lộ trình.",
            _ => "Cân bằng thời gian di chuyển, quãng đường, mức ưu tiên điểm tham quan và độ ổn định của lộ trình."
        };
    }
}

public class RouteOptimizationResult
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

public class RouteOptimizationDayResult
{
    public int Day { get; set; }

    public bool Changed { get; set; }

    public int StopCount { get; set; }

    public double CurrentObjectiveScore { get; set; }

    public double SuggestedObjectiveScore { get; set; }

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

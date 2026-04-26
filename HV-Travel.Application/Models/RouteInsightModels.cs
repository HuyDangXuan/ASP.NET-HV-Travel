namespace HVTravel.Application.Models;

public class RouteTravelEstimate
{
    public double DistanceKm { get; set; }

    public int DriveMinutes { get; set; }

    public int JunctionDelayMinutes { get; set; }

    public int TravelMinutes { get; set; }

    public string DayPart { get; set; } = "night";

    public string CongestionLevel { get; set; } = "low";
}

public class RouteInsightResult
{
    public static RouteInsightResult Empty { get; } = new();

    public bool HasRouting { get; set; }

    public int DayCount { get; set; }

    public int StopCount { get; set; }

    public int TotalVisitMinutes { get; set; }

    public int TotalTravelMinutes { get; set; }

    public int TotalJourneyMinutes { get; set; }

    public double TotalDistanceKm { get; set; }

    public double? AverageAttractionScore { get; set; }

    public IReadOnlyList<RouteInsightDay> Days { get; set; } = Array.Empty<RouteInsightDay>();

    public IReadOnlyList<RouteInsightWarning> Warnings { get; set; } = Array.Empty<RouteInsightWarning>();
}

public class RouteInsightDay
{
    public int Day { get; set; }

    public int StopCount { get; set; }

    public int VisitMinutes { get; set; }

    public int TravelMinutes { get; set; }

    public int JourneyMinutes { get; set; }

    public double DistanceKm { get; set; }

    public double? AverageAttractionScore { get; set; }

    public string PeakDayPart { get; set; } = string.Empty;

    public string PeakCongestionLevel { get; set; } = string.Empty;

    public IReadOnlyList<RouteInsightLeg> Legs { get; set; } = Array.Empty<RouteInsightLeg>();
}

public class RouteInsightLeg
{
    public int Day { get; set; }

    public string FromStopId { get; set; } = string.Empty;

    public string ToStopId { get; set; } = string.Empty;

    public string Profile { get; set; } = "default";

    public double DistanceKm { get; set; }

    public int DriveMinutes { get; set; }

    public int JunctionDelayMinutes { get; set; }

    public int TravelMinutes { get; set; }

    public string DayPart { get; set; } = "night";

    public string CongestionLevel { get; set; } = "low";

    public int DepartureMinuteOfDay { get; set; }

    public int ArrivalMinuteOfDay { get; set; }
}

public class RouteInsightWarning
{
    public string Code { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public int? Day { get; set; }

    public string StopId { get; set; } = string.Empty;
}

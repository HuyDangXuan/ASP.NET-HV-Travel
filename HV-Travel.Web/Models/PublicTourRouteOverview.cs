namespace HVTravel.Web.Models;

public class PublicTourRouteOverview
{
    public static PublicTourRouteOverview Empty { get; } = new();

    public bool HasRouting { get; set; }

    public int DayCount { get; set; }

    public int StopCount { get; set; }

    public int TotalVisitMinutes { get; set; }

    public int TotalTravelMinutes { get; set; }

    public int TotalJourneyMinutes { get; set; }

    public double TotalDistanceKm { get; set; }

    public IReadOnlyList<PublicTourRouteDay> Days { get; set; } = Array.Empty<PublicTourRouteDay>();
}

public class PublicTourRouteDay
{
    public int Day { get; set; }

    public string DayTitle { get; set; } = string.Empty;

    public int TravelMinutes { get; set; }

    public int JourneyMinutes { get; set; }

    public double DistanceKm { get; set; }

    public string PeakDayPart { get; set; } = string.Empty;

    public string PeakCongestionLevel { get; set; } = string.Empty;

    public IReadOnlyList<PublicTourRouteStopViewModel> Stops { get; set; } = Array.Empty<PublicTourRouteStopViewModel>();
}

public class PublicTourRouteStopViewModel
{
    public string Name { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public int VisitMinutes { get; set; }

    public string Note { get; set; } = string.Empty;

    public PublicTourRouteTransferViewModel? TransferFromPrevious { get; set; }
}

public class PublicTourRouteTransferViewModel
{
    public double DistanceKm { get; set; }

    public int DriveMinutes { get; set; }

    public int JunctionDelayMinutes { get; set; }

    public int TravelMinutes { get; set; }

    public string DayPart { get; set; } = string.Empty;

    public string CongestionLevel { get; set; } = string.Empty;
}

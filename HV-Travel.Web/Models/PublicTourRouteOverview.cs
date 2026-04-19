namespace HVTravel.Web.Models;

public class PublicTourRouteOverview
{
    public static PublicTourRouteOverview Empty { get; } = new();

    public bool HasRouting { get; set; }

    public int DayCount { get; set; }

    public int StopCount { get; set; }

    public int TotalVisitMinutes { get; set; }

    public List<PublicTourRouteDay> Days { get; set; } = new();
}

public class PublicTourRouteDay
{
    public int Day { get; set; }

    public string DayTitle { get; set; } = string.Empty;

    public List<PublicTourRouteStopViewModel> Stops { get; set; } = new();
}

public class PublicTourRouteStopViewModel
{
    public string Name { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public int VisitMinutes { get; set; }

    public string Note { get; set; } = string.Empty;
}

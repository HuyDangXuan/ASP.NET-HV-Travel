using HVTravel.Application.Models;

namespace HVTravel.Web.Models;

public class TripPlannerPreviewRequest
{
    public List<string> TourIds { get; set; } = [];

    public string RouteStyle { get; set; } = "balanced";

    public int Travellers { get; set; } = 2;

    public int? MaxDays { get; set; } = 7;
}

public class TripPlannerPreviewResponse
{
    public TripPlannerResult Plan { get; set; } = new();

    public List<string> Warnings { get; set; } = [];
}

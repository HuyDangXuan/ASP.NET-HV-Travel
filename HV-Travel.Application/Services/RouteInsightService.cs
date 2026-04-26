using HVTravel.Application.Interfaces;
using HVTravel.Application.Models;
using HVTravel.Domain.Entities;

namespace HVTravel.Application.Services;

public class RouteInsightService : IRouteInsightService
{
    private const int DefaultDayStartMinuteOfDay = 8 * 60;

    private readonly IRouteTravelEstimator _routeTravelEstimator;

    public RouteInsightService()
        : this(new RouteTravelEstimator())
    {
    }

    public RouteInsightService(IRouteTravelEstimator routeTravelEstimator)
    {
        _routeTravelEstimator = routeTravelEstimator;
    }

    public RouteInsightResult Build(Tour? tour)
    {
        if (tour?.Routing?.Stops == null || tour.Routing.Stops.Count == 0)
        {
            return RouteInsightResult.Empty;
        }

        var validStops = tour.Routing.Stops
            .Where(stop => stop != null && stop.Day > 0 && stop.Order > 0 && !string.IsNullOrWhiteSpace(stop.Name))
            .OrderBy(stop => stop.Day)
            .ThenBy(stop => stop.Order)
            .ToList();

        if (validStops.Count == 0)
        {
            return RouteInsightResult.Empty;
        }

        var warnings = new List<RouteInsightWarning>();
        var days = validStops
            .GroupBy(stop => stop.Day)
            .OrderBy(group => group.Key)
            .Select(group => BuildDay(group.Key, group.ToList(), warnings))
            .ToList();

        var scores = validStops
            .Where(stop => stop.AttractionScore >= 0d && stop.AttractionScore <= 10d)
            .Select(stop => stop.AttractionScore)
            .ToList();

        return new RouteInsightResult
        {
            HasRouting = true,
            DayCount = days.Count,
            StopCount = validStops.Count,
            TotalVisitMinutes = Math.Max(0, validStops.Sum(stop => Math.Max(0, stop.VisitMinutes))),
            TotalTravelMinutes = Math.Max(0, days.Sum(day => day.TravelMinutes)),
            TotalJourneyMinutes = Math.Max(0, days.Sum(day => day.JourneyMinutes)),
            TotalDistanceKm = days.Sum(day => day.DistanceKm),
            AverageAttractionScore = scores.Count > 0 ? scores.Average() : null,
            Days = days,
            Warnings = warnings
        };
    }

    private RouteInsightDay BuildDay(int day, IReadOnlyList<TourRouteStop> stops, ICollection<RouteInsightWarning> warnings)
    {
        var orderedStops = stops
            .OrderBy(stop => stop.Order)
            .ToList();

        var legs = new List<RouteInsightLeg>();
        var currentMinuteOfDay = DefaultDayStartMinuteOfDay;

        for (var index = 0; index < orderedStops.Count - 1; index++)
        {
            var fromStop = orderedStops[index];
            var toStop = orderedStops[index + 1];
            currentMinuteOfDay += Math.Max(0, fromStop.VisitMinutes);

            if (!HasCoordinatePair(fromStop.Coordinates) || !HasCoordinatePair(toStop.Coordinates))
            {
                warnings.Add(new RouteInsightWarning
                {
                    Code = "missing_coordinates",
                    Day = day,
                    StopId = string.IsNullOrWhiteSpace(toStop.Id) ? fromStop.Id : toStop.Id,
                    Message = $"Missing coordinates for day {day} leg between '{fromStop.Name}' and '{toStop.Name}'."
                });
                continue;
            }

            var profile = _routeTravelEstimator.ResolveProfile(fromStop, toStop);
            var estimate = _routeTravelEstimator.Estimate(fromStop, toStop, profile, currentMinuteOfDay);
            var arrivalMinuteOfDay = currentMinuteOfDay + estimate.TravelMinutes;

            legs.Add(new RouteInsightLeg
            {
                Day = day,
                FromStopId = fromStop.Id ?? string.Empty,
                ToStopId = toStop.Id ?? string.Empty,
                Profile = profile,
                DistanceKm = estimate.DistanceKm,
                DriveMinutes = estimate.DriveMinutes,
                JunctionDelayMinutes = estimate.JunctionDelayMinutes,
                TravelMinutes = estimate.TravelMinutes,
                DayPart = estimate.DayPart,
                CongestionLevel = estimate.CongestionLevel,
                DepartureMinuteOfDay = currentMinuteOfDay,
                ArrivalMinuteOfDay = arrivalMinuteOfDay
            });

            currentMinuteOfDay = arrivalMinuteOfDay;
        }

        var scores = orderedStops
            .Where(stop => stop.AttractionScore >= 0d && stop.AttractionScore <= 10d)
            .Select(stop => stop.AttractionScore)
            .ToList();

        var visitMinutes = orderedStops.Sum(stop => Math.Max(0, stop.VisitMinutes));
        var travelMinutes = legs.Sum(leg => leg.TravelMinutes);
        var peakLeg = ResolvePeakLeg(legs);

        return new RouteInsightDay
        {
            Day = day,
            StopCount = orderedStops.Count,
            VisitMinutes = visitMinutes,
            TravelMinutes = travelMinutes,
            JourneyMinutes = visitMinutes + travelMinutes,
            DistanceKm = legs.Sum(leg => leg.DistanceKm),
            AverageAttractionScore = scores.Count > 0 ? scores.Average() : null,
            PeakDayPart = peakLeg?.DayPart ?? string.Empty,
            PeakCongestionLevel = peakLeg?.CongestionLevel ?? string.Empty,
            Legs = legs
        };
    }

    private static RouteInsightLeg? ResolvePeakLeg(IEnumerable<RouteInsightLeg> legs)
    {
        return legs
            .OrderByDescending(leg => CongestionSeverity(leg.CongestionLevel))
            .ThenByDescending(leg => PeakWindowPriority(leg.DayPart))
            .ThenByDescending(leg => leg.TravelMinutes)
            .FirstOrDefault();
    }

    private static int CongestionSeverity(string? value)
    {
        return value switch
        {
            "high" => 3,
            "moderate" => 2,
            _ => 1
        };
    }

    private static int PeakWindowPriority(string? value)
    {
        return value switch
        {
            "morning_peak" => 3,
            "evening_peak" => 2,
            "late_morning" or "midday" or "afternoon" => 1,
            _ => 0
        };
    }

    private static bool HasCoordinatePair(GeoPoint? coordinates)
    {
        return coordinates?.Lat.HasValue == true && coordinates.Lng.HasValue;
    }
}

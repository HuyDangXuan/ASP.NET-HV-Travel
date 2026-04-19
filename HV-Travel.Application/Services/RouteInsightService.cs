using HVTravel.Application.Interfaces;
using HVTravel.Application.Models;
using HVTravel.Domain.Entities;

namespace HVTravel.Application.Services;

public class RouteInsightService : IRouteInsightService
{
    private const double EarthRadiusKm = 6371d;
    private static readonly string[] UrbanKeywords = ["market", "city", "museum", "landmark", "meeting"];
    private static readonly string[] ScenicKeywords = ["viewpoint", "beach", "park", "forest", "lake"];

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

    private static RouteInsightDay BuildDay(int day, IReadOnlyList<TourRouteStop> stops, ICollection<RouteInsightWarning> warnings)
    {
        var orderedStops = stops
            .OrderBy(stop => stop.Order)
            .ToList();

        var legs = new List<RouteInsightLeg>();
        for (var index = 0; index < orderedStops.Count - 1; index++)
        {
            var fromStop = orderedStops[index];
            var toStop = orderedStops[index + 1];

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

            var profile = ResolveProfile(toStop.Type, fromStop.Type);
            var distanceKm = CalculateDistanceKm(
                fromStop.Coordinates!.Lat!.Value,
                fromStop.Coordinates.Lng!.Value,
                toStop.Coordinates!.Lat!.Value,
                toStop.Coordinates.Lng!.Value);

            var speedKmPerHour = profile switch
            {
                "urban" => 24d,
                "scenic" => 32d,
                _ => 28d
            };

            var junctionDelayMinutes = profile switch
            {
                "urban" => 6,
                "scenic" => 3,
                _ => 4
            };

            var driveMinutes = Math.Max(5, (int)Math.Round((distanceKm / speedKmPerHour) * 60d, MidpointRounding.AwayFromZero));

            legs.Add(new RouteInsightLeg
            {
                Day = day,
                FromStopId = fromStop.Id ?? string.Empty,
                ToStopId = toStop.Id ?? string.Empty,
                Profile = profile,
                DistanceKm = distanceKm,
                DriveMinutes = driveMinutes,
                JunctionDelayMinutes = junctionDelayMinutes,
                TravelMinutes = driveMinutes + junctionDelayMinutes
            });
        }

        var scores = orderedStops
            .Where(stop => stop.AttractionScore >= 0d && stop.AttractionScore <= 10d)
            .Select(stop => stop.AttractionScore)
            .ToList();

        var visitMinutes = orderedStops.Sum(stop => Math.Max(0, stop.VisitMinutes));
        var travelMinutes = legs.Sum(leg => leg.TravelMinutes);

        return new RouteInsightDay
        {
            Day = day,
            StopCount = orderedStops.Count,
            VisitMinutes = visitMinutes,
            TravelMinutes = travelMinutes,
            JourneyMinutes = visitMinutes + travelMinutes,
            DistanceKm = legs.Sum(leg => leg.DistanceKm),
            AverageAttractionScore = scores.Count > 0 ? scores.Average() : null,
            Legs = legs
        };
    }

    private static bool HasCoordinatePair(GeoPoint? coordinates)
    {
        return coordinates?.Lat.HasValue == true && coordinates.Lng.HasValue;
    }

    private static string ResolveProfile(string? primaryType, string? fallbackType)
    {
        var type = !string.IsNullOrWhiteSpace(primaryType) ? primaryType : fallbackType;
        if (string.IsNullOrWhiteSpace(type))
        {
            return "default";
        }

        if (ContainsAny(type, UrbanKeywords))
        {
            return "urban";
        }

        if (ContainsAny(type, ScenicKeywords))
        {
            return "scenic";
        }

        return "default";
    }

    private static bool ContainsAny(string value, IEnumerable<string> keywords)
    {
        return keywords.Any(keyword => value.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }

    private static double CalculateDistanceKm(double fromLat, double fromLng, double toLat, double toLng)
    {
        var latDistance = DegreesToRadians(toLat - fromLat);
        var lngDistance = DegreesToRadians(toLng - fromLng);
        var a = Math.Pow(Math.Sin(latDistance / 2d), 2d)
                + Math.Cos(DegreesToRadians(fromLat))
                * Math.Cos(DegreesToRadians(toLat))
                * Math.Pow(Math.Sin(lngDistance / 2d), 2d);

        var c = 2d * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1d - a));
        return EarthRadiusKm * c;
    }

    private static double DegreesToRadians(double degrees)
    {
        return degrees * (Math.PI / 180d);
    }
}

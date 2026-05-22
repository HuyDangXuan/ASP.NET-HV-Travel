using HVTravel.Application.Interfaces;
using HVTravel.Application.Models;
using HVTravel.Domain.Entities;

namespace HVTravel.Application.Services;

public class RouteTravelEstimator : IRouteTravelEstimator
{
    private const double EarthRadiusKm = 6371d;
    private static readonly string[] UrbanKeywords = ["market", "city", "museum", "landmark", "meeting"];
    private static readonly string[] ScenicKeywords = ["viewpoint", "beach", "park", "forest", "lake"];

    public RouteTravelEstimate Estimate(TourRouteStop? fromStop, TourRouteStop? toStop, string? profile, int departureMinuteOfDay)
    {
        var normalizedProfile = NormalizeProfile(profile);
        var normalizedDepartureMinute = NormalizeMinuteOfDay(departureMinuteOfDay);
        var dayPart = ResolveDayPart(normalizedDepartureMinute);
        var distanceKm = CalculateDistanceKm(fromStop, toStop);
        if (distanceKm <= 0d)
        {
            return new RouteTravelEstimate
            {
                DayPart = dayPart,
                CongestionLevel = ResolveCongestionLevel(normalizedProfile, dayPart)
            };
        }

        var speedKmPerHour = ResolveSpeedKmPerHour(normalizedProfile, dayPart);
        var junctionDelayMinutes = ResolveJunctionDelayMinutes(normalizedProfile, dayPart);
        var driveMinutes = Math.Max(5, (int)Math.Round((distanceKm / speedKmPerHour) * 60d, MidpointRounding.AwayFromZero));

        return new RouteTravelEstimate
        {
            DistanceKm = distanceKm,
            DriveMinutes = driveMinutes,
            JunctionDelayMinutes = junctionDelayMinutes,
            TravelMinutes = driveMinutes + junctionDelayMinutes,
            DayPart = dayPart,
            CongestionLevel = ResolveCongestionLevel(normalizedProfile, dayPart)
        };
    }

    public string ResolveProfile(TourRouteStop? fromStop, TourRouteStop? toStop)
    {
        var type = !string.IsNullOrWhiteSpace(toStop?.Type) ? toStop.Type : fromStop?.Type;
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

    private static string NormalizeProfile(string? profile)
    {
        return profile?.Trim().ToLowerInvariant() switch
        {
            "urban" => "urban",
            "scenic" => "scenic",
            _ => "default"
        };
    }

    private static int NormalizeMinuteOfDay(int departureMinuteOfDay)
    {
        if (departureMinuteOfDay < 0)
        {
            return 0;
        }

        return departureMinuteOfDay % (24 * 60);
    }

    private static string ResolveDayPart(int minuteOfDay)
    {
        return minuteOfDay switch
        {
            >= 300 and <= 419 => "early_morning",
            >= 420 and <= 539 => "morning_peak",
            >= 540 and <= 689 => "late_morning",
            >= 690 and <= 809 => "midday",
            >= 810 and <= 989 => "afternoon",
            >= 990 and <= 1139 => "evening_peak",
            _ => "night"
        };
    }

    private static double ResolveSpeedKmPerHour(string profile, string dayPart)
    {
        return (profile, dayPart) switch
        {
            ("urban", "morning_peak" or "evening_peak") => 24d,
            ("urban", "late_morning" or "midday" or "afternoon") => 26d,
            ("urban", _) => 28d,
            ("scenic", "morning_peak" or "evening_peak") => 31d,
            ("scenic", "late_morning" or "midday" or "afternoon") => 33d,
            ("scenic", _) => 34d,
            ("default", "morning_peak" or "evening_peak") => 26d,
            ("default", "late_morning" or "midday" or "afternoon") => 28d,
            _ => 30d
        };
    }

    private static int ResolveJunctionDelayMinutes(string profile, string dayPart)
    {
        return (profile, dayPart) switch
        {
            ("urban", "morning_peak" or "evening_peak") => 6,
            ("urban", "late_morning" or "midday" or "afternoon") => 5,
            ("urban", _) => 4,
            ("scenic", "morning_peak" or "evening_peak") => 3,
            ("scenic", "late_morning" or "midday" or "afternoon") => 2,
            ("scenic", _) => 2,
            ("default", "morning_peak" or "evening_peak") => 5,
            ("default", "late_morning" or "midday" or "afternoon") => 4,
            _ => 3
        };
    }

    private static string ResolveCongestionLevel(string profile, string dayPart)
    {
        return (profile, dayPart) switch
        {
            ("urban", "morning_peak" or "evening_peak") => "high",
            ("default", "morning_peak" or "evening_peak") => "moderate",
            ("urban", "late_morning" or "midday" or "afternoon") => "moderate",
            ("default", "late_morning" or "midday" or "afternoon") => "moderate",
            ("scenic", "morning_peak" or "evening_peak") => "moderate",
            _ => "low"
        };
    }

    private static bool ContainsAny(string value, IEnumerable<string> keywords)
    {
        return keywords.Any(keyword => value.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }

    private static double CalculateDistanceKm(TourRouteStop? fromStop, TourRouteStop? toStop)
    {
        if (!HasCoordinatePair(fromStop?.Coordinates) || !HasCoordinatePair(toStop?.Coordinates))
        {
            return 0d;
        }

        var latDistance = DegreesToRadians(toStop!.Coordinates!.Lat!.Value - fromStop!.Coordinates!.Lat!.Value);
        var lngDistance = DegreesToRadians(toStop.Coordinates.Lng!.Value - fromStop.Coordinates.Lng!.Value);
        var a = Math.Pow(Math.Sin(latDistance / 2d), 2d)
                + Math.Cos(DegreesToRadians(fromStop.Coordinates.Lat.Value))
                * Math.Cos(DegreesToRadians(toStop.Coordinates.Lat.Value))
                * Math.Pow(Math.Sin(lngDistance / 2d), 2d);

        var c = 2d * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1d - a));
        return EarthRadiusKm * c;
    }

    private static bool HasCoordinatePair(GeoPoint? coordinates)
    {
        return coordinates?.Lat.HasValue == true && coordinates.Lng.HasValue;
    }

    private static double DegreesToRadians(double degrees)
    {
        return degrees * (Math.PI / 180d);
    }
}

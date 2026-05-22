using HVTravel.Application.Models;
using HVTravel.Application.Services;
using HVTravel.Domain.Entities;
using HVTravel.Web.Models;

namespace HVTravel.Web.Services;

public static class PublicTourRouteOverviewBuilder
{
    public static PublicTourRouteOverview Build(Tour? tour, RouteInsightResult? routeInsight = null)
    {
        if (tour?.Routing?.Stops == null || tour.Routing.Stops.Count == 0)
        {
            return PublicTourRouteOverview.Empty;
        }

        routeInsight ??= new RouteInsightService().Build(tour);

        var validStops = tour.Routing.Stops
            .Where(stop => stop != null && stop.Day > 0 && stop.Order > 0 && !string.IsNullOrWhiteSpace(stop.Name))
            .OrderBy(stop => stop.Day)
            .ThenBy(stop => stop.Order)
            .ToList();

        if (validStops.Count == 0)
        {
            return PublicTourRouteOverview.Empty;
        }

        var days = validStops
            .GroupBy(stop => stop.Day)
            .OrderBy(group => group.Key)
            .Select(group =>
            {
                var matchingSchedule = tour.Schedule?.FirstOrDefault(item => item.Day == group.Key);
                var routeDayInsight = routeInsight.Days.FirstOrDefault(item => item.Day == group.Key);
                var legLookup = (routeDayInsight?.Legs ?? Array.Empty<RouteInsightLeg>())
                    .ToDictionary(leg => leg.ToStopId, StringComparer.Ordinal);
                return new PublicTourRouteDay
                {
                    Day = group.Key,
                    DayTitle = string.IsNullOrWhiteSpace(matchingSchedule?.Title) ? $"Day {group.Key}" : matchingSchedule.Title,
                    TravelMinutes = routeDayInsight?.TravelMinutes ?? 0,
                    JourneyMinutes = routeDayInsight?.JourneyMinutes ?? 0,
                    DistanceKm = routeDayInsight?.DistanceKm ?? 0d,
                    PeakDayPart = routeDayInsight?.PeakDayPart ?? string.Empty,
                    PeakCongestionLevel = routeDayInsight?.PeakCongestionLevel ?? string.Empty,
                    Stops = group
                        .OrderBy(stop => stop.Order)
                        .Select(stop => new PublicTourRouteStopViewModel
                        {
                            Name = stop.Name,
                            Type = stop.Type,
                            VisitMinutes = stop.VisitMinutes,
                            Note = stop.Note,
                            TransferFromPrevious = legLookup.TryGetValue(stop.Id ?? string.Empty, out var transferLeg)
                                ? new PublicTourRouteTransferViewModel
                                {
                                    DistanceKm = transferLeg.DistanceKm,
                                    DriveMinutes = transferLeg.DriveMinutes,
                                    JunctionDelayMinutes = transferLeg.JunctionDelayMinutes,
                                    TravelMinutes = transferLeg.TravelMinutes,
                                    DayPart = transferLeg.DayPart,
                                    CongestionLevel = transferLeg.CongestionLevel
                                }
                                : null
                        })
                        .ToList()
                };
            })
            .ToList();

        return new PublicTourRouteOverview
        {
            HasRouting = routeInsight.HasRouting,
            DayCount = days.Count,
            StopCount = validStops.Count,
            TotalVisitMinutes = routeInsight.TotalVisitMinutes,
            TotalTravelMinutes = routeInsight.TotalTravelMinutes,
            TotalJourneyMinutes = routeInsight.TotalJourneyMinutes,
            TotalDistanceKm = routeInsight.TotalDistanceKm,
            Days = days
        };
    }
}

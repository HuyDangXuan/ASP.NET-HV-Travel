using HVTravel.Domain.Entities;
using HVTravel.Web.Models;

namespace HVTravel.Web.Services;

public static class PublicTourRouteOverviewBuilder
{
    public static PublicTourRouteOverview Build(Tour? tour)
    {
        if (tour?.Routing?.Stops == null || tour.Routing.Stops.Count == 0)
        {
            return PublicTourRouteOverview.Empty;
        }

        var dayTitles = (tour.Schedule ?? [])
            .Where(item => item.Day > 0 && !string.IsNullOrWhiteSpace(item.Title))
            .GroupBy(item => item.Day)
            .ToDictionary(group => group.Key, group => group.First().Title);

        var routeDays = tour.Routing.Stops
            .Where(IsValidStop)
            .OrderBy(stop => stop.Day)
            .ThenBy(stop => stop.Order)
            .GroupBy(stop => stop.Day)
            .Select(group => new PublicTourRouteDay
            {
                Day = group.Key,
                DayTitle = dayTitles.TryGetValue(group.Key, out var title) ? title : $"Day {group.Key}",
                Stops = group.Select(stop => new PublicTourRouteStopViewModel
                {
                    Name = stop.Name,
                    Type = stop.Type,
                    VisitMinutes = Math.Max(0, stop.VisitMinutes),
                    Note = stop.Note
                }).ToList()
            })
            .ToList();

        if (routeDays.Count == 0)
        {
            return PublicTourRouteOverview.Empty;
        }

        return new PublicTourRouteOverview
        {
            HasRouting = true,
            DayCount = routeDays.Count,
            StopCount = routeDays.Sum(day => day.Stops.Count),
            TotalVisitMinutes = routeDays.Sum(day => day.Stops.Sum(stop => stop.VisitMinutes)),
            Days = routeDays
        };
    }

    private static bool IsValidStop(TourRouteStop? stop)
    {
        return stop != null
            && stop.Day > 0
            && !string.IsNullOrWhiteSpace(stop.Name);
    }
}

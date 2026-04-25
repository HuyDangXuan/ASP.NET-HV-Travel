using HVTravel.Application.Interfaces;
using HVTravel.Application.Models;
using HVTravel.Domain.Entities;

namespace HVTravel.Application.Services;

public class TripPlannerService : ITripPlannerService
{
    private const int DefaultMaxDays = 7;
    private const int DefaultTravellers = 2;
    private const int MaxPlannerItems = 6;

    private readonly IRouteInsightService _routeInsightService;
    private readonly IRouteRecommendationService _routeRecommendationService;

    public TripPlannerService()
        : this(new RouteInsightService(), new RouteRecommendationService(new RouteInsightService()))
    {
    }

    public TripPlannerService(IRouteInsightService routeInsightService, IRouteRecommendationService routeRecommendationService)
    {
        _routeInsightService = routeInsightService;
        _routeRecommendationService = routeRecommendationService;
    }

    public TripPlannerResult Build(TripPlannerRequest? request)
    {
        request ??= new TripPlannerRequest();

        var routeStyle = RouteRecommendationStyles.Normalize(request.RouteStyle);
        var travellers = request.Travellers > 0 ? request.Travellers : DefaultTravellers;
        var maxDays = Math.Clamp(request.MaxDays ?? DefaultMaxDays, 1, 30);
        var warnings = new List<TripPlannerWarning>();
        var selectedItems = new List<TripPlannerItem>();
        var suggestedItems = new List<TripPlannerItem>();
        var days = new List<TripPlannerDay>();
        var addedTourIds = new HashSet<string>(StringComparer.Ordinal);
        var selectedTours = DedupeTours(request.SelectedTours).ToList();
        var selectedIds = selectedTours
            .Where(tour => !string.IsNullOrWhiteSpace(tour.Id))
            .Select(tour => tour.Id)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var tour in selectedTours)
        {
            TryAddTour(
                tour,
                "selected",
                maxDays,
                addedTourIds,
                selectedItems,
                days,
                warnings);
        }

        if (selectedItems.Count + suggestedItems.Count < MaxPlannerItems)
        {
            var candidateTours = DedupeTours(request.CandidateTours)
                .Where(tour => !selectedIds.Contains(tour.Id))
                .Where(tour => IsPubliclyVisible(tour.Status))
                .ToList();

            var recommendation = _routeRecommendationService.Recommend(candidateTours, new RouteRecommendationRequest
            {
                RouteStyle = routeStyle,
                Travellers = travellers,
                CurrentTourId = selectedTours.FirstOrDefault()?.Id,
                CurrentCity = selectedTours.FirstOrDefault()?.Destination?.City,
                CurrentRegion = selectedTours.FirstOrDefault()?.Destination?.Region
            });

            var consideredSuggestions = 0;
            foreach (var tour in recommendation.Items)
            {
                if (selectedItems.Count + suggestedItems.Count >= MaxPlannerItems
                    || consideredSuggestions >= MaxPlannerItems)
                {
                    break;
                }

                consideredSuggestions++;
                TryAddTour(
                    tour,
                    "suggested",
                    maxDays,
                    addedTourIds,
                    suggestedItems,
                    days,
                    warnings);
            }
        }

        var includedItems = selectedItems.Concat(suggestedItems).ToList();
        return new TripPlannerResult
        {
            SelectedItems = selectedItems,
            SuggestedItems = suggestedItems,
            Days = days,
            TotalDays = days.Count,
            TotalStartingAdultPrice = includedItems.Sum(item => item.StartingAdultPrice),
            TotalVisitMinutes = includedItems.Sum(item => item.VisitMinutes),
            TotalTravelMinutes = includedItems.Sum(item => item.TravelMinutes),
            TotalJourneyMinutes = includedItems.Sum(item => item.JourneyMinutes),
            TotalDistanceKm = Math.Round(includedItems.Sum(item => item.DistanceKm), 1),
            Warnings = warnings
                .GroupBy(warning => $"{warning.Code}:{warning.TourId}", StringComparer.Ordinal)
                .Select(group => group.First())
                .ToList()
        };
    }

    private void TryAddTour(
        Tour tour,
        string source,
        int maxDays,
        ISet<string> addedTourIds,
        ICollection<TripPlannerItem> targetItems,
        ICollection<TripPlannerDay> targetDays,
        ICollection<TripPlannerWarning> warnings)
    {
        if (string.IsNullOrWhiteSpace(tour.Id) || addedTourIds.Contains(tour.Id))
        {
            return;
        }

        var durationDays = Math.Max(1, tour.Duration?.Days ?? 1);
        if (targetDays.Count + durationDays > maxDays)
        {
            warnings.Add(new TripPlannerWarning
            {
                Code = "max-days-exceeded",
                TourId = tour.Id,
                Message = $"Tour \"{tour.Name}\" bị bỏ qua vì vượt giới hạn {maxDays} ngày của planner."
            });
            return;
        }

        var insight = _routeInsightService.Build(tour);
        var item = BuildItem(tour, source, durationDays, insight, warnings);
        targetItems.Add(item);
        addedTourIds.Add(tour.Id);

        for (var tourDay = 1; tourDay <= durationDays; tourDay++)
        {
            var routeDay = insight.Days.FirstOrDefault(day => day.Day == tourDay);
            var scheduleTitle = tour.Schedule?
                .FirstOrDefault(day => day.Day == tourDay)?
                .Title;

            targetDays.Add(new TripPlannerDay
            {
                DayNumber = targetDays.Count + 1,
                TourId = tour.Id,
                TourName = tour.Name ?? string.Empty,
                TourDay = tourDay,
                Title = !string.IsNullOrWhiteSpace(scheduleTitle)
                    ? scheduleTitle
                    : $"{tour.Name} - Ngày {tourDay}",
                HasRouting = routeDay != null,
                StopCount = routeDay?.StopCount ?? 0,
                VisitMinutes = routeDay?.VisitMinutes ?? 0,
                TravelMinutes = routeDay?.TravelMinutes ?? 0,
                JourneyMinutes = routeDay?.JourneyMinutes ?? 0
            });
        }
    }

    private TripPlannerItem BuildItem(
        Tour tour,
        string source,
        int durationDays,
        RouteInsightResult insight,
        ICollection<TripPlannerWarning> warnings)
    {
        if (!insight.HasRouting)
        {
            warnings.Add(new TripPlannerWarning
            {
                Code = "missing-routing",
                TourId = tour.Id,
                Message = $"Tour \"{tour.Name}\" chưa có dữ liệu lộ trình có cấu trúc."
            });
        }

        var startingAdultPrice = ResolveStartingAdultPrice(tour, warnings);
        return new TripPlannerItem
        {
            TourId = tour.Id,
            DetailIdentifier = string.IsNullOrWhiteSpace(tour.Slug) ? tour.Id : tour.Slug,
            Name = tour.Name ?? string.Empty,
            DestinationLabel = BuildDestinationLabel(tour),
            DurationText = string.IsNullOrWhiteSpace(tour.Duration?.Text) ? $"{durationDays} ngày" : tour.Duration.Text,
            DurationDays = durationDays,
            StartingAdultPrice = startingAdultPrice,
            Rating = tour.Rating,
            Source = source,
            ImageUrl = tour.Images?.FirstOrDefault() ?? string.Empty,
            HasRouting = insight.HasRouting,
            StopCount = insight.StopCount,
            VisitMinutes = insight.TotalVisitMinutes,
            TravelMinutes = insight.TotalTravelMinutes,
            JourneyMinutes = insight.TotalJourneyMinutes,
            DistanceKm = Math.Round(insight.TotalDistanceKm, 1)
        };
    }

    private static decimal ResolveStartingAdultPrice(Tour tour, ICollection<TripPlannerWarning> warnings)
    {
        var departures = tour.EffectiveDepartures
            .Where(departure => departure.StartDate >= DateTime.UtcNow.Date)
            .OrderBy(departure => departure.AdultPrice)
            .ToList();

        if (departures.Count == 0)
        {
            departures = tour.EffectiveDepartures
                .OrderBy(departure => departure.AdultPrice)
                .ToList();
        }

        var departurePrice = departures
            .Where(departure => departure.AdultPrice > 0m)
            .Select(departure => departure.AdultPrice)
            .FirstOrDefault();

        if (departurePrice > 0m)
        {
            return departurePrice;
        }

        warnings.Add(new TripPlannerWarning
        {
            Code = "missing-departure",
            TourId = tour.Id,
            Message = $"Tour \"{tour.Name}\" chưa có lịch khởi hành mở có giá; hệ thống dùng giá mặc định của tour."
        });

        return tour.Price?.Adult ?? 0m;
    }

    private static IEnumerable<Tour> DedupeTours(IEnumerable<Tour>? tours)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var tour in tours ?? Array.Empty<Tour>())
        {
            if (tour == null || string.IsNullOrWhiteSpace(tour.Id) || !seen.Add(tour.Id))
            {
                continue;
            }

            yield return tour;
        }
    }

    private static string BuildDestinationLabel(Tour tour)
    {
        var parts = new[] { tour.Destination?.City, tour.Destination?.Region, tour.Destination?.Country }
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!.Trim())
            .ToList();

        return parts.Count > 0 ? string.Join(", ", parts) : "Việt Nam";
    }

    private static bool IsPubliclyVisible(string? status)
    {
        return status is "Active" or "ComingSoon" or "SoldOut";
    }
}

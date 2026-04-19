using System.Text;
using HVTravel.Application.Interfaces;
using HVTravel.Application.Models;
using HVTravel.Domain.Entities;
using HVTravel.Domain.Interfaces;
using HVTravel.Domain.Models;
using HVTravel.Domain.Utils;

namespace HVTravel.Web.Services;

public interface ITourAiRouteAdvisorContextBuilder
{
    Task<TourAiRouteAdvisorContext> BuildAsync(Tour tour, string? routeStyle, CancellationToken cancellationToken = default);
}

public sealed class TourAiRouteAdvisorContext
{
    public string RouteStyle { get; init; } = RouteRecommendationStyles.Balanced;

    public string RouteStyleLabel { get; init; } = "balanced";

    public string SnapshotText { get; init; } = string.Empty;

    public IReadOnlyList<string> SuggestedPrompts { get; init; } = Array.Empty<string>();

    public IReadOnlyList<TourAiRelatedRouteSummary> RelatedTourSummaries { get; init; } = Array.Empty<TourAiRelatedRouteSummary>();
}

public sealed class TourAiRelatedRouteSummary
{
    public string Name { get; init; } = string.Empty;

    public string DestinationLabel { get; init; } = string.Empty;

    public string DurationText { get; init; } = string.Empty;

    public decimal StartingAdultPrice { get; init; }

    public double Rating { get; init; }

    public string RouteSummary { get; init; } = string.Empty;

    public string Reason { get; init; } = string.Empty;
}

public sealed class TourAiRouteAdvisorContextBuilder : ITourAiRouteAdvisorContextBuilder
{
    private const int RelatedTourLimit = 3;
    private const int CandidateLimit = 24;

    private readonly ITourRepository _tourRepository;
    private readonly IRouteInsightService _routeInsightService;
    private readonly IRouteRecommendationService _routeRecommendationService;

    public TourAiRouteAdvisorContextBuilder(
        ITourRepository tourRepository,
        IRouteInsightService routeInsightService,
        IRouteRecommendationService routeRecommendationService)
    {
        _tourRepository = tourRepository;
        _routeInsightService = routeInsightService;
        _routeRecommendationService = routeRecommendationService;
    }

    public async Task<TourAiRouteAdvisorContext> BuildAsync(Tour tour, string? routeStyle, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tour);

        var normalizedRouteStyle = RouteRecommendationStyles.Normalize(routeStyle);
        var routeInsight = _routeInsightService.Build(tour);
        var relatedTours = await BuildRelatedTourSummariesAsync(tour, normalizedRouteStyle, cancellationToken);
        var snapshotText = BuildSnapshotText(tour, routeInsight, normalizedRouteStyle, relatedTours);

        return new TourAiRouteAdvisorContext
        {
            RouteStyle = normalizedRouteStyle,
            RouteStyleLabel = GetRouteStyleLabel(normalizedRouteStyle),
            SnapshotText = snapshotText,
            SuggestedPrompts = BuildSuggestedPrompts(routeInsight.HasRouting, normalizedRouteStyle),
            RelatedTourSummaries = relatedTours
        };
    }

    private async Task<IReadOnlyList<TourAiRelatedRouteSummary>> BuildRelatedTourSummariesAsync(
        Tour currentTour,
        string routeStyle,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var candidates = await SearchRelatedCandidatesAsync(currentTour, cancellationToken);
        if (candidates.Count == 0)
        {
            return Array.Empty<TourAiRelatedRouteSummary>();
        }

        var recommendation = _routeRecommendationService.Recommend(
            candidates,
            new RouteRecommendationRequest
            {
                RouteStyle = routeStyle,
                CurrentTourId = currentTour.Id,
                CurrentCity = currentTour.Destination?.City,
                CurrentRegion = currentTour.Destination?.Region
            });

        return recommendation.Items
            .Where(tour => !string.Equals(tour.Id, currentTour.Id, StringComparison.Ordinal))
            .Where(tour => IsPubliclyVisible(tour.Status))
            .Take(RelatedTourLimit)
            .Select(tour => BuildRelatedSummary(tour, routeStyle))
            .ToList();
    }

    private async Task<List<Tour>> SearchRelatedCandidatesAsync(Tour currentTour, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var request = new TourSearchRequest
        {
            Region = currentTour.Destination?.Region,
            Page = 1,
            PageSize = CandidateLimit,
            PublicOnly = true,
            UseRecommendationRanking = false
        };

        var result = await _tourRepository.SearchAsync(request);
        return (result.Items ?? [])
            .Where(tour => !string.Equals(tour.Id, currentTour.Id, StringComparison.Ordinal))
            .Where(tour => IsPubliclyVisible(tour.Status))
            .ToList();
    }

    private TourAiRelatedRouteSummary BuildRelatedSummary(Tour tour, string routeStyle)
    {
        var insight = _routeInsightService.Build(tour);

        return new TourAiRelatedRouteSummary
        {
            Name = tour.Name ?? string.Empty,
            DestinationLabel = BuildDestinationLabel(tour),
            DurationText = ValueOrFallback(tour.Duration?.Text),
            StartingAdultPrice = ResolveStartingAdultPrice(tour),
            Rating = tour.Rating,
            RouteSummary = insight.HasRouting
                ? $"{insight.StopCount} stops, travel {insight.TotalTravelMinutes} minutes, journey {insight.TotalJourneyMinutes} minutes"
                : "No structured routing data",
            Reason = BuildRelatedReason(routeStyle, tour, insight)
        };
    }

    private string BuildSnapshotText(
        Tour tour,
        RouteInsightResult routeInsight,
        string routeStyle,
        IReadOnlyList<TourAiRelatedRouteSummary> relatedTours)
    {
        var departures = ResolveDepartures(tour)
            .OrderBy(item => item.StartDate)
            .Take(6)
            .ToList();
        var builder = new StringBuilder();

        builder.AppendLine("Route-aware AI advisor context:");
        builder.AppendLine($"- Route style: {routeStyle} ({GetRouteStyleLabel(routeStyle)}).");
        builder.AppendLine("- Coordinate privacy: khong lo toa do, lat/lng, raw coordinates.");
        builder.AppendLine($"- Tour name: {ValueOrFallback(tour.Name)}");
        builder.AppendLine($"- Tour code: {ValueOrFallback(tour.Code)}");
        builder.AppendLine($"- Destination: {BuildDestinationLabel(tour)}");
        builder.AppendLine($"- Duration: {ValueOrFallback(tour.Duration?.Text)}");
        builder.AppendLine($"- Short description: {ValueOrFallback(RichTextContentFormatter.ToPlainTextSummary(tour.ShortDescription ?? tour.Description, 320))}");
        builder.AppendLine($"- Highlights: {BuildListLine(tour.Highlights)}");
        builder.AppendLine($"- Includes: {BuildListLine(tour.GeneratedInclusions)}");
        builder.AppendLine($"- Excludes: {BuildListLine(tour.GeneratedExclusions)}");
        builder.AppendLine($"- Meeting point: {ValueOrFallback(tour.MeetingPoint)}");
        builder.AppendLine($"- Cancellation policy: {ValueOrFallback(tour.CancellationPolicy?.Summary)}");
        builder.AppendLine($"- Confirmation: {ValueOrFallback(tour.ConfirmationType)}");
        builder.AppendLine($"- Default adult price: {FormatCurrency(tour.Price?.Adult)}");

        builder.AppendLine("- Schedule summary:");
        if (tour.Schedule is { Count: > 0 })
        {
            foreach (var item in tour.Schedule.OrderBy(entry => entry.Day).Take(8))
            {
                builder.AppendLine($"  * Day {item.Day:00}: {ValueOrFallback(item.Title)}. {ValueOrFallback(RichTextContentFormatter.ToPlainText(item.Description))}");
            }
        }
        else
        {
            builder.AppendLine("  * No detailed schedule data.");
        }

        builder.AppendLine("- Departure availability:");
        if (departures.Count > 0)
        {
            builder.AppendLine($"  * Starting adult price: {FormatCurrency(departures.Min(item => item.AdultPrice))}");
            foreach (var departure in departures)
            {
                builder.AppendLine(
                    $"  * {departure.StartDate:dd/MM/yyyy}: adult {FormatCurrency(departure.AdultPrice)}, child {FormatCurrency(departure.ChildPrice)}, infant {FormatCurrency(departure.InfantPrice)}, remaining {departure.RemainingCapacity} seats, status {ValueOrFallback(departure.Status)}, confirmation {ValueOrFallback(departure.ConfirmationType)}");
            }
        }
        else
        {
            builder.AppendLine("  * No open departure data.");
        }

        if (routeInsight.HasRouting)
        {
            builder.AppendLine("- RouteInsight ETA v2 summary:");
            builder.AppendLine($"  * Total stops: {routeInsight.StopCount}");
            builder.AppendLine($"  * Total visit minutes: {routeInsight.TotalVisitMinutes}");
            builder.AppendLine($"  * Total travel minutes: {routeInsight.TotalTravelMinutes}");
            builder.AppendLine($"  * Total journey minutes: {routeInsight.TotalJourneyMinutes}");
            builder.AppendLine($"  * Total distance km: {routeInsight.TotalDistanceKm:0.0}");

            foreach (var day in routeInsight.Days.Take(6))
            {
                var peakTraffic = !string.IsNullOrWhiteSpace(day.PeakDayPart)
                    ? $"{day.PeakDayPart}/{day.PeakCongestionLevel}"
                    : "stable";
                builder.AppendLine(
                    $"  * Day {day.Day:00}: {day.StopCount} stops, visit {day.VisitMinutes} minutes, travel {day.TravelMinutes} minutes, journey {day.JourneyMinutes} minutes, traffic {peakTraffic}.");
            }

            if (routeInsight.Warnings.Count > 0)
            {
                builder.AppendLine("- Route warnings:");
                foreach (var warning in routeInsight.Warnings.Take(4))
                {
                    builder.AppendLine($"  * {warning.Message}");
                }
            }
        }
        else
        {
            builder.AppendLine("- RouteInsight ETA v2 summary: tour nay chua co du lieu routing co cau truc.");
        }

        builder.AppendLine("- Related route-aware tours:");
        if (relatedTours.Count > 0)
        {
            foreach (var related in relatedTours)
            {
                builder.AppendLine(
                    $"  * {related.Name}: {related.DestinationLabel}, {related.DurationText}, from {FormatCurrency(related.StartingAdultPrice)}, rating {related.Rating:0.0}, route {related.RouteSummary}, reason {related.Reason}");
            }
        }
        else
        {
            builder.AppendLine("  * No related public tours were found in the current route context.");
        }

        return builder.ToString().Trim();
    }

    private static IReadOnlyList<string> BuildSuggestedPrompts(bool hasRouting, string routeStyle)
    {
        var routeStyleLabel = GetRouteStyleLabel(routeStyle);

        if (!hasRouting)
        {
            return
            [
                "Tour nay phu hop voi ai?",
                "Co ngay khoi hanh nao con cho khong?",
                "Tour nay co diem noi bat nao?",
                "Co tour lien quan nao de so sanh khong?"
            ];
        }

        return
        [
            "Tour nay di chuyen nhieu khong?",
            "Ngay nao trong lich trinh co nhieu thoi gian di chuyen nhat?",
            $"Tour nay hop voi kieu hanh trinh {routeStyleLabel} khong?",
            "Co tour nao tuong tu nhung gon hon khong?"
        ];
    }

    private static IReadOnlyList<TourDeparture> ResolveDepartures(Tour tour)
    {
        if (tour.Departures is { Count: > 0 })
        {
            return tour.Departures;
        }

        return tour.EffectiveDepartures;
    }

    private static decimal ResolveStartingAdultPrice(Tour tour)
    {
        var departures = ResolveDepartures(tour);
        var departurePrice = departures
            .Where(item => item.AdultPrice > 0m)
            .OrderBy(item => item.AdultPrice)
            .Select(item => item.AdultPrice)
            .FirstOrDefault();

        return departurePrice > 0m ? departurePrice : tour.Price?.Adult ?? 0m;
    }

    private static string BuildRelatedReason(string routeStyle, Tour tour, RouteInsightResult insight)
    {
        var similarity = $"{BuildDestinationLabel(tour)} with {ValueOrFallback(tour.Duration?.Text)}";
        return routeStyle switch
        {
            RouteRecommendationStyles.Compact => insight.HasRouting
                ? $"compact fit from shorter travel profile; {similarity}"
                : $"compact fallback by duration and availability; {similarity}",
            RouteRecommendationStyles.Highlights => insight.AverageAttractionScore.HasValue
                ? $"highlight fit from attraction strength {insight.AverageAttractionScore:0.0}; {similarity}"
                : $"highlight fallback by rating and destination; {similarity}",
            _ => $"balanced fit across route, price, availability and destination; {similarity}"
        };
    }

    private static string GetRouteStyleLabel(string routeStyle)
    {
        return routeStyle switch
        {
            RouteRecommendationStyles.Compact => "compact",
            RouteRecommendationStyles.Highlights => "highlights",
            _ => "balanced"
        };
    }

    private static bool IsPubliclyVisible(string? status)
    {
        return status is "Active" or "ComingSoon" or "SoldOut";
    }

    private static string BuildDestinationLabel(Tour tour)
    {
        var parts = new[] { tour.Destination?.City, tour.Destination?.Region, tour.Destination?.Country }
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!.Trim())
            .ToList();

        return parts.Count > 0 ? string.Join(", ", parts) : "No destination data";
    }

    private static string BuildListLine(IEnumerable<string>? values)
    {
        var items = values?.Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value.Trim()).ToList() ?? [];
        return items.Count > 0 ? string.Join("; ", items) : "No data";
    }

    private static string ValueOrFallback(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "No data" : value.Trim();
    }

    private static string FormatCurrency(decimal? value)
    {
        var amount = value.GetValueOrDefault();
        return amount > 0m ? $"{amount:N0} VND" : "No data";
    }
}

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
                ? $"{insight.StopCount} điểm dừng, di chuyển {insight.TotalTravelMinutes} phút, hành trình {insight.TotalJourneyMinutes} phút"
                : "Chưa có dữ liệu lộ trình có cấu trúc",
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

        builder.AppendLine("Ngữ cảnh AI tư vấn tour theo lộ trình:");
        builder.AppendLine($"- Kiểu hành trình: {routeStyle} ({GetRouteStyleLabel(routeStyle)}).");
        builder.AppendLine("- Quy tắc riêng tư tọa độ: không lộ tọa độ, lat/lng hoặc raw coordinates.");
        builder.AppendLine($"- Tên tour: {ValueOrFallback(tour.Name)}");
        builder.AppendLine($"- Mã tour: {ValueOrFallback(tour.Code)}");
        builder.AppendLine($"- Điểm đến: {BuildDestinationLabel(tour)}");
        builder.AppendLine($"- Thời lượng: {ValueOrFallback(tour.Duration?.Text)}");
        builder.AppendLine($"- Mô tả ngắn: {ValueOrFallback(RichTextContentFormatter.ToPlainTextSummary(tour.ShortDescription ?? tour.Description, 320))}");
        builder.AppendLine($"- Điểm nổi bật: {BuildListLine(tour.Highlights)}");
        builder.AppendLine($"- Bao gồm: {BuildListLine(tour.GeneratedInclusions)}");
        builder.AppendLine($"- Không bao gồm: {BuildListLine(tour.GeneratedExclusions)}");
        builder.AppendLine($"- Điểm hẹn: {ValueOrFallback(tour.MeetingPoint)}");
        builder.AppendLine($"- Chính sách hủy: {ValueOrFallback(tour.CancellationPolicy?.Summary)}");
        builder.AppendLine($"- Xác nhận: {ValueOrFallback(tour.ConfirmationType)}");
        builder.AppendLine($"- Giá người lớn mặc định: {FormatCurrency(tour.Price?.Adult)}");

        builder.AppendLine("- Tóm tắt lịch trình:");
        if (tour.Schedule is { Count: > 0 })
        {
            foreach (var item in tour.Schedule.OrderBy(entry => entry.Day).Take(8))
            {
                builder.AppendLine($"  * Ngày {item.Day:00}: {ValueOrFallback(item.Title)}. {ValueOrFallback(RichTextContentFormatter.ToPlainText(item.Description))}");
            }
        }
        else
        {
            builder.AppendLine("  * Chưa có dữ liệu lịch trình chi tiết.");
        }

        builder.AppendLine("- Lịch khởi hành còn chỗ:");
        if (departures.Count > 0)
        {
            builder.AppendLine($"  * Giá người lớn từ: {FormatCurrency(departures.Min(item => item.AdultPrice))}");
            foreach (var departure in departures)
            {
                builder.AppendLine(
                    $"  * {departure.StartDate:dd/MM/yyyy}: người lớn {FormatCurrency(departure.AdultPrice)}, trẻ em {FormatCurrency(departure.ChildPrice)}, em bé {FormatCurrency(departure.InfantPrice)}, còn {departure.RemainingCapacity} chỗ, trạng thái {ValueOrFallback(departure.Status)}, xác nhận {ValueOrFallback(departure.ConfirmationType)}");
            }
        }
        else
        {
            builder.AppendLine("  * Chưa có lịch khởi hành đang mở.");
        }

        if (routeInsight.HasRouting)
        {
            builder.AppendLine("- Tóm tắt RouteInsight ETA v2:");
            builder.AppendLine($"  * Tổng điểm dừng: {routeInsight.StopCount}");
            builder.AppendLine($"  * Tổng phút tham quan: {routeInsight.TotalVisitMinutes}");
            builder.AppendLine($"  * Tổng phút di chuyển: {routeInsight.TotalTravelMinutes}");
            builder.AppendLine($"  * Tổng phút hành trình: {routeInsight.TotalJourneyMinutes}");
            builder.AppendLine($"  * Tổng quãng đường km: {routeInsight.TotalDistanceKm:0.0}");

            foreach (var day in routeInsight.Days.Take(6))
            {
                var peakTraffic = !string.IsNullOrWhiteSpace(day.PeakDayPart)
                    ? $"{day.PeakDayPart}/{day.PeakCongestionLevel}"
                    : "ổn định";
                builder.AppendLine(
                    $"  * Ngày {day.Day:00}: {day.StopCount} điểm dừng, tham quan {day.VisitMinutes} phút, di chuyển {day.TravelMinutes} phút, hành trình {day.JourneyMinutes} phút, giao thông {peakTraffic}.");
            }

            if (routeInsight.Warnings.Count > 0)
            {
                builder.AppendLine("- Cảnh báo lộ trình:");
                foreach (var warning in routeInsight.Warnings.Take(4))
                {
                    builder.AppendLine($"  * {warning.Message}");
                }
            }
        }
        else
        {
            builder.AppendLine("- Tóm tắt RouteInsight ETA v2: tour này chưa có dữ liệu lộ trình có cấu trúc.");
        }

        builder.AppendLine("- Tour liên quan theo lộ trình:");
        if (relatedTours.Count > 0)
        {
            foreach (var related in relatedTours)
            {
                builder.AppendLine(
                    $"  * {related.Name}: {related.DestinationLabel}, {related.DurationText}, từ {FormatCurrency(related.StartingAdultPrice)}, đánh giá {related.Rating:0.0}, lộ trình {related.RouteSummary}, lý do {related.Reason}");
            }
        }
        else
        {
            builder.AppendLine("  * Chưa tìm thấy tour public liên quan trong ngữ cảnh lộ trình hiện tại.");
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
                "Tour này phù hợp với ai?",
                "Có ngày khởi hành nào còn chỗ không?",
                "Tour này có điểm nổi bật nào?",
                "Có tour liên quan nào để so sánh không?"
            ];
        }

        return
        [
            "Tour này di chuyển nhiều không?",
            "Ngày nào trong lịch trình có nhiều thời gian di chuyển nhất?",
            $"Tour này hợp với kiểu hành trình {routeStyleLabel} không?",
            "Có tour nào tương tự nhưng gọn hơn không?"
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
        var similarity = $"{BuildDestinationLabel(tour)} với thời lượng {ValueOrFallback(tour.Duration?.Text)}";
        return routeStyle switch
        {
            RouteRecommendationStyles.Compact => insight.HasRouting
                ? $"phù hợp kiểu gọn nhờ hồ sơ di chuyển ngắn hơn; {similarity}"
                : $"fallback kiểu gọn theo thời lượng và tình trạng còn chỗ; {similarity}",
            RouteRecommendationStyles.Highlights => insight.AverageAttractionScore.HasValue
                ? $"phù hợp kiểu điểm nổi bật nhờ điểm hấp dẫn {insight.AverageAttractionScore:0.0}; {similarity}"
                : $"fallback kiểu điểm nổi bật theo đánh giá và điểm đến; {similarity}",
            _ => $"phù hợp kiểu cân bằng theo lộ trình, giá, tình trạng còn chỗ và điểm đến; {similarity}"
        };
    }

    private static string GetRouteStyleLabel(string routeStyle)
    {
        return routeStyle switch
        {
            RouteRecommendationStyles.Compact => "gọn, ít di chuyển",
            RouteRecommendationStyles.Highlights => "ưu tiên điểm nổi bật",
            _ => "cân bằng"
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

        return parts.Count > 0 ? string.Join(", ", parts) : "Chưa có dữ liệu điểm đến";
    }

    private static string BuildListLine(IEnumerable<string>? values)
    {
        var items = values?.Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value.Trim()).ToList() ?? [];
        return items.Count > 0 ? string.Join("; ", items) : "Chưa có dữ liệu";
    }

    private static string ValueOrFallback(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "Chưa có dữ liệu" : value.Trim();
    }

    private static string FormatCurrency(decimal? value)
    {
        var amount = value.GetValueOrDefault();
        return amount > 0m ? $"{amount:N0} VND" : "Chưa có dữ liệu";
    }
}

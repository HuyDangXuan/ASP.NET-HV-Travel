using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using HVTravel.Application.Interfaces;
using HVTravel.Application.Models;
using HVTravel.Domain.Entities;

namespace HVTravel.Application.Services;

internal static partial class TourSearchDocumentMapper
{
    [GeneratedRegex(@"<[^>]+>", RegexOptions.Compiled)]
    private static partial Regex HtmlTagRegex();

    [GeneratedRegex(@"\s+", RegexOptions.Compiled)]
    private static partial Regex WhitespaceRegex();

    public static TourSearchDocument Map(Tour tour, IRouteInsightService routeInsightService)
    {
        var departures = tour.EffectiveDepartures.ToList();
        var nextDeparture = departures
            .Where(item => item.StartDate >= DateTime.UtcNow.Date)
            .OrderBy(item => item.StartDate)
            .Select(item => (DateTime?)item.StartDate)
            .FirstOrDefault()
            ?? departures.OrderBy(item => item.StartDate).Select(item => (DateTime?)item.StartDate).FirstOrDefault();

        var confirmationTypes = departures
            .Select(item => item.ConfirmationType)
            .Append(tour.ConfirmationType)
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var effectiveDiscount = GetEffectiveDiscount(tour);
        var hasPromotion = effectiveDiscount > 0m
            || (tour.BadgeSet?.Any(static badge => string.Equals(badge, "deal", StringComparison.OrdinalIgnoreCase)) ?? false);
        var country = tour.Destination?.Country?.Trim() ?? string.Empty;
        var isDomestic = IsVietnam(country);
        var insight = routeInsightService.Build(tour);

        return new TourSearchDocument
        {
            Id = tour.Id,
            Slug = tour.Slug ?? string.Empty,
            Name = NormalizeText(tour.Name),
            ShortDescriptionText = NormalizeText(tour.ShortDescription),
            DescriptionText = NormalizeText(tour.Description),
            Destination = NormalizeText(tour.Destination?.City),
            Region = NormalizeText(tour.Destination?.Region),
            Country = NormalizeText(country),
            HighlightsText = NormalizeText(string.Join(' ', tour.Highlights ?? [])),
            StartingAdultPrice = GetStartingAdultPrice(tour),
            DurationDays = Math.Max(0, tour.Duration?.Days ?? 0),
            Rating = tour.Rating,
            CreatedAt = tour.CreatedAt,
            NextDepartureDate = nextDeparture,
            DepartureMonths = departures
                .Select(item => item.StartDate.Month)
                .Distinct()
                .OrderBy(static value => value)
                .ToList(),
            MaxRemainingCapacity = departures.Count == 0
                ? Math.Max(0, tour.RemainingSpots)
                : departures.Max(static item => item.RemainingCapacity),
            ConfirmationTypes = confirmationTypes,
            IsFreeCancellation = tour.CancellationPolicy?.IsFreeCancellation == true,
            HasPromotion = hasPromotion,
            EffectiveDiscount = effectiveDiscount,
            Status = tour.Status ?? string.Empty,
            RoutingSummary = BuildRoutingSummary(insight),
            IsDomestic = isDomestic,
            IsInternational = !string.IsNullOrWhiteSpace(country) && !isDomestic,
            IsPremium = GetStartingAdultPrice(tour) >= 10000000m,
            IsBudget = GetStartingAdultPrice(tour) <= 3000000m,
            IsDeal = hasPromotion,
            CancellationType = tour.CancellationPolicy?.IsFreeCancellation == true ? "FreeCancellation" : "Strict"
        };
    }

    private static string BuildRoutingSummary(HVTravel.Application.Models.RouteInsightResult insight)
    {
        if (insight == null || !insight.HasRouting)
        {
            return string.Empty;
        }

        return string.Create(
            CultureInfo.InvariantCulture,
            $"{insight.StopCount} stops {insight.TotalTravelMinutes} travel minutes {insight.TotalDistanceKm:0.#} km");
    }

    private static decimal GetStartingAdultPrice(Tour tour)
    {
        var departurePrices = tour.EffectiveDepartures
            .Select(static item => item.AdultPrice)
            .Where(static value => value > 0m)
            .ToList();

        return departurePrices.Count != 0 ? departurePrices.Min() : tour.Price?.Adult ?? 0m;
    }

    private static decimal GetEffectiveDiscount(Tour tour)
    {
        var departureDiscounts = tour.EffectiveDepartures
            .Select(static item => item.DiscountPercentage)
            .ToList();

        if (departureDiscounts.Count != 0)
        {
            return departureDiscounts.Max();
        }

        return (decimal)(tour.Price?.Discount ?? 0d);
    }

    private static string NormalizeText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var withoutTags = HtmlTagRegex().Replace(value, " ");
        var normalized = withoutTags.Replace('\u00A0', ' ');
        return WhitespaceRegex().Replace(normalized, " ").Trim();
    }

    private static bool IsVietnam(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var normalized = RemoveDiacritics(value).Replace(" ", string.Empty, StringComparison.OrdinalIgnoreCase);
        return normalized.Equals("vietnam", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("vn", StringComparison.OrdinalIgnoreCase);
    }

    private static string RemoveDiacritics(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
        var builder = new System.Text.StringBuilder(normalized.Length);
        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}

using HVTravel.Application.Interfaces;
using HVTravel.Application.Models;
using HVTravel.Domain.Interfaces;
using HVTravel.Domain.Models;
using Microsoft.Extensions.Options;

namespace HVTravel.Application.Services;

public class MeilisearchTourSearchBackend : ITourSearchBackend
{
    private const int MaxRecommendationCandidateLimit = 1000;
    private static readonly IReadOnlyList<string> RequestedFacets =
    [
        "region",
        "destination",
        "confirmationTypes",
        "cancellationType"
    ];

    private readonly IMeilisearchTourIndexClient _client;
    private readonly ITourRepository _tourRepository;
    private readonly MeilisearchOptions _options;

    public MeilisearchTourSearchBackend(
        IMeilisearchTourIndexClient client,
        ITourRepository tourRepository,
        IOptions<MeilisearchOptions> options)
    {
        _client = client;
        _tourRepository = tourRepository;
        _options = options.Value ?? new MeilisearchOptions();
    }

    public string Name => "meili";

    public int Priority => 100;

    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        return _options.Enabled && await _client.IsHealthyAsync(cancellationToken);
    }

    public async Task<TourSearchResult> SearchAsync(TourSearchRequest request, CancellationToken cancellationToken = default)
    {
        request ??= new TourSearchRequest();

        var command = BuildCommand(request);
        var response = await _client.SearchAsync(command, cancellationToken);
        var items = response.Ids.Count == 0
            ? new List<HVTravel.Domain.Entities.Tour>()
            : (await _tourRepository.GetByIdsAsync(response.Ids)).ToList();

        var pageSize = Math.Max(1, request.PageSize);
        var totalItems = Math.Max(response.EstimatedTotalHits, items.Count);
        var totalPages = totalItems == 0 ? 0 : (int)Math.Ceiling(totalItems / (double)pageSize);
        var currentPage = Math.Max(1, Math.Min(request.Page, totalPages == 0 ? 1 : totalPages));

        return new TourSearchResult
        {
            Items = items,
            TotalItems = totalItems,
            TotalPages = totalPages,
            CurrentPage = currentPage,
            Regions = BuildFacetOptions(response.FacetDistribution, "region", request.Region),
            Destinations = BuildFacetOptions(response.FacetDistribution, "destination", request.Destination),
            ConfirmationTypes = BuildFacetOptions(response.FacetDistribution, "confirmationTypes", request.ConfirmationType),
            CancellationTypes = BuildFacetOptions(response.FacetDistribution, "cancellationType", request.CancellationType)
        };
    }

    private static MeilisearchTourSearchCommand BuildCommand(TourSearchRequest request)
    {
        var requestedPageSize = Math.Max(1, request.PageSize);
        var effectivePageSize = request.UseRecommendationRanking || requestedPageSize == int.MaxValue
            ? Math.Min(MaxRecommendationCandidateLimit, requestedPageSize == int.MaxValue ? MaxRecommendationCandidateLimit : requestedPageSize)
            : requestedPageSize;
        var currentPage = Math.Max(1, request.Page);

        return new MeilisearchTourSearchCommand
        {
            Query = request.Search?.Trim() ?? string.Empty,
            Limit = effectivePageSize,
            Offset = request.UseRecommendationRanking ? 0 : (currentPage - 1) * effectivePageSize,
            Filter = BuildFilter(request),
            Sort = BuildSort(request.Sort),
            Facets = RequestedFacets
        };
    }

    private static string BuildFilter(TourSearchRequest request)
    {
        var filters = new List<string>();

        if (request.PublicOnly)
        {
            filters.Add("status IN [\"Active\", \"ComingSoon\", \"SoldOut\"]");
        }

        if (!string.IsNullOrWhiteSpace(request.Region))
        {
            filters.Add($"region = {Quote(request.Region)}");
        }

        if (!string.IsNullOrWhiteSpace(request.Destination))
        {
            filters.Add($"destination = {Quote(request.Destination)}");
        }

        if (request.MinPrice.HasValue)
        {
            filters.Add($"startingAdultPrice >= {request.MinPrice.Value}");
        }

        if (request.MaxPrice.HasValue)
        {
            filters.Add($"startingAdultPrice <= {request.MaxPrice.Value}");
        }

        if (request.MaxDays.HasValue)
        {
            filters.Add($"durationDays <= {request.MaxDays.Value}");
        }

        if (request.DepartureMonth.HasValue)
        {
            filters.Add($"departureMonths = {request.DepartureMonth.Value}");
        }

        var requiredCapacity = request.Travellers > 0
            ? request.Travellers
            : request.AvailableOnly ? 1 : 0;
        if (requiredCapacity > 0)
        {
            filters.Add($"maxRemainingCapacity >= {requiredCapacity}");
        }

        if (!string.IsNullOrWhiteSpace(request.ConfirmationType))
        {
            filters.Add($"confirmationTypes = {Quote(request.ConfirmationType)}");
        }

        if (!string.IsNullOrWhiteSpace(request.CancellationType))
        {
            if (string.Equals(request.CancellationType, "FreeCancellation", StringComparison.OrdinalIgnoreCase))
            {
                filters.Add("isFreeCancellation = true");
            }
            else if (string.Equals(request.CancellationType, "Strict", StringComparison.OrdinalIgnoreCase))
            {
                filters.Add("isFreeCancellation = false");
            }
        }

        if (request.PromotionOnly)
        {
            filters.Add("hasPromotion = true");
        }

        if (!string.IsNullOrWhiteSpace(request.Collection))
        {
            var collectionFilter = request.Collection.Trim().ToLowerInvariant() switch
            {
                "domestic" => "isDomestic = true",
                "international" => "isInternational = true",
                "premium" => "isPremium = true",
                "budget" => "isBudget = true",
                "deal" => "isDeal = true",
                _ => string.Empty
            };

            if (!string.IsNullOrWhiteSpace(collectionFilter))
            {
                filters.Add(collectionFilter);
            }
        }

        return string.Join(" AND ", filters);
    }

    private static IReadOnlyList<string> BuildSort(string? sort)
    {
        return sort?.Trim().ToLowerInvariant() switch
        {
            "price_asc" => ["startingAdultPrice:asc"],
            "price_desc" => ["startingAdultPrice:desc"],
            "rating" => ["rating:desc"],
            "newest" => ["createdAt:desc"],
            "departure" => ["nextDepartureDate:asc"],
            "best_value" => ["effectiveDiscount:desc", "rating:desc", "startingAdultPrice:asc"],
            _ => Array.Empty<string>()
        };
    }

    private static IReadOnlyList<TourFacetOption> BuildFacetOptions(
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, int>> facets,
        string key,
        string? selected)
    {
        if (!facets.TryGetValue(key, out var values))
        {
            return Array.Empty<TourFacetOption>();
        }

        return values
            .Where(static pair => !string.IsNullOrWhiteSpace(pair.Key))
            .Select(pair => new TourFacetOption
            {
                Value = pair.Key,
                Count = pair.Value,
                Selected = string.Equals(pair.Key, selected, StringComparison.OrdinalIgnoreCase)
            })
            .OrderBy(static option => option.Value)
            .ToList();
    }

    private static string Quote(string value)
    {
        return $"\"{value.Trim().Replace("\"", "\\\"", StringComparison.Ordinal)}\"";
    }
}

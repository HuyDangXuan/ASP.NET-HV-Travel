using HVTravel.Application.Interfaces;
using HVTravel.Application.Models;
using HVTravel.Domain.Entities;
using HVTravel.Domain.Interfaces;
using HVTravel.Domain.Models;
using MongoDB.Bson;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HVTravel.Application.Services;

public class AdminTourSearchService : IAdminTourSearchService
{
    private readonly ITourRepository _tourRepository;
    private readonly IMeilisearchDocumentIndexClient _client;
    private readonly MeilisearchOptions _options;
    private readonly ILogger<AdminTourSearchService> _logger;

    public AdminTourSearchService(
        ITourRepository tourRepository,
        IMeilisearchDocumentIndexClient client,
        IOptions<MeilisearchOptions> options,
        ILogger<AdminTourSearchService> logger)
    {
        _tourRepository = tourRepository;
        _client = client;
        _options = options.Value ?? new MeilisearchOptions();
        _logger = logger;
    }

    public Task<PaginatedResult<Tour>> SearchAsync(AdminTourSearchRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedRequest = Normalize(request);

        return SearchFallbackExecutor.ExecuteAsync(
            isAvailableAsync: ct => IsMeilisearchAvailableAsync(ct),
            preferredAsync: ct => SearchWithMeilisearchAsync(normalizedRequest, ct),
            fallbackAsync: () => SearchWithRepositoryAsync(normalizedRequest),
            logger: _logger,
            scope: "AdminTours",
            cancellationToken: cancellationToken);
    }

    private Task<bool> IsMeilisearchAvailableAsync(CancellationToken cancellationToken)
    {
        return !_options.Enabled
            ? Task.FromResult(false)
            : _client.IsHealthyAsync(cancellationToken);
    }

    private async Task<PaginatedResult<Tour>> SearchWithMeilisearchAsync(AdminTourSearchRequest request, CancellationToken cancellationToken)
    {
        var filters = new List<string>();
        var targetStatus = ResolveTargetStatus(request.Status);
        if (targetStatus == null)
        {
            filters.Add("status != \"Deleted\"");
        }
        else
        {
            filters.Add($"status = {MeilisearchQueryHelpers.Quote(targetStatus)}");
        }

        var search = request.SearchString?.Trim() ?? string.Empty;
        if (ObjectId.TryParse(search, out _))
        {
            filters.Add($"id = {MeilisearchQueryHelpers.Quote(search)}");
        }

        var response = await _client.SearchAsync<TourSearchDocument>(
            MeilisearchIndexDefinitions.Tours(_options),
            new MeilisearchDocumentSearchCommand
            {
                Query = search,
                Limit = request.PageSize,
                Offset = (request.Page - 1) * request.PageSize,
                Filter = MeilisearchQueryHelpers.JoinAnd(filters),
                Sort = BuildSort(request.SortOrder)
            },
            cancellationToken);

        var items = response.Ids.Count == 0
            ? Array.Empty<Tour>()
            : await _tourRepository.GetByIdsAsync(response.Ids);

        return new PaginatedResult<Tour>(items, response.EstimatedTotalHits, request.Page, request.PageSize);
    }

    private async Task<PaginatedResult<Tour>> SearchWithRepositoryAsync(AdminTourSearchRequest request)
    {
        var statusLower = (request.Status ?? "all").ToLowerInvariant();
        var targetStatus = ResolveTargetStatus(statusLower);
        var search = !string.IsNullOrWhiteSpace(request.SearchString) ? request.SearchString.Trim().ToLowerInvariant() : null;
        var isObjectId = !string.IsNullOrEmpty(search) && ObjectId.TryParse(search, out _);

        System.Linq.Expressions.Expression<Func<Tour, bool>> filter;
        if (!string.IsNullOrEmpty(search))
        {
            filter = tour =>
                (targetStatus == null ? tour.Status != "Deleted" : tour.Status == targetStatus) &&
                (
                    tour.Name.ToLower().Contains(search) ||
                    (tour.Destination != null && tour.Destination.City != null && tour.Destination.City.ToLower().Contains(search)) ||
                    (isObjectId && tour.Id == search)
                );
        }
        else
        {
            filter = tour => targetStatus == null ? tour.Status != "Deleted" : tour.Status == targetStatus;
        }

        var tours = await _tourRepository.FindAsync(filter);
        var sorted = request.SortOrder switch
        {
            "name_desc" => tours.OrderByDescending(static tour => tour.Name),
            "price" => tours.OrderBy(static tour => tour.Price.Adult),
            "price_desc" => tours.OrderByDescending(static tour => tour.Price.Adult),
            "status" => tours.OrderBy(static tour => tour.Status),
            "status_desc" => tours.OrderByDescending(static tour => tour.Status),
            _ => tours.OrderBy(static tour => tour.Name)
        };

        var ordered = sorted.ToList();
        var items = ordered.Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).ToList();
        return new PaginatedResult<Tour>(items, ordered.Count, request.Page, request.PageSize);
    }

    private static AdminTourSearchRequest Normalize(AdminTourSearchRequest? request)
    {
        request ??= new AdminTourSearchRequest();
        if (request.PageSize < 5)
        {
            request.PageSize = 10;
        }
        else if (request.PageSize > 100)
        {
            request.PageSize = 100;
        }

        if (request.Page < 1)
        {
            request.Page = 1;
        }

        return request;
    }

    private static string? ResolveTargetStatus(string? status)
    {
        return status?.Trim().ToLowerInvariant() switch
        {
            "active" => "Active",
            "draft" => "Draft",
            "soldout" => "SoldOut",
            "comingsoon" => "ComingSoon",
            "hidden" => "Hidden",
            "deleted" => "Deleted",
            _ => null
        };
    }

    private static IReadOnlyList<string> BuildSort(string? sortOrder)
    {
        return sortOrder?.Trim().ToLowerInvariant() switch
        {
            "name_desc" => ["name:desc"],
            "price" => ["startingAdultPrice:asc"],
            "price_desc" => ["startingAdultPrice:desc"],
            "status" => ["status:asc"],
            "status_desc" => ["status:desc"],
            _ => ["name:asc"]
        };
    }
}

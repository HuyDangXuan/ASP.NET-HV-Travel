using HVTravel.Application.Interfaces;
using HVTravel.Application.Models;
using HVTravel.Domain.Entities;
using HVTravel.Domain.Interfaces;
using HVTravel.Domain.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HVTravel.Application.Services;

public class AdminReviewSearchService : IAdminReviewSearchService
{
    private readonly IRepository<Review> _reviewRepository;
    private readonly IMeilisearchDocumentIndexClient _client;
    private readonly MeilisearchOptions _options;
    private readonly ILogger<AdminReviewSearchService> _logger;

    public AdminReviewSearchService(
        IRepository<Review> reviewRepository,
        IMeilisearchDocumentIndexClient client,
        IOptions<MeilisearchOptions> options,
        ILogger<AdminReviewSearchService> logger)
    {
        _reviewRepository = reviewRepository;
        _client = client;
        _options = options.Value ?? new MeilisearchOptions();
        _logger = logger;
    }

    public Task<PaginatedResult<Review>> SearchAsync(AdminReviewSearchRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedRequest = Normalize(request);
        return SearchFallbackExecutor.ExecuteAsync(
            isAvailableAsync: ct => IsMeilisearchAvailableAsync(ct),
            preferredAsync: ct => SearchWithMeilisearchAsync(normalizedRequest, ct),
            fallbackAsync: () => SearchWithRepositoryAsync(normalizedRequest),
            logger: _logger,
            scope: "AdminReviews",
            cancellationToken: cancellationToken);
    }

    private Task<bool> IsMeilisearchAvailableAsync(CancellationToken cancellationToken)
    {
        return !_options.Enabled
            ? Task.FromResult(false)
            : _client.IsHealthyAsync(cancellationToken);
    }

    private async Task<PaginatedResult<Review>> SearchWithMeilisearchAsync(AdminReviewSearchRequest request, CancellationToken cancellationToken)
    {
        var filters = new List<string>();
        if (!string.Equals(request.Status, "all", StringComparison.OrdinalIgnoreCase))
        {
            filters.Add($"moderationStatus = {MeilisearchQueryHelpers.Quote(request.Status)}");
        }

        if (string.Equals(request.Verified, "verified", StringComparison.OrdinalIgnoreCase))
        {
            filters.Add("isVerifiedBooking = true");
        }
        else if (string.Equals(request.Verified, "unverified", StringComparison.OrdinalIgnoreCase))
        {
            filters.Add("isVerifiedBooking = false");
        }

        if (TryParseStartDate(request.StartDate, out var start))
        {
            filters.Add($"createdAt >= {MeilisearchQueryHelpers.Quote(start.ToString("O"))}");
        }

        if (TryParseEndDate(request.EndDate, out var end))
        {
            filters.Add($"createdAt <= {MeilisearchQueryHelpers.Quote(end.ToString("O"))}");
        }

        var response = await _client.SearchAsync<ReviewSearchDocument>(
            MeilisearchIndexDefinitions.Reviews(_options),
            new MeilisearchDocumentSearchCommand
            {
                Query = request.SearchString?.Trim() ?? string.Empty,
                Limit = request.PageSize,
                Offset = (request.Page - 1) * request.PageSize,
                Filter = MeilisearchQueryHelpers.JoinAnd(filters),
                Sort = BuildSort(request.SortOrder)
            },
            cancellationToken);

        var items = response.Ids.Count == 0
            ? Array.Empty<Review>()
            : await _reviewRepository.GetByIdsAsync(response.Ids);

        return new PaginatedResult<Review>(items, response.EstimatedTotalHits, request.Page, request.PageSize);
    }

    private async Task<PaginatedResult<Review>> SearchWithRepositoryAsync(AdminReviewSearchRequest request)
    {
        var reviews = (await _reviewRepository.GetAllAsync()).AsEnumerable();
        var normalizedSearch = string.IsNullOrWhiteSpace(request.SearchString) ? null : request.SearchString.Trim().ToLowerInvariant();
        DateTime? start = null;
        DateTime? end = null;

        if (TryParseStartDate(request.StartDate, out var parsedStart))
        {
            start = parsedStart;
        }

        if (TryParseEndDate(request.EndDate, out var parsedEnd))
        {
            end = parsedEnd;
        }

        reviews = reviews.Where(review =>
            MatchesModerationStatus(review, request.Status) &&
            MatchesVerificationState(review, request.Verified) &&
            MatchesSearch(review, normalizedSearch) &&
            (start == null || review.CreatedAt >= start.Value) &&
            (end == null || review.CreatedAt <= end.Value));

        var sorted = request.SortOrder switch
        {
            "date_asc" => reviews.OrderBy(static review => review.CreatedAt),
            "rating" => reviews.OrderBy(static review => review.Rating).ThenByDescending(static review => review.CreatedAt),
            "rating_desc" => reviews.OrderByDescending(static review => review.Rating).ThenByDescending(static review => review.CreatedAt),
            "status" => reviews.OrderBy(review => GetModerationStatusRank(review.ModerationStatus)).ThenByDescending(static review => review.CreatedAt),
            "status_desc" => reviews.OrderByDescending(review => GetModerationStatusRank(review.ModerationStatus)).ThenByDescending(static review => review.CreatedAt),
            _ => reviews.OrderByDescending(static review => review.CreatedAt)
        };

        var ordered = sorted.ToList();
        var items = ordered.Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).ToList();
        return new PaginatedResult<Review>(items, ordered.Count, request.Page, request.PageSize);
    }

    private static AdminReviewSearchRequest Normalize(AdminReviewSearchRequest? request)
    {
        request ??= new AdminReviewSearchRequest();
        if (request.Page < 1)
        {
            request.Page = 1;
        }

        if (request.PageSize <= 0)
        {
            request.PageSize = 10;
        }

        return request;
    }

    private static IReadOnlyList<string> BuildSort(string? sortOrder)
    {
        return sortOrder?.Trim().ToLowerInvariant() switch
        {
            "date_asc" => ["createdAt:asc"],
            "rating" => ["rating:asc", "createdAt:desc"],
            "rating_desc" => ["rating:desc", "createdAt:desc"],
            "status" => ["moderationStatusRank:asc", "createdAt:desc"],
            "status_desc" => ["moderationStatusRank:desc", "createdAt:desc"],
            _ => ["createdAt:desc"]
        };
    }

    private static bool MatchesModerationStatus(Review review, string? status)
    {
        return status?.Trim().ToLowerInvariant() switch
        {
            "pending" => string.Equals(review.ModerationStatus, "Pending", StringComparison.OrdinalIgnoreCase),
            "approved" => string.Equals(review.ModerationStatus, "Approved", StringComparison.OrdinalIgnoreCase),
            "rejected" => string.Equals(review.ModerationStatus, "Rejected", StringComparison.OrdinalIgnoreCase),
            _ => true
        };
    }

    private static bool MatchesVerificationState(Review review, string? verified)
    {
        return verified?.Trim().ToLowerInvariant() switch
        {
            "verified" => review.IsVerifiedBooking,
            "unverified" => !review.IsVerifiedBooking,
            _ => true
        };
    }

    private static bool MatchesSearch(Review review, string? normalizedSearch)
    {
        if (string.IsNullOrWhiteSpace(normalizedSearch))
        {
            return true;
        }

        return (review.Id?.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) ?? false) ||
               (review.BookingId?.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) ?? false) ||
               (review.DisplayName?.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) ?? false) ||
               (review.Comment?.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) ?? false) ||
               (review.ModeratorName?.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) ?? false);
    }

    private static int GetModerationStatusRank(string? moderationStatus)
    {
        return moderationStatus?.ToLowerInvariant() switch
        {
            "pending" => 0,
            "approved" => 1,
            "rejected" => 2,
            _ => 99
        };
    }

    private static bool TryParseStartDate(string? value, out DateTime date)
    {
        if (DateTime.TryParse(value, out var parsed))
        {
            date = parsed.Date;
            return true;
        }

        date = default;
        return false;
    }

    private static bool TryParseEndDate(string? value, out DateTime date)
    {
        if (DateTime.TryParse(value, out var parsed))
        {
            date = parsed.Date.AddDays(1).AddTicks(-1);
            return true;
        }

        date = default;
        return false;
    }
}

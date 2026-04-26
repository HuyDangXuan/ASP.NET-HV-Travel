using HVTravel.Application.Interfaces;
using HVTravel.Application.Models;
using HVTravel.Domain.Entities;
using HVTravel.Domain.Interfaces;
using HVTravel.Domain.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HVTravel.Application.Services;

public class AdminUserSearchService : IAdminUserSearchService
{
    private readonly IRepository<User> _userRepository;
    private readonly IMeilisearchDocumentIndexClient _client;
    private readonly MeilisearchOptions _options;
    private readonly ILogger<AdminUserSearchService> _logger;

    public AdminUserSearchService(
        IRepository<User> userRepository,
        IMeilisearchDocumentIndexClient client,
        IOptions<MeilisearchOptions> options,
        ILogger<AdminUserSearchService> logger)
    {
        _userRepository = userRepository;
        _client = client;
        _options = options.Value ?? new MeilisearchOptions();
        _logger = logger;
    }

    public async Task<AdminUserSearchResult> SearchAsync(AdminUserSearchRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedRequest = Normalize(request);
        var page = await SearchFallbackExecutor.ExecuteAsync(
            isAvailableAsync: ct => IsMeilisearchAvailableAsync(ct),
            preferredAsync: ct => SearchWithMeilisearchAsync(normalizedRequest, ct),
            fallbackAsync: () => SearchWithRepositoryAsync(normalizedRequest),
            logger: _logger,
            scope: "AdminUsers",
            cancellationToken: cancellationToken);

        var allUsers = (await _userRepository.GetAllAsync()).ToList();
        return new AdminUserSearchResult
        {
            Users = page.Items.ToList(),
            CurrentPage = page.PageIndex,
            TotalPages = page.TotalPages,
            PageSize = page.PageSize,
            TotalCount = page.TotalCount,
            TotalUsers = allUsers.Count,
            ActiveCount = allUsers.Count(static user => user.Status == "Active"),
            InactiveCount = allUsers.Count(static user => user.Status == "Inactive"),
            RoleCounts = allUsers
                .GroupBy(user => user.Role ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase)
        };
    }

    private Task<bool> IsMeilisearchAvailableAsync(CancellationToken cancellationToken)
    {
        return !_options.Enabled
            ? Task.FromResult(false)
            : _client.IsHealthyAsync(cancellationToken);
    }

    private async Task<PaginatedResult<User>> SearchWithMeilisearchAsync(AdminUserSearchRequest request, CancellationToken cancellationToken)
    {
        var filters = new List<string>();
        if (string.Equals(request.Status, "active", StringComparison.OrdinalIgnoreCase))
        {
            filters.Add("status = \"Active\"");
        }
        else if (string.Equals(request.Status, "inactive", StringComparison.OrdinalIgnoreCase))
        {
            filters.Add("status = \"Inactive\"");
        }

        if (!string.IsNullOrWhiteSpace(request.RoleFilter))
        {
            filters.Add($"role = {MeilisearchQueryHelpers.Quote(request.RoleFilter)}");
        }

        var response = await _client.SearchAsync<UserSearchDocument>(
            MeilisearchIndexDefinitions.Users(_options),
            new MeilisearchDocumentSearchCommand
            {
                Query = request.SearchQuery?.Trim() ?? string.Empty,
                Limit = request.PageSize,
                Offset = (request.Page - 1) * request.PageSize,
                Filter = MeilisearchQueryHelpers.JoinAnd(filters),
                Sort = BuildSort(request.SortOrder)
            },
            cancellationToken);

        var items = response.Ids.Count == 0
            ? Array.Empty<User>()
            : await _userRepository.GetByIdsAsync(response.Ids);

        return new PaginatedResult<User>(items, response.EstimatedTotalHits, request.Page, request.PageSize);
    }

    private async Task<PaginatedResult<User>> SearchWithRepositoryAsync(AdminUserSearchRequest request)
    {
        var users = (await _userRepository.GetAllAsync()).AsEnumerable();

        if (string.Equals(request.Status, "active", StringComparison.OrdinalIgnoreCase))
        {
            users = users.Where(static user => user.Status == "Active");
        }
        else if (string.Equals(request.Status, "inactive", StringComparison.OrdinalIgnoreCase))
        {
            users = users.Where(static user => user.Status == "Inactive");
        }

        if (!string.IsNullOrWhiteSpace(request.SearchQuery))
        {
            var search = request.SearchQuery.Trim();
            users = users.Where(user =>
                user.FullName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                user.Email.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(request.RoleFilter))
        {
            users = users.Where(user => string.Equals(user.Role, request.RoleFilter, StringComparison.OrdinalIgnoreCase));
        }

        var sorted = request.SortOrder switch
        {
            "account_desc" => users.OrderByDescending(static user => user.FullName),
            "email" => users.OrderBy(static user => user.Email),
            "email_desc" => users.OrderByDescending(static user => user.Email),
            "role" => users.OrderBy(static user => user.Role),
            "role_desc" => users.OrderByDescending(static user => user.Role),
            "status" => users.OrderBy(static user => user.Status),
            "status_desc" => users.OrderByDescending(static user => user.Status),
            "date" => users.OrderBy(static user => user.LastLogin ?? DateTime.MinValue),
            "date_desc" => users.OrderByDescending(static user => user.LastLogin ?? DateTime.MinValue),
            _ => users.OrderBy(static user => user.FullName)
        };

        var ordered = sorted.ToList();
        var items = ordered.Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).ToList();
        return new PaginatedResult<User>(items, ordered.Count, request.Page, request.PageSize);
    }

    private static AdminUserSearchRequest Normalize(AdminUserSearchRequest? request)
    {
        request ??= new AdminUserSearchRequest();
        if (request.Page < 1)
        {
            request.Page = 1;
        }

        if (request.PageSize < 1)
        {
            request.PageSize = 10;
        }

        return request;
    }

    private static IReadOnlyList<string> BuildSort(string? sortOrder)
    {
        return sortOrder?.Trim().ToLowerInvariant() switch
        {
            "account_desc" => ["fullName:desc"],
            "email" => ["email:asc"],
            "email_desc" => ["email:desc"],
            "role" => ["role:asc"],
            "role_desc" => ["role:desc"],
            "status" => ["status:asc"],
            "status_desc" => ["status:desc"],
            "date" => ["lastLogin:asc"],
            "date_desc" => ["lastLogin:desc"],
            _ => ["fullName:asc"]
        };
    }
}

using HVTravel.Application.Interfaces;
using HVTravel.Application.Models;
using HVTravel.Domain.Entities;
using HVTravel.Domain.Interfaces;
using HVTravel.Domain.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HVTravel.Application.Services;

public class AdminCustomerSearchService : IAdminCustomerSearchService
{
    private static readonly string[] SegmentKeys = ["VIP", "New", "Standard", "ChurnRisk"];

    private readonly IRepository<Customer> _customerRepository;
    private readonly IRepository<Booking> _bookingRepository;
    private readonly IMeilisearchDocumentIndexClient _client;
    private readonly MeilisearchOptions _options;
    private readonly ILogger<AdminCustomerSearchService> _logger;

    public AdminCustomerSearchService(
        IRepository<Customer> customerRepository,
        IRepository<Booking> bookingRepository,
        IMeilisearchDocumentIndexClient client,
        IOptions<MeilisearchOptions> options,
        ILogger<AdminCustomerSearchService> logger)
    {
        _customerRepository = customerRepository;
        _bookingRepository = bookingRepository;
        _client = client;
        _options = options.Value ?? new MeilisearchOptions();
        _logger = logger;
    }

    public Task<AdminCustomerSearchResult> SearchAsync(AdminCustomerSearchRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedRequest = Normalize(request);
        return SearchFallbackExecutor.ExecuteAsync(
            isAvailableAsync: ct => IsMeilisearchAvailableAsync(ct),
            preferredAsync: ct => SearchWithMeilisearchAsync(normalizedRequest, ct),
            fallbackAsync: () => SearchWithRepositoryAsync(normalizedRequest),
            logger: _logger,
            scope: "AdminCustomers",
            cancellationToken: cancellationToken);
    }

    private Task<bool> IsMeilisearchAvailableAsync(CancellationToken cancellationToken)
    {
        return !_options.Enabled
            ? Task.FromResult(false)
            : _client.IsHealthyAsync(cancellationToken);
    }

    private async Task<AdminCustomerSearchResult> SearchWithMeilisearchAsync(AdminCustomerSearchRequest request, CancellationToken cancellationToken)
    {
        var filters = new List<string>();
        if (request.Segments.Length != 0)
        {
            filters.Add($"segment IN [{string.Join(", ", request.Segments.Select(MeilisearchQueryHelpers.Quote))}]");
        }

        if (request.MinSpending.HasValue)
        {
            filters.Add($"totalSpending >= {request.MinSpending.Value}");
        }

        if (request.MinOrders.HasValue)
        {
            filters.Add($"totalOrders >= {request.MinOrders.Value}");
        }

        var response = await _client.SearchAsync<CustomerSearchDocument>(
            MeilisearchIndexDefinitions.Customers(_options),
            new MeilisearchDocumentSearchCommand
            {
                Query = request.SearchQuery?.Trim() ?? string.Empty,
                Limit = request.PageSize,
                Offset = (request.Page - 1) * request.PageSize,
                Filter = MeilisearchQueryHelpers.JoinAnd(filters),
                Sort = BuildSort(request.SortOrder),
                Facets = ["segment"]
            },
            cancellationToken);

        var customers = response.Ids.Count == 0
            ? Array.Empty<Customer>()
            : await _customerRepository.GetByIdsAsync(response.Ids);
        var documentLookup = response.Documents.ToDictionary(document => document.Id, StringComparer.Ordinal);
        foreach (var customer in customers)
        {
            if (documentLookup.TryGetValue(customer.Id, out var document))
            {
                customer.Stats ??= new CustomerStats();
                customer.Stats.TotalOrders = document.TotalOrders;
                customer.Stats.TotalSpending = document.TotalSpending;
            }
        }

        return new AdminCustomerSearchResult
        {
            Page = new PaginatedResult<Customer>(customers, response.EstimatedTotalHits, request.Page, request.PageSize),
            TotalCustomers = response.EstimatedTotalHits,
            NewCustomersCount = ReadSegmentCount(response.FacetDistribution, "New"),
            SegmentPercentages = BuildSegmentPercentages(response.FacetDistribution, response.EstimatedTotalHits)
        };
    }

    private async Task<AdminCustomerSearchResult> SearchWithRepositoryAsync(AdminCustomerSearchRequest request)
    {
        var customers = (await _customerRepository.GetAllAsync()).ToList();
        var allBookings = await _bookingRepository.GetAllAsync();
        var customerStats = new Dictionary<string, CustomerSearchAggregate>(StringComparer.Ordinal);

        foreach (var bookingGroup in allBookings.Where(static booking => !string.IsNullOrWhiteSpace(booking.CustomerId)).GroupBy(booking => booking.CustomerId))
        {
            customerStats[bookingGroup.Key] = SearchDocumentMapper.BuildCustomerAggregate(bookingGroup);
        }

        foreach (var customer in customers)
        {
            customer.Stats ??= new CustomerStats();
            if (customerStats.TryGetValue(customer.Id, out var aggregate))
            {
                customer.Stats.TotalOrders = aggregate.TotalOrders;
                customer.Stats.TotalSpending = aggregate.TotalSpending;
            }
            else
            {
                customer.Stats.TotalOrders = 0;
                customer.Stats.TotalSpending = 0m;
            }
        }

        IEnumerable<Customer> query = customers;
        if (!string.IsNullOrWhiteSpace(request.SearchQuery))
        {
            var search = request.SearchQuery.Trim();
            query = query.Where(customer =>
                customer.FullName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                customer.Email.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                customer.PhoneNumber.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        if (request.Segments.Length != 0)
        {
            query = query.Where(customer => request.Segments.Contains(customer.Segment));
        }

        if (request.MinSpending.HasValue)
        {
            query = query.Where(customer => customer.Stats.TotalSpending >= request.MinSpending.Value);
        }

        if (request.MinOrders.HasValue)
        {
            query = query.Where(customer => customer.Stats.TotalOrders >= request.MinOrders.Value);
        }

        var sorted = request.SortOrder switch
        {
            "name_desc" => query.OrderByDescending(static customer => customer.FullName),
            "spending" => query.OrderBy(static customer => customer.Stats.TotalSpending),
            "spending_desc" => query.OrderByDescending(static customer => customer.Stats.TotalSpending),
            "orders" => query.OrderBy(static customer => customer.Stats.TotalOrders),
            "orders_desc" => query.OrderByDescending(static customer => customer.Stats.TotalOrders),
            _ => query.OrderBy(static customer => customer.FullName)
        };

        var ordered = sorted.ToList();
        var items = ordered.Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).ToList();
        return new AdminCustomerSearchResult
        {
            Page = new PaginatedResult<Customer>(items, ordered.Count, request.Page, request.PageSize),
            TotalCustomers = ordered.Count,
            NewCustomersCount = ordered.Count(static customer => customer.Segment == "New"),
            SegmentPercentages = CalculateSegmentPercentages(ordered)
        };
    }

    private static AdminCustomerSearchRequest Normalize(AdminCustomerSearchRequest? request)
    {
        request ??= new AdminCustomerSearchRequest();
        if (request.Page < 1)
        {
            request.Page = 1;
        }

        if (request.PageSize < 1)
        {
            request.PageSize = 10;
        }

        request.Segments = (request.Segments ?? Array.Empty<string>())
            .Where(static segment => !string.IsNullOrWhiteSpace(segment))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return request;
    }

    private static IReadOnlyList<string> BuildSort(string? sortOrder)
    {
        return sortOrder?.Trim().ToLowerInvariant() switch
        {
            "name_desc" => ["fullName:desc"],
            "spending" => ["totalSpending:asc"],
            "spending_desc" => ["totalSpending:desc"],
            "orders" => ["totalOrders:asc"],
            "orders_desc" => ["totalOrders:desc"],
            _ => ["fullName:asc"]
        };
    }

    private static IReadOnlyDictionary<string, double> BuildSegmentPercentages(
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, int>> facets,
        int totalCount)
    {
        var output = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        foreach (var key in SegmentKeys)
        {
            var count = ReadSegmentCount(facets, key);
            if (string.Equals(key, "ChurnRisk", StringComparison.OrdinalIgnoreCase))
            {
                count += ReadSegmentCount(facets, "Nguy Cơ Rời Bỏ");
            }

            output[key] = totalCount == 0 ? 0d : Math.Round(count / (double)totalCount * 100d, 1);
        }

        return output;
    }

    private static int ReadSegmentCount(
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, int>> facets,
        string segment)
    {
        if (!facets.TryGetValue("segment", out var values))
        {
            return 0;
        }

        return values.TryGetValue(segment, out var count) ? count : 0;
    }

    private static IReadOnlyDictionary<string, double> CalculateSegmentPercentages(IEnumerable<Customer> customers)
    {
        var list = customers.ToList();
        var total = list.Count;
        var output = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        foreach (var key in SegmentKeys)
        {
            var count = list.Count(customer => string.Equals(customer.Segment, key, StringComparison.OrdinalIgnoreCase));
            if (string.Equals(key, "ChurnRisk", StringComparison.OrdinalIgnoreCase))
            {
                count += list.Count(customer => string.Equals(customer.Segment, "Nguy Cơ Rời Bỏ", StringComparison.OrdinalIgnoreCase));
            }

            output[key] = total == 0 ? 0d : Math.Round(count / (double)total * 100d, 1);
        }

        return output;
    }
}

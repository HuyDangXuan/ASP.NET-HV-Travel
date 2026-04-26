using HVTravel.Application.Interfaces;
using HVTravel.Application.Models;
using HVTravel.Domain.Entities;
using HVTravel.Domain.Interfaces;
using HVTravel.Domain.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HVTravel.Application.Services;

public class AdminPaymentSearchService : IAdminPaymentSearchService
{
    private readonly IRepository<Booking> _bookingRepository;
    private readonly IMeilisearchDocumentIndexClient _client;
    private readonly MeilisearchOptions _options;
    private readonly ILogger<AdminPaymentSearchService> _logger;

    public AdminPaymentSearchService(
        IRepository<Booking> bookingRepository,
        IMeilisearchDocumentIndexClient client,
        IOptions<MeilisearchOptions> options,
        ILogger<AdminPaymentSearchService> logger)
    {
        _bookingRepository = bookingRepository;
        _client = client;
        _options = options.Value ?? new MeilisearchOptions();
        _logger = logger;
    }

    public async Task<AdminPaymentSearchResult> SearchAsync(AdminPaymentSearchRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedRequest = Normalize(request);
        var page = await SearchFallbackExecutor.ExecuteAsync(
            isAvailableAsync: ct => IsMeilisearchAvailableAsync(ct),
            preferredAsync: ct => SearchWithMeilisearchAsync(normalizedRequest, ct),
            fallbackAsync: () => SearchWithRepositoryAsync(normalizedRequest),
            logger: _logger,
            scope: "AdminPayments",
            cancellationToken: cancellationToken);

        var allBookings = (await _bookingRepository.GetAllAsync()).ToList();
        var filteredBookings = ApplyFilters(allBookings, normalizedRequest).ToList();

        return new AdminPaymentSearchResult
        {
            Page = page,
            TotalRevenue = allBookings
                .Where(static booking => booking.Status == "Completed" && booking.PaymentStatus == "Full")
                .Sum(static booking => booking.TotalAmount),
            TotalRefunded = allBookings
                .Where(static booking => booking.Status == "Refunded" && booking.PaymentStatus == "Refunded")
                .Sum(static booking => booking.TotalAmount),
            FilteredBookingsCount = filteredBookings.Count,
            SuccessfulPaymentsCount = filteredBookings.Count(static booking =>
                booking.PaymentStatus == "Full" ||
                booking.PaymentStatus == "Success" ||
                booking.PaymentStatus == "Paid"),
            RefundBookings = filteredBookings
                .Where(static booking => booking.Status == "Refunded" && booking.PaymentStatus == "Refunded")
                .ToList()
        };
    }

    private Task<bool> IsMeilisearchAvailableAsync(CancellationToken cancellationToken)
    {
        return !_options.Enabled
            ? Task.FromResult(false)
            : _client.IsHealthyAsync(cancellationToken);
    }

    private async Task<PaginatedResult<Booking>> SearchWithMeilisearchAsync(AdminPaymentSearchRequest request, CancellationToken cancellationToken)
    {
        var filters = new List<string>();
        if (!string.IsNullOrWhiteSpace(request.PaymentStatusFilter))
        {
            filters.Add($"paymentStatus = {MeilisearchQueryHelpers.Quote(request.PaymentStatusFilter)}");
        }

        if (!string.IsNullOrWhiteSpace(request.BookingStatusFilter))
        {
            filters.Add($"bookingStatus = {MeilisearchQueryHelpers.Quote(request.BookingStatusFilter)}");
        }

        var response = await _client.SearchAsync<PaymentAdminSearchDocument>(
            MeilisearchIndexDefinitions.Payments(_options),
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
            ? Array.Empty<Booking>()
            : await _bookingRepository.GetByIdsAsync(response.Ids);

        return new PaginatedResult<Booking>(items, response.EstimatedTotalHits, request.Page, request.PageSize);
    }

    private async Task<PaginatedResult<Booking>> SearchWithRepositoryAsync(AdminPaymentSearchRequest request)
    {
        var allBookings = (await _bookingRepository.GetAllAsync()).ToList();
        var sorted = ApplySort(ApplyFilters(allBookings, request), request.SortOrder).ToList();
        var items = sorted.Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).ToList();
        return new PaginatedResult<Booking>(items, sorted.Count, request.Page, request.PageSize);
    }

    private static IEnumerable<Booking> ApplyFilters(IEnumerable<Booking> bookings, AdminPaymentSearchRequest request)
    {
        var filtered = bookings;
        var normalizedSearch = request.SearchString?.Trim();
        if (!string.IsNullOrWhiteSpace(normalizedSearch))
        {
            filtered = filtered.Where(booking =>
                (!string.IsNullOrWhiteSpace(booking.Id) && booking.Id.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrWhiteSpace(booking.BookingCode) && booking.BookingCode.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase)));
        }

        if (!string.IsNullOrWhiteSpace(request.PaymentStatusFilter))
        {
            filtered = filtered.Where(booking => booking.PaymentStatus == request.PaymentStatusFilter);
        }

        if (!string.IsNullOrWhiteSpace(request.BookingStatusFilter))
        {
            filtered = filtered.Where(booking => booking.Status == request.BookingStatusFilter);
        }

        return filtered;
    }

    private static IOrderedEnumerable<Booking> ApplySort(IEnumerable<Booking> bookings, string? sortOrder)
    {
        return sortOrder?.Trim().ToLowerInvariant() switch
        {
            "id_desc" => bookings.OrderByDescending(static booking => booking.Id),
            "date" => bookings.OrderBy(static booking => booking.BookingDate),
            "date_desc" => bookings.OrderByDescending(static booking => booking.BookingDate),
            "booking" => bookings.OrderBy(static booking => booking.BookingCode),
            "booking_desc" => bookings.OrderByDescending(static booking => booking.BookingCode),
            "status" => bookings.OrderBy(static booking => booking.PaymentStatus),
            "status_desc" => bookings.OrderByDescending(static booking => booking.PaymentStatus),
            "booking_status" => bookings.OrderBy(static booking => booking.Status),
            "booking_status_desc" => bookings.OrderByDescending(static booking => booking.Status),
            "amount" => bookings.OrderBy(static booking => booking.TotalAmount),
            "amount_desc" => bookings.OrderByDescending(static booking => booking.TotalAmount),
            _ => bookings.OrderByDescending(static booking => booking.BookingDate)
        };
    }

    private static AdminPaymentSearchRequest Normalize(AdminPaymentSearchRequest? request)
    {
        request ??= new AdminPaymentSearchRequest();
        if (request.Page < 1)
        {
            request.Page = 1;
        }

        if (request.PageSize < 5)
        {
            request.PageSize = 10;
        }
        else if (request.PageSize > 100)
        {
            request.PageSize = 100;
        }

        return request;
    }

    private static IReadOnlyList<string> BuildSort(string? sortOrder)
    {
        return sortOrder?.Trim().ToLowerInvariant() switch
        {
            "id_desc" => ["id:desc"],
            "date" => ["createdAt:asc"],
            "date_desc" => ["createdAt:desc"],
            "booking" => ["bookingCode:asc"],
            "booking_desc" => ["bookingCode:desc"],
            "status" => ["paymentStatus:asc"],
            "status_desc" => ["paymentStatus:desc"],
            "booking_status" => ["bookingStatus:asc"],
            "booking_status_desc" => ["bookingStatus:desc"],
            "amount" => ["amount:asc"],
            "amount_desc" => ["amount:desc"],
            _ => ["createdAt:desc"]
        };
    }
}

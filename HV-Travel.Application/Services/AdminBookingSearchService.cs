using System.Linq.Expressions;
using HVTravel.Application.Interfaces;
using HVTravel.Application.Models;
using HVTravel.Domain.Entities;
using HVTravel.Domain.Interfaces;
using HVTravel.Domain.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HVTravel.Application.Services;

public class AdminBookingSearchService : IAdminBookingSearchService
{
    private readonly IRepository<Booking> _bookingRepository;
    private readonly IMeilisearchDocumentIndexClient _client;
    private readonly MeilisearchOptions _options;
    private readonly ILogger<AdminBookingSearchService> _logger;

    public AdminBookingSearchService(
        IRepository<Booking> bookingRepository,
        IMeilisearchDocumentIndexClient client,
        IOptions<MeilisearchOptions> options,
        ILogger<AdminBookingSearchService> logger)
    {
        _bookingRepository = bookingRepository;
        _client = client;
        _options = options.Value ?? new MeilisearchOptions();
        _logger = logger;
    }

    public async Task<AdminBookingSearchResult> SearchAsync(AdminBookingSearchRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedRequest = Normalize(request);
        var page = await SearchFallbackExecutor.ExecuteAsync(
            isAvailableAsync: ct => IsMeilisearchAvailableAsync(ct),
            preferredAsync: ct => SearchWithMeilisearchAsync(normalizedRequest, ct),
            fallbackAsync: () => SearchWithRepositoryAsync(normalizedRequest),
            logger: _logger,
            scope: "AdminBookings",
            cancellationToken: cancellationToken);

        var today = DateTime.UtcNow.Date;
        var todayBookings = await _bookingRepository.FindAsync(booking => booking.CreatedAt >= today);
        var pendingPayments = await _bookingRepository.FindAsync(booking => booking.PaymentStatus == "Unpaid" || booking.Status == "Pending");
        var refundRequests = await _bookingRepository.FindAsync(booking => booking.Status == "Cancelled" || booking.PaymentStatus == "Refunded");

        return new AdminBookingSearchResult
        {
            Page = page,
            TodayBookingsCount = todayBookings.Count(),
            PendingPaymentCount = pendingPayments.Count(),
            PendingPaymentTotal = pendingPayments.Sum(static booking => booking.TotalAmount),
            RefundRequestCount = refundRequests.Count()
        };
    }

    private Task<bool> IsMeilisearchAvailableAsync(CancellationToken cancellationToken)
    {
        return !_options.Enabled
            ? Task.FromResult(false)
            : _client.IsHealthyAsync(cancellationToken);
    }

    private async Task<PaginatedResult<Booking>> SearchWithMeilisearchAsync(AdminBookingSearchRequest request, CancellationToken cancellationToken)
    {
        var filters = new List<string>();
        var targetStatus = ResolveTargetStatus(request.Status);
        if (string.Equals(targetStatus, "Deleted", StringComparison.OrdinalIgnoreCase))
        {
            filters.Add("isDeleted = true");
        }
        else
        {
            filters.Add("isDeleted = false");
        }

        if (!string.IsNullOrWhiteSpace(targetStatus) && !string.Equals(targetStatus, "Deleted", StringComparison.OrdinalIgnoreCase))
        {
            filters.Add($"(bookingStatus = {MeilisearchQueryHelpers.Quote(targetStatus)} OR paymentStatus = {MeilisearchQueryHelpers.Quote(targetStatus)})");
        }

        if (TryParseStartDate(request.StartDate, out var start))
        {
            filters.Add($"createdAt >= {MeilisearchQueryHelpers.Quote(start.ToString("O"))}");
        }

        if (TryParseEndDate(request.EndDate, out var end))
        {
            filters.Add($"createdAt <= {MeilisearchQueryHelpers.Quote(end.ToString("O"))}");
        }

        var response = await _client.SearchAsync<BookingSearchDocument>(
            MeilisearchIndexDefinitions.Bookings(_options),
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

    private async Task<PaginatedResult<Booking>> SearchWithRepositoryAsync(AdminBookingSearchRequest request)
    {
        var targetStatus = ResolveTargetStatus(request.Status);
        var search = !string.IsNullOrEmpty(request.SearchString) ? request.SearchString.Trim().ToLowerInvariant() : null;
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

        Expression<Func<Booking, bool>> filter = booking =>
            (targetStatus == "Deleted" ? booking.IsDeleted : !booking.IsDeleted) &&
            (targetStatus == null || targetStatus == "Deleted" || booking.Status == targetStatus || booking.PaymentStatus == targetStatus) &&
            (search == null ||
                (booking.BookingCode != null && booking.BookingCode.ToLower().Contains(search)) ||
                (booking.ContactInfo != null &&
                    ((booking.ContactInfo.Name != null && booking.ContactInfo.Name.ToLower().Contains(search)) ||
                     (booking.ContactInfo.Email != null && booking.ContactInfo.Email.ToLower().Contains(search))))) &&
            (start == null || booking.CreatedAt >= start) &&
            (end == null || booking.CreatedAt <= end);

        var bookings = await _bookingRepository.FindAsync(filter);
        var sorted = request.SortOrder switch
        {
            "date_asc" => bookings.OrderBy(static booking => booking.CreatedAt),
            "total" => bookings.OrderBy(static booking => booking.TotalAmount),
            "total_desc" => bookings.OrderByDescending(static booking => booking.TotalAmount),
            "status" => bookings.OrderBy(static booking => booking.Status),
            "status_desc" => bookings.OrderByDescending(static booking => booking.Status),
            _ => bookings.OrderByDescending(static booking => booking.CreatedAt)
        };

        var ordered = sorted.ToList();
        var items = ordered.Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).ToList();
        return new PaginatedResult<Booking>(items, ordered.Count, request.Page, request.PageSize);
    }

    private static AdminBookingSearchRequest Normalize(AdminBookingSearchRequest? request)
    {
        request ??= new AdminBookingSearchRequest();
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

    private static string? ResolveTargetStatus(string? status)
    {
        return status?.Trim().ToLowerInvariant() switch
        {
            "paid" => "Paid",
            "pending" => "Pending",
            "cancelled" => "Cancelled",
            "confirmed" => "Confirmed",
            "completed" => "Completed",
            "refunded" => "Refunded",
            "deleted" => "Deleted",
            _ => null
        };
    }

    private static IReadOnlyList<string> BuildSort(string? sortOrder)
    {
        return sortOrder?.Trim().ToLowerInvariant() switch
        {
            "date_asc" => ["createdAt:asc"],
            "total" => ["totalAmount:asc"],
            "total_desc" => ["totalAmount:desc"],
            "status" => ["bookingStatus:asc"],
            "status_desc" => ["bookingStatus:desc"],
            _ => ["createdAt:desc"]
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

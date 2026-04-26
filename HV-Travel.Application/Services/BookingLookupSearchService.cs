using HVTravel.Application.Interfaces;
using HVTravel.Application.Models;
using HVTravel.Domain.Entities;
using HVTravel.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HVTravel.Application.Services;

public class BookingLookupSearchService : IBookingLookupSearchService
{
    private readonly IRepository<Booking> _bookingRepository;
    private readonly IMeilisearchDocumentIndexClient _client;
    private readonly MeilisearchOptions _options;
    private readonly ILogger<BookingLookupSearchService> _logger;

    public BookingLookupSearchService(
        IRepository<Booking> bookingRepository,
        IMeilisearchDocumentIndexClient client,
        IOptions<MeilisearchOptions> options,
        ILogger<BookingLookupSearchService> logger)
    {
        _bookingRepository = bookingRepository;
        _client = client;
        _options = options.Value ?? new MeilisearchOptions();
        _logger = logger;
    }

    public Task<Booking?> LookupAsync(BookingLookupSearchRequest request, CancellationToken cancellationToken = default)
    {
        request ??= new BookingLookupSearchRequest();

        return SearchFallbackExecutor.ExecuteAsync(
            isAvailableAsync: ct => IsMeilisearchAvailableAsync(ct),
            preferredAsync: ct => LookupWithMeilisearchAsync(request, ct),
            fallbackAsync: () => LookupWithRepositoryAsync(request),
            logger: _logger,
            scope: "BookingLookup",
            cancellationToken: cancellationToken);
    }

    private Task<bool> IsMeilisearchAvailableAsync(CancellationToken cancellationToken)
    {
        return !_options.Enabled
            ? Task.FromResult(false)
            : _client.IsHealthyAsync(cancellationToken);
    }

    private async Task<Booking?> LookupWithMeilisearchAsync(BookingLookupSearchRequest request, CancellationToken cancellationToken)
    {
        var bookingCode = request.BookingCode?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(bookingCode))
        {
            return null;
        }

        var filters = new List<string>
        {
            $"bookingCode = {MeilisearchQueryHelpers.Quote(bookingCode)}",
            "publicLookupEnabled = true",
            "isDeleted = false"
        };

        var contactFilters = new List<string>();
        var email = request.Email?.Trim() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(email))
        {
            contactFilters.Add($"contactEmail = {MeilisearchQueryHelpers.Quote(email)}");
        }

        var normalizedPhone = MeilisearchQueryHelpers.NormalizePhone(request.Phone);
        if (!string.IsNullOrWhiteSpace(normalizedPhone))
        {
            contactFilters.Add($"contactPhoneNormalized = {MeilisearchQueryHelpers.Quote(normalizedPhone)}");
        }

        if (contactFilters.Count != 0)
        {
            filters.Add($"({MeilisearchQueryHelpers.JoinOr(contactFilters)})");
        }

        var response = await _client.SearchAsync<BookingSearchDocument>(
            MeilisearchIndexDefinitions.Bookings(_options),
            new MeilisearchDocumentSearchCommand
            {
                Query = string.Empty,
                Limit = 5,
                Filter = MeilisearchQueryHelpers.JoinAnd(filters)
            },
            cancellationToken);

        if (response.Ids.Count == 0)
        {
            return null;
        }

        var bookings = await _bookingRepository.GetByIdsAsync(response.Ids);
        return bookings.FirstOrDefault();
    }

    private async Task<Booking?> LookupWithRepositoryAsync(BookingLookupSearchRequest request)
    {
        var bookingCode = request.BookingCode?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(bookingCode))
        {
            return null;
        }

        var normalizedPhone = MeilisearchQueryHelpers.NormalizePhone(request.Phone);
        var email = request.Email?.Trim() ?? string.Empty;
        return (await _bookingRepository.FindAsync(b => b.BookingCode == bookingCode))
            .Where(static booking => booking.PublicLookupEnabled && !booking.IsDeleted)
            .FirstOrDefault(candidate =>
                (!string.IsNullOrWhiteSpace(email) && string.Equals(candidate.ContactInfo?.Email?.Trim(), email, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrWhiteSpace(normalizedPhone) && MeilisearchQueryHelpers.NormalizePhone(candidate.ContactInfo?.Phone) == normalizedPhone));
    }
}

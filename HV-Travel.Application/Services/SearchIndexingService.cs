using HVTravel.Application.Interfaces;
using HVTravel.Application.Models;
using HVTravel.Domain.Entities;
using HVTravel.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HVTravel.Application.Services;

public class SearchIndexingService : ITourSearchIndexingService, ISearchIndexingService
{
    private readonly ITourRepository _tourRepository;
    private readonly IRepository<Booking> _bookingRepository;
    private readonly IRepository<User> _userRepository;
    private readonly IRepository<Review> _reviewRepository;
    private readonly IRepository<AncillaryLead> _serviceLeadRepository;
    private readonly IRepository<Customer> _customerRepository;
    private readonly IMeilisearchDocumentIndexClient _client;
    private readonly IRouteInsightService _routeInsightService;
    private readonly MeilisearchOptions _options;
    private readonly ILogger<SearchIndexingService> _logger;

    public SearchIndexingService(
        ITourRepository tourRepository,
        IRepository<Booking> bookingRepository,
        IRepository<User> userRepository,
        IRepository<Review> reviewRepository,
        IRepository<AncillaryLead> serviceLeadRepository,
        IRepository<Customer> customerRepository,
        IMeilisearchDocumentIndexClient client,
        IRouteInsightService routeInsightService,
        IOptions<MeilisearchOptions> options,
        ILogger<SearchIndexingService> logger)
    {
        _tourRepository = tourRepository;
        _bookingRepository = bookingRepository;
        _userRepository = userRepository;
        _reviewRepository = reviewRepository;
        _serviceLeadRepository = serviceLeadRepository;
        _customerRepository = customerRepository;
        _client = client;
        _routeInsightService = routeInsightService;
        _options = options.Value ?? new MeilisearchOptions();
        _logger = logger;
    }

    public async Task UpsertTourAsync(Tour? tour, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled || tour == null || string.IsNullOrWhiteSpace(tour.Id))
        {
            return;
        }

        try
        {
            await _client.UpsertDocumentsAsync(
                MeilisearchIndexDefinitions.Tours(_options),
                [TourSearchDocumentMapper.Map(tour, _routeInsightService)],
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to upsert Meilisearch tour document for {TourId}.", tour.Id);
        }
    }

    public async Task DeleteTourAsync(string? tourId, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled || string.IsNullOrWhiteSpace(tourId))
        {
            return;
        }

        try
        {
            await _client.DeleteDocumentsAsync(MeilisearchIndexDefinitions.Tours(_options), [tourId], cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete Meilisearch tour document for {TourId}.", tourId);
        }
    }

    public async Task UpsertBookingAsync(Booking? booking, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled || booking == null || string.IsNullOrWhiteSpace(booking.Id))
        {
            return;
        }

        try
        {
            await _client.UpsertDocumentsAsync(
                MeilisearchIndexDefinitions.Bookings(_options),
                [SearchDocumentMapper.MapBooking(booking)],
                cancellationToken);

            var paymentDocument = await BuildPaymentDocumentAsync(booking);
            await _client.UpsertDocumentsAsync(
                MeilisearchIndexDefinitions.Payments(_options),
                [paymentDocument],
                cancellationToken);

            if (!string.IsNullOrWhiteSpace(booking.CustomerId))
            {
                await UpsertCustomerByIdAsync(booking.CustomerId, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to upsert Meilisearch booking document for {BookingId}.", booking.Id);
        }
    }

    public async Task DeleteBookingAsync(string? bookingId, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled || string.IsNullOrWhiteSpace(bookingId))
        {
            return;
        }

        string? customerId = null;
        try
        {
            var booking = await _bookingRepository.GetByIdAsync(bookingId);
            customerId = booking?.CustomerId;
        }
        catch
        {
            customerId = null;
        }

        try
        {
            await _client.DeleteDocumentsAsync(MeilisearchIndexDefinitions.Bookings(_options), [bookingId], cancellationToken);
            await _client.DeleteDocumentsAsync(MeilisearchIndexDefinitions.Payments(_options), [bookingId], cancellationToken);
            if (!string.IsNullOrWhiteSpace(customerId))
            {
                await UpsertCustomerByIdAsync(customerId, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete Meilisearch booking/payment documents for {BookingId}.", bookingId);
        }
    }

    public async Task UpsertUserAsync(User? user, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled || user == null || string.IsNullOrWhiteSpace(user.Id))
        {
            return;
        }

        try
        {
            await _client.UpsertDocumentsAsync(
                MeilisearchIndexDefinitions.Users(_options),
                [SearchDocumentMapper.MapUser(user)],
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to upsert Meilisearch user document for {UserId}.", user.Id);
        }
    }

    public async Task DeleteUserAsync(string? userId, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled || string.IsNullOrWhiteSpace(userId))
        {
            return;
        }

        try
        {
            await _client.DeleteDocumentsAsync(MeilisearchIndexDefinitions.Users(_options), [userId], cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete Meilisearch user document for {UserId}.", userId);
        }
    }

    public async Task UpsertReviewAsync(Review? review, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled || review == null || string.IsNullOrWhiteSpace(review.Id))
        {
            return;
        }

        try
        {
            var document = await BuildReviewDocumentAsync(review);
            await _client.UpsertDocumentsAsync(MeilisearchIndexDefinitions.Reviews(_options), [document], cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to upsert Meilisearch review document for {ReviewId}.", review.Id);
        }
    }

    public async Task DeleteReviewAsync(string? reviewId, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled || string.IsNullOrWhiteSpace(reviewId))
        {
            return;
        }

        try
        {
            await _client.DeleteDocumentsAsync(MeilisearchIndexDefinitions.Reviews(_options), [reviewId], cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete Meilisearch review document for {ReviewId}.", reviewId);
        }
    }

    public async Task UpsertServiceLeadAsync(AncillaryLead? lead, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled || lead == null || string.IsNullOrWhiteSpace(lead.Id))
        {
            return;
        }

        try
        {
            await _client.UpsertDocumentsAsync(
                MeilisearchIndexDefinitions.ServiceLeads(_options),
                [SearchDocumentMapper.MapServiceLead(lead)],
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to upsert Meilisearch service lead document for {LeadId}.", lead.Id);
        }
    }

    public async Task DeleteServiceLeadAsync(string? leadId, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled || string.IsNullOrWhiteSpace(leadId))
        {
            return;
        }

        try
        {
            await _client.DeleteDocumentsAsync(MeilisearchIndexDefinitions.ServiceLeads(_options), [leadId], cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete Meilisearch service lead document for {LeadId}.", leadId);
        }
    }

    public async Task UpsertCustomerAsync(Customer? customer, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled || customer == null || string.IsNullOrWhiteSpace(customer.Id))
        {
            return;
        }

        try
        {
            var document = await BuildCustomerDocumentAsync(customer, cancellationToken);
            await _client.UpsertDocumentsAsync(MeilisearchIndexDefinitions.Customers(_options), [document], cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to upsert Meilisearch customer document for {CustomerId}.", customer.Id);
        }
    }

    public async Task DeleteCustomerAsync(string? customerId, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled || string.IsNullOrWhiteSpace(customerId))
        {
            return;
        }

        try
        {
            await _client.DeleteDocumentsAsync(MeilisearchIndexDefinitions.Customers(_options), [customerId], cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete Meilisearch customer document for {CustomerId}.", customerId);
        }
    }

    public async Task RebuildAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return;
        }

        try
        {
            var tours = (await _tourRepository.GetAllAsync())
                .Where(static item => !string.IsNullOrWhiteSpace(item.Id))
                .Select(item => TourSearchDocumentMapper.Map(item, _routeInsightService))
                .ToList();
            await _client.ReplaceAllDocumentsAsync(MeilisearchIndexDefinitions.Tours(_options), tours, cancellationToken);

            var bookings = (await _bookingRepository.GetAllAsync())
                .Where(static item => !string.IsNullOrWhiteSpace(item.Id))
                .ToList();
            await _client.ReplaceAllDocumentsAsync(
                MeilisearchIndexDefinitions.Bookings(_options),
                bookings.Select(SearchDocumentMapper.MapBooking).ToList(),
                cancellationToken);

            var users = (await _userRepository.GetAllAsync())
                .Where(static item => !string.IsNullOrWhiteSpace(item.Id))
                .Select(SearchDocumentMapper.MapUser)
                .ToList();
            await _client.ReplaceAllDocumentsAsync(MeilisearchIndexDefinitions.Users(_options), users, cancellationToken);

            var toursById = (await _tourRepository.GetAllAsync())
                .Where(static item => !string.IsNullOrWhiteSpace(item.Id))
                .ToDictionary(item => item.Id, StringComparer.Ordinal);
            var customersById = (await _customerRepository.GetAllAsync())
                .Where(static item => !string.IsNullOrWhiteSpace(item.Id))
                .ToDictionary(item => item.Id, StringComparer.Ordinal);

            var reviews = (await _reviewRepository.GetAllAsync())
                .Where(static item => !string.IsNullOrWhiteSpace(item.Id))
                .Select(review =>
                {
                    toursById.TryGetValue(review.TourId ?? string.Empty, out var tour);
                    customersById.TryGetValue(review.CustomerId ?? string.Empty, out var customer);
                    return SearchDocumentMapper.MapReview(review, tour?.Name, customer?.Email);
                })
                .ToList();
            await _client.ReplaceAllDocumentsAsync(MeilisearchIndexDefinitions.Reviews(_options), reviews, cancellationToken);

            var serviceLeads = (await _serviceLeadRepository.GetAllAsync())
                .Where(static item => !string.IsNullOrWhiteSpace(item.Id))
                .Select(SearchDocumentMapper.MapServiceLead)
                .ToList();
            await _client.ReplaceAllDocumentsAsync(MeilisearchIndexDefinitions.ServiceLeads(_options), serviceLeads, cancellationToken);

            var customerDocuments = customersById.Values
                .Select(customer =>
                {
                    var aggregate = SearchDocumentMapper.BuildCustomerAggregate(
                        bookings.Where(booking => string.Equals(booking.CustomerId, customer.Id, StringComparison.Ordinal)));
                    return SearchDocumentMapper.MapCustomer(customer, aggregate);
                })
                .ToList();
            await _client.ReplaceAllDocumentsAsync(MeilisearchIndexDefinitions.Customers(_options), customerDocuments, cancellationToken);

            var paymentDocuments = bookings
                .Select(booking =>
                {
                    customersById.TryGetValue(booking.CustomerId ?? string.Empty, out var customer);
                    return SearchDocumentMapper.MapPaymentAdmin(booking, customer?.FullName);
                })
                .ToList();
            await _client.ReplaceAllDocumentsAsync(MeilisearchIndexDefinitions.Payments(_options), paymentDocuments, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to rebuild Meilisearch indexes. Repository fallback remains active.");
        }
    }

    private async Task UpsertCustomerByIdAsync(string customerId, CancellationToken cancellationToken)
    {
        var customer = await _customerRepository.GetByIdAsync(customerId);
        if (customer == null)
        {
            await DeleteCustomerAsync(customerId, cancellationToken);
            return;
        }

        var document = await BuildCustomerDocumentAsync(customer, cancellationToken);
        await _client.UpsertDocumentsAsync(MeilisearchIndexDefinitions.Customers(_options), [document], cancellationToken);
    }

    private async Task<CustomerSearchDocument> BuildCustomerDocumentAsync(Customer customer, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var bookings = await _bookingRepository.FindAsync(booking => booking.CustomerId == customer.Id);
        var aggregate = SearchDocumentMapper.BuildCustomerAggregate(bookings);
        return SearchDocumentMapper.MapCustomer(customer, aggregate);
    }

    private async Task<ReviewSearchDocument> BuildReviewDocumentAsync(Review review)
    {
        var tour = string.IsNullOrWhiteSpace(review.TourId) ? null : await _tourRepository.GetByIdAsync(review.TourId);
        var customer = string.IsNullOrWhiteSpace(review.CustomerId) ? null : await _customerRepository.GetByIdAsync(review.CustomerId);
        return SearchDocumentMapper.MapReview(review, tour?.Name, customer?.Email);
    }

    private async Task<PaymentAdminSearchDocument> BuildPaymentDocumentAsync(Booking booking)
    {
        var customer = string.IsNullOrWhiteSpace(booking.CustomerId) ? null : await _customerRepository.GetByIdAsync(booking.CustomerId);
        return SearchDocumentMapper.MapPaymentAdmin(booking, customer?.FullName);
    }
}

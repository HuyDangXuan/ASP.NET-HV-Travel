using HVTravel.Application.Models;
using HVTravel.Domain.Entities;
using HVTravel.Domain.Models;

namespace HVTravel.Application.Interfaces;

public interface IMeilisearchDocumentIndexClient
{
    Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);

    Task EnsureConfiguredAsync(MeilisearchIndexDefinition definition, CancellationToken cancellationToken = default);

    Task<MeilisearchDocumentSearchResponse<TDocument>> SearchAsync<TDocument>(
        MeilisearchIndexDefinition definition,
        MeilisearchDocumentSearchCommand command,
        CancellationToken cancellationToken = default)
        where TDocument : class;

    Task UpsertDocumentsAsync<TDocument>(
        MeilisearchIndexDefinition definition,
        IReadOnlyCollection<TDocument> documents,
        CancellationToken cancellationToken = default)
        where TDocument : class;

    Task DeleteDocumentsAsync(
        MeilisearchIndexDefinition definition,
        IReadOnlyCollection<string> ids,
        CancellationToken cancellationToken = default);

    Task ReplaceAllDocumentsAsync<TDocument>(
        MeilisearchIndexDefinition definition,
        IReadOnlyCollection<TDocument> documents,
        CancellationToken cancellationToken = default)
        where TDocument : class;
}

public interface IBookingLookupSearchService
{
    Task<Booking?> LookupAsync(BookingLookupSearchRequest request, CancellationToken cancellationToken = default);
}

public interface IAdminTourSearchService
{
    Task<PaginatedResult<Tour>> SearchAsync(AdminTourSearchRequest request, CancellationToken cancellationToken = default);
}

public interface IAdminBookingSearchService
{
    Task<AdminBookingSearchResult> SearchAsync(AdminBookingSearchRequest request, CancellationToken cancellationToken = default);
}

public interface IAdminUserSearchService
{
    Task<AdminUserSearchResult> SearchAsync(AdminUserSearchRequest request, CancellationToken cancellationToken = default);
}

public interface IAdminReviewSearchService
{
    Task<PaginatedResult<Review>> SearchAsync(AdminReviewSearchRequest request, CancellationToken cancellationToken = default);
}

public interface IAdminServiceLeadSearchService
{
    Task<IReadOnlyList<AncillaryLead>> SearchAsync(AdminServiceLeadSearchRequest request, CancellationToken cancellationToken = default);
}

public interface IAdminCustomerSearchService
{
    Task<AdminCustomerSearchResult> SearchAsync(AdminCustomerSearchRequest request, CancellationToken cancellationToken = default);
}

public interface IAdminPaymentSearchService
{
    Task<AdminPaymentSearchResult> SearchAsync(AdminPaymentSearchRequest request, CancellationToken cancellationToken = default);
}

public interface ISearchIndexingService
{
    Task UpsertTourAsync(Tour? tour, CancellationToken cancellationToken = default);

    Task DeleteTourAsync(string? tourId, CancellationToken cancellationToken = default);

    Task UpsertBookingAsync(Booking? booking, CancellationToken cancellationToken = default);

    Task DeleteBookingAsync(string? bookingId, CancellationToken cancellationToken = default);

    Task UpsertUserAsync(User? user, CancellationToken cancellationToken = default);

    Task DeleteUserAsync(string? userId, CancellationToken cancellationToken = default);

    Task UpsertReviewAsync(Review? review, CancellationToken cancellationToken = default);

    Task DeleteReviewAsync(string? reviewId, CancellationToken cancellationToken = default);

    Task UpsertServiceLeadAsync(AncillaryLead? lead, CancellationToken cancellationToken = default);

    Task DeleteServiceLeadAsync(string? leadId, CancellationToken cancellationToken = default);

    Task UpsertCustomerAsync(Customer? customer, CancellationToken cancellationToken = default);

    Task DeleteCustomerAsync(string? customerId, CancellationToken cancellationToken = default);

    Task RebuildAsync(CancellationToken cancellationToken = default);
}

namespace HVTravel.Application.Models;

public class MeilisearchOptions
{
    public bool Enabled { get; set; }

    public string Url { get; set; } = "http://localhost:7700";

    public string ApiKey { get; set; } = string.Empty;

    public string IndexName { get; set; } = "hv_travel_tours";

    public bool BootstrapOnStartup { get; set; }

    public MeilisearchIndexOptions Indexes { get; set; } = new();

    public string ResolveToursIndexName()
    {
        return !string.IsNullOrWhiteSpace(Indexes.Tours) ? Indexes.Tours : IndexName;
    }
}

public class MeilisearchIndexOptions
{
    public string Tours { get; set; } = "hv_travel_tours";

    public string Bookings { get; set; } = "hv_travel_bookings";

    public string Users { get; set; } = "hv_travel_users";

    public string Reviews { get; set; } = "hv_travel_reviews";

    public string ServiceLeads { get; set; } = "hv_travel_service_leads";

    public string Customers { get; set; } = "hv_travel_customers";

    public string Payments { get; set; } = "hv_travel_payments";
}

public sealed class MeilisearchIndexDefinition
{
    public required string Name { get; init; }

    public IReadOnlyList<string> SearchableAttributes { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> FilterableAttributes { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> SortableAttributes { get; init; } = Array.Empty<string>();
}

public class MeilisearchDocumentSearchCommand
{
    public string Query { get; set; } = string.Empty;

    public int Limit { get; set; } = 20;

    public int Offset { get; set; }

    public string Filter { get; set; } = string.Empty;

    public IReadOnlyList<string> Sort { get; set; } = Array.Empty<string>();

    public IReadOnlyList<string> Facets { get; set; } = Array.Empty<string>();
}

public class MeilisearchDocumentSearchResponse<TDocument>
{
    public IReadOnlyList<TDocument> Documents { get; set; } = Array.Empty<TDocument>();

    public IReadOnlyList<string> Ids { get; set; } = Array.Empty<string>();

    public int EstimatedTotalHits { get; set; }

    public IReadOnlyDictionary<string, IReadOnlyDictionary<string, int>> FacetDistribution { get; set; }
        = new Dictionary<string, IReadOnlyDictionary<string, int>>(StringComparer.OrdinalIgnoreCase);
}

public class MeilisearchTourSearchCommand
{
    public string Query { get; set; } = string.Empty;

    public int Limit { get; set; } = 20;

    public int Offset { get; set; }

    public string Filter { get; set; } = string.Empty;

    public IReadOnlyList<string> Sort { get; set; } = Array.Empty<string>();

    public IReadOnlyList<string> Facets { get; set; } = Array.Empty<string>();
}

public class MeilisearchTourSearchResponse
{
    public IReadOnlyList<string> Ids { get; set; } = Array.Empty<string>();

    public int EstimatedTotalHits { get; set; }

    public IReadOnlyDictionary<string, IReadOnlyDictionary<string, int>> FacetDistribution { get; set; }
        = new Dictionary<string, IReadOnlyDictionary<string, int>>(StringComparer.OrdinalIgnoreCase);
}

public class TourSearchDocument
{
    public string Id { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string ShortDescriptionText { get; set; } = string.Empty;

    public string DescriptionText { get; set; } = string.Empty;

    public string Destination { get; set; } = string.Empty;

    public string Region { get; set; } = string.Empty;

    public string Country { get; set; } = string.Empty;

    public string HighlightsText { get; set; } = string.Empty;

    public decimal StartingAdultPrice { get; set; }

    public int DurationDays { get; set; }

    public double Rating { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? NextDepartureDate { get; set; }

    public IReadOnlyList<int> DepartureMonths { get; set; } = Array.Empty<int>();

    public int MaxRemainingCapacity { get; set; }

    public IReadOnlyList<string> ConfirmationTypes { get; set; } = Array.Empty<string>();

    public bool IsFreeCancellation { get; set; }

    public bool HasPromotion { get; set; }

    public decimal EffectiveDiscount { get; set; }

    public string Status { get; set; } = string.Empty;

    public string RoutingSummary { get; set; } = string.Empty;

    public bool IsDomestic { get; set; }

    public bool IsInternational { get; set; }

    public bool IsPremium { get; set; }

    public bool IsBudget { get; set; }

    public bool IsDeal { get; set; }

    public string CancellationType { get; set; } = "Strict";
}

public class BookingSearchDocument
{
    public string Id { get; set; } = string.Empty;

    public string BookingCode { get; set; } = string.Empty;

    public string ContactName { get; set; } = string.Empty;

    public string ContactEmail { get; set; } = string.Empty;

    public string ContactPhone { get; set; } = string.Empty;

    public string ContactPhoneNormalized { get; set; } = string.Empty;

    public string TourName { get; set; } = string.Empty;

    public string BookingStatus { get; set; } = string.Empty;

    public string PaymentStatus { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime? DepartureDate { get; set; }

    public decimal TotalAmount { get; set; }

    public bool IsDeleted { get; set; }

    public bool PublicLookupEnabled { get; set; }
}

public class UserSearchDocument
{
    public string Id { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTime? LastLogin { get; set; }

    public DateTime CreatedAt { get; set; }
}

public class ReviewSearchDocument
{
    public string Id { get; set; } = string.Empty;

    public string TourId { get; set; } = string.Empty;

    public string TourName { get; set; } = string.Empty;

    public string CustomerId { get; set; } = string.Empty;

    public string CustomerEmail { get; set; } = string.Empty;

    public string BookingId { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string Comment { get; set; } = string.Empty;

    public string ModeratorName { get; set; } = string.Empty;

    public string ModerationStatus { get; set; } = string.Empty;

    public int ModerationStatusRank { get; set; }

    public bool IsVerifiedBooking { get; set; }

    public DateTime CreatedAt { get; set; }

    public int Rating { get; set; }
}

public class ServiceLeadSearchDocument
{
    public string Id { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;

    public string Destination { get; set; } = string.Empty;

    public string ServiceType { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}

public class CustomerSearchDocument
{
    public string Id { get; set; } = string.Empty;

    public string CustomerCode { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;

    public string Segment { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public int TotalOrders { get; set; }

    public decimal TotalSpending { get; set; }

    public DateTime? LastBookingDate { get; set; }

    public DateTime CreatedAt { get; set; }
}

public class PaymentAdminSearchDocument
{
    public string Id { get; set; } = string.Empty;

    public string BookingCode { get; set; } = string.Empty;

    public string CustomerId { get; set; } = string.Empty;

    public string CustomerName { get; set; } = string.Empty;

    public string PaymentStatus { get; set; } = string.Empty;

    public string BookingStatus { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? PaidAt { get; set; }
}

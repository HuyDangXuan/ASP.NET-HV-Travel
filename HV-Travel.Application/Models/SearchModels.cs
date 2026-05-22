using HVTravel.Domain.Entities;
using HVTravel.Domain.Models;

namespace HVTravel.Application.Models;

public class BookingLookupSearchRequest
{
    public string BookingCode { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;
}

public class AdminTourSearchRequest
{
    public string Status { get; set; } = "all";

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 10;

    public string SearchString { get; set; } = string.Empty;

    public string SortOrder { get; set; } = string.Empty;
}

public class AdminBookingSearchRequest
{
    public string Status { get; set; } = "all";

    public string SearchString { get; set; } = string.Empty;

    public string StartDate { get; set; } = string.Empty;

    public string EndDate { get; set; } = string.Empty;

    public string SortOrder { get; set; } = string.Empty;

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 10;
}

public class AdminBookingSearchResult
{
    public PaginatedResult<Booking> Page { get; set; } = new(Array.Empty<Booking>(), 0, 1, 10);

    public int TodayBookingsCount { get; set; }

    public int PendingPaymentCount { get; set; }

    public decimal PendingPaymentTotal { get; set; }

    public int RefundRequestCount { get; set; }
}

public class AdminUserSearchRequest
{
    public string Status { get; set; } = "all";

    public string SearchQuery { get; set; } = string.Empty;

    public string RoleFilter { get; set; } = string.Empty;

    public string SortOrder { get; set; } = string.Empty;

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 10;
}

public class AdminUserSearchResult
{
    public IReadOnlyList<User> Users { get; set; } = Array.Empty<User>();

    public int CurrentPage { get; set; } = 1;

    public int TotalPages { get; set; }

    public int PageSize { get; set; } = 10;

    public int TotalCount { get; set; }

    public int TotalUsers { get; set; }

    public int ActiveCount { get; set; }

    public int InactiveCount { get; set; }

    public IReadOnlyDictionary<string, int> RoleCounts { get; set; }
        = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
}

public class AdminReviewSearchRequest
{
    public string Status { get; set; } = "all";

    public string Verified { get; set; } = "all";

    public string SearchString { get; set; } = string.Empty;

    public string StartDate { get; set; } = string.Empty;

    public string EndDate { get; set; } = string.Empty;

    public string SortOrder { get; set; } = string.Empty;

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 10;
}

public class AdminServiceLeadSearchRequest
{
    public string Status { get; set; } = string.Empty;

    public string ServiceType { get; set; } = string.Empty;

    public string Search { get; set; } = string.Empty;
}

public class AdminCustomerSearchRequest
{
    public string SearchQuery { get; set; } = string.Empty;

    public string[] Segments { get; set; } = Array.Empty<string>();

    public decimal? MinSpending { get; set; }

    public int? MinOrders { get; set; }

    public string SortOrder { get; set; } = string.Empty;

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 10;
}

public class AdminCustomerSearchResult
{
    public PaginatedResult<Customer> Page { get; set; } = new(Array.Empty<Customer>(), 0, 1, 10);

    public int TotalCustomers { get; set; }

    public int NewCustomersCount { get; set; }

    public IReadOnlyDictionary<string, double> SegmentPercentages { get; set; }
        = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
}

public class AdminPaymentSearchRequest
{
    public string SortOrder { get; set; } = string.Empty;

    public string BookingStatusFilter { get; set; } = string.Empty;

    public string PaymentStatusFilter { get; set; } = string.Empty;

    public string SearchString { get; set; } = string.Empty;

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 10;
}

public class AdminPaymentSearchResult
{
    public PaginatedResult<Booking> Page { get; set; } = new(Array.Empty<Booking>(), 0, 1, 10);

    public decimal TotalRevenue { get; set; }

    public decimal TotalRefunded { get; set; }

    public int FilteredBookingsCount { get; set; }

    public int SuccessfulPaymentsCount { get; set; }

    public IReadOnlyList<Booking> RefundBookings { get; set; } = Array.Empty<Booking>();
}

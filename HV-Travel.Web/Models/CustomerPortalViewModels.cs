using HVTravel.Domain.Entities;

namespace HVTravel.Web.Models;

public class CustomerPortalDashboardViewModel
{
    public Customer Customer { get; set; } = new();
    public LoyaltyTierViewModel Tier { get; set; } = new();
    public Booking? UpcomingBooking { get; set; }
    public IReadOnlyList<Booking> RecentBookings { get; set; } = Array.Empty<Booking>();
    public IReadOnlyList<PortalReviewRequestViewModel> ReviewRequests { get; set; } = Array.Empty<PortalReviewRequestViewModel>();
    public IReadOnlyList<VoucherWalletItem> ActiveVouchers { get; set; } = Array.Empty<VoucherWalletItem>();
    public IReadOnlyList<SavedTravellerProfile> SavedTravellers { get; set; } = Array.Empty<SavedTravellerProfile>();
    public IReadOnlyList<Promotion> PersonalizedOffers { get; set; } = Array.Empty<Promotion>();
    public IReadOnlyList<Notification> RecentNotifications { get; set; } = Array.Empty<Notification>();
    public int UnreadNotificationCount { get; set; }
}

public class LoyaltyTierViewModel
{
    public string Name { get; set; } = "Explorer";
    public string Description { get; set; } = string.Empty;
    public int Points { get; set; }
    public decimal LifetimeSpend { get; set; }
}

public class PortalReviewRequestViewModel
{
    public string BookingId { get; set; } = string.Empty;
    public string BookingCode { get; set; } = string.Empty;
    public string TourId { get; set; } = string.Empty;
    public string TourName { get; set; } = string.Empty;
    public DateTime? TravelDate { get; set; }
}

public class SavedTravellerInputViewModel
{
    public string FullName { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string PassportNumber { get; set; } = string.Empty;
    public string Nationality { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
}

public class ReviewSubmissionViewModel
{
    public string BookingId { get; set; } = string.Empty;
    public string TourId { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
}

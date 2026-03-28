using HVTravel.Domain.Entities;
using HVTravel.Domain.Interfaces;
using HVTravel.Web.Models;

namespace HVTravel.Web.Services;

public class CustomerPortalService
{
    private readonly IRepository<Customer> _customerRepository;
    private readonly IRepository<Booking> _bookingRepository;
    private readonly IRepository<VoucherWalletItem> _voucherRepository;
    private readonly IRepository<SavedTravellerProfile> _travellerRepository;
    private readonly IRepository<Review> _reviewRepository;
    private readonly IRepository<Notification> _notificationRepository;
    private readonly IRepository<Promotion> _promotionRepository;

    public CustomerPortalService(
        IRepository<Customer> customerRepository,
        IRepository<Booking> bookingRepository,
        IRepository<VoucherWalletItem> voucherRepository,
        IRepository<SavedTravellerProfile> travellerRepository,
        IRepository<Review> reviewRepository,
        IRepository<Notification> notificationRepository,
        IRepository<Promotion> promotionRepository)
    {
        _customerRepository = customerRepository;
        _bookingRepository = bookingRepository;
        _voucherRepository = voucherRepository;
        _travellerRepository = travellerRepository;
        _reviewRepository = reviewRepository;
        _notificationRepository = notificationRepository;
        _promotionRepository = promotionRepository;
    }

    public async Task<CustomerPortalDashboardViewModel> BuildDashboardAsync(string customerId)
    {
        var customer = await _customerRepository.GetByIdAsync(customerId) ?? new Customer { Id = customerId, Stats = new CustomerStats() };
        customer.Stats ??= new CustomerStats();

        var bookings = (await _bookingRepository.FindAsync(b => b.CustomerId == customerId && !b.IsDeleted))
            .OrderByDescending(b => b.BookingDate)
            .ToList();

        var now = DateTime.UtcNow;
        var upcomingBooking = bookings
            .Where(b => (b.TourSnapshot?.StartDate ?? DateTime.MinValue) >= now && b.Status != "Cancelled")
            .OrderBy(b => b.TourSnapshot?.StartDate)
            .FirstOrDefault();

        var existingReviews = await _reviewRepository.FindAsync(r => r.CustomerId == customerId);
        var reviewedBookingIds = existingReviews
            .Where(r => !string.IsNullOrWhiteSpace(r.BookingId))
            .Select(r => r.BookingId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var reviewRequests = bookings
            .Where(b => string.Equals(b.Status, "Completed", StringComparison.OrdinalIgnoreCase))
            .Where(b => (b.TourSnapshot?.StartDate ?? b.BookingDate) <= now)
            .Where(b => !reviewedBookingIds.Contains(b.Id ?? string.Empty))
            .Select(b => new PortalReviewRequestViewModel
            {
                BookingId = b.Id ?? string.Empty,
                BookingCode = b.BookingCode ?? string.Empty,
                TourId = b.TourId ?? string.Empty,
                TourName = b.TourSnapshot?.Name ?? "Hành trình HV Travel",
                TravelDate = b.TourSnapshot?.StartDate
            })
            .ToList();

        var activeVouchers = (await _voucherRepository.FindAsync(v => v.CustomerId == customerId))
            .Where(v => string.Equals(v.Status, "Active", StringComparison.OrdinalIgnoreCase))
            .Where(v => !v.ExpiresAt.HasValue || v.ExpiresAt.Value >= now)
            .OrderBy(v => v.ExpiresAt)
            .ToList();

        var savedTravellers = (await _travellerRepository.FindAsync(t => t.CustomerId == customerId))
            .OrderByDescending(t => t.IsDefault)
            .ThenBy(t => t.FullName)
            .ToList();

        var notifications = (await _notificationRepository.FindAsync(n => n.RecipientId == customerId || n.RecipientId == "ALL"))
            .OrderByDescending(n => n.CreatedAt)
            .ToList();

        var offers = (await _promotionRepository.GetAllAsync())
            .Where(IsPromotionActive)
            .Where(p => IsEligibleForSegment(p, customer.Segment))
            .OrderByDescending(p => p.Priority)
            .ThenByDescending(p => p.DiscountPercentage)
            .ToList();

        var tier = ResolveTier(customer.Stats.LoyaltyPoints, customer.Stats.LifetimeSpend);
        customer.Stats.Tier = tier.Name;

        return new CustomerPortalDashboardViewModel
        {
            Customer = customer,
            Tier = tier,
            UpcomingBooking = upcomingBooking,
            RecentBookings = bookings.Take(6).ToList(),
            ReviewRequests = reviewRequests,
            ActiveVouchers = activeVouchers,
            SavedTravellers = savedTravellers,
            PersonalizedOffers = offers,
            RecentNotifications = notifications.Take(5).ToList(),
            UnreadNotificationCount = notifications.Count(n => !n.IsRead)
        };
    }

    public static LoyaltyTierViewModel ResolveTier(int points, decimal lifetimeSpend)
    {
        if (points >= 10000 || lifetimeSpend >= 120000000)
        {
            return new LoyaltyTierViewModel
            {
                Name = "Diamond",
                Description = "Ưu tiên giữ chỗ, concierge và đặc quyền đối tác.",
                Points = points,
                LifetimeSpend = lifetimeSpend
            };
        }

        if (points >= 5000 || lifetimeSpend >= 60000000)
        {
            return new LoyaltyTierViewModel
            {
                Name = "Gold",
                Description = "Voucher định kỳ, hotline ưu tiên và ưu đãi sớm.",
                Points = points,
                LifetimeSpend = lifetimeSpend
            };
        }

        if (points >= 2000 || lifetimeSpend >= 20000000)
        {
            return new LoyaltyTierViewModel
            {
                Name = "Silver",
                Description = "Nhận deal cá nhân hóa và quà tặng theo chiến dịch.",
                Points = points,
                LifetimeSpend = lifetimeSpend
            };
        }

        return new LoyaltyTierViewModel
        {
            Name = "Explorer",
            Description = "Bắt đầu tích điểm cho những chuyến đi đầu tiên.",
            Points = points,
            LifetimeSpend = lifetimeSpend
        };
    }

    private static bool IsPromotionActive(Promotion promotion)
    {
        var now = DateTime.UtcNow;
        return promotion.IsActive && promotion.ValidFrom <= now && promotion.ValidTo >= now;
    }

    private static bool IsEligibleForSegment(Promotion promotion, string segment)
    {
        if (promotion.EligibleSegments == null || promotion.EligibleSegments.Count == 0)
        {
            return true;
        }

        return promotion.EligibleSegments.Any(value =>
            string.Equals(value, segment, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(value, "All", StringComparison.OrdinalIgnoreCase));
    }
}

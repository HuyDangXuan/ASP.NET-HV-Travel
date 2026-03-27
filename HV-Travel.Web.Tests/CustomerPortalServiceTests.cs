using HVTravel.Domain.Entities;
using HVTravel.Web.Services;
using HV_Travel.Web.Tests.TestSupport;

namespace HV_Travel.Web.Tests;

public class CustomerPortalServiceTests
{
    [Fact]
    public async Task BuildDashboardAsync_AggregatesBookingsWalletAndReviewEligibility()
    {
        var customer = new Customer
        {
            Id = "customer-1",
            CustomerCode = "CUS000001",
            FullName = "Nguyễn Ngọc Mai",
            Email = "mai@example.com",
            PhoneNumber = "0909000111",
            Segment = "VIP",
            Stats = new CustomerStats
            {
                LoyaltyPoints = 6200,
                LifetimeSpend = 87000000,
                TripCount = 5,
                ReferralCode = "MAI-TRAVEL"
            }
        };

        var bookings = new InMemoryRepository<Booking>(
        [
            new Booking
            {
                Id = "booking-upcoming",
                BookingCode = "BK-UPCOMING",
                CustomerId = customer.Id,
                TourSnapshot = new TourSnapshot { Name = "Nhật Bản mùa hoa", Code = "JP-01", StartDate = DateTime.UtcNow.AddDays(25), Duration = "5 ngày 4 đêm" },
                BookingDate = DateTime.UtcNow.AddDays(-3),
                Status = "Confirmed",
                PaymentStatus = "Paid",
                TotalAmount = 32000000,
                ParticipantsCount = 2,
                ContactInfo = new ContactInfo { Name = customer.FullName, Email = customer.Email, Phone = customer.PhoneNumber }
            },
            new Booking
            {
                Id = "booking-completed",
                BookingCode = "BK-COMPLETED",
                CustomerId = customer.Id,
                TourId = "tour-completed",
                TourSnapshot = new TourSnapshot { Name = "Seoul cuối thu", Code = "KR-01", StartDate = DateTime.UtcNow.AddDays(-40), Duration = "4 ngày 3 đêm" },
                BookingDate = DateTime.UtcNow.AddDays(-60),
                Status = "Completed",
                PaymentStatus = "Paid",
                TotalAmount = 22000000,
                ParticipantsCount = 2,
                ContactInfo = new ContactInfo { Name = customer.FullName, Email = customer.Email, Phone = customer.PhoneNumber }
            }
        ]);

        var service = new CustomerPortalService(
            new InMemoryRepository<Customer>([customer]),
            bookings,
            new InMemoryRepository<VoucherWalletItem>(
            [
                new VoucherWalletItem { Id = "voucher-1", CustomerId = customer.Id, Code = "VIPFLASH", Title = "Flash Sale Nhật Bản", Status = "Active", ExpiresAt = DateTime.UtcNow.AddDays(7), DiscountPercentage = 12 },
                new VoucherWalletItem { Id = "voucher-2", CustomerId = customer.Id, Code = "USED", Title = "Đã dùng", Status = "Used", ExpiresAt = DateTime.UtcNow.AddDays(7), DiscountPercentage = 5 }
            ]),
            new InMemoryRepository<SavedTravellerProfile>(
            [
                new SavedTravellerProfile { Id = "traveller-1", CustomerId = customer.Id, FullName = "Nguyễn Ngọc Mai", PassportNumber = "C1234567", Nationality = "Việt Nam", IsDefault = true }
            ]),
            new InMemoryRepository<Review>(),
            new InMemoryRepository<Notification>(
            [
                new Notification { Id = "noti-1", RecipientId = customer.Id, Title = "Nhắc thanh toán", Message = "Hoàn tất thanh toán", IsRead = false },
                new Notification { Id = "noti-2", RecipientId = customer.Id, Title = "Ưu đãi", Message = "Voucher mới", IsRead = true }
            ]),
            new InMemoryRepository<Promotion>(
            [
                new Promotion { Id = "promo-1", Code = "VIPFLASH", Title = "VIP Flash", CampaignType = "Voucher", Description = "Ưu đãi khách VIP", ValidFrom = DateTime.UtcNow.AddDays(-1), ValidTo = DateTime.UtcNow.AddDays(10), IsActive = true, EligibleSegments = ["VIP"] },
                new Promotion { Id = "promo-2", Code = "STAFF", Title = "Không phù hợp", CampaignType = "Voucher", Description = "Không áp dụng", ValidFrom = DateTime.UtcNow.AddDays(-1), ValidTo = DateTime.UtcNow.AddDays(10), IsActive = true, EligibleSegments = ["Corporate"] }
            ]));

        var dashboard = await service.BuildDashboardAsync(customer.Id);

        Assert.Equal("Gold", dashboard.Tier.Name);
        Assert.Equal("BK-UPCOMING", dashboard.UpcomingBooking?.BookingCode);
        Assert.Single(dashboard.ReviewRequests);
        Assert.Single(dashboard.ActiveVouchers);
        Assert.Single(dashboard.SavedTravellers);
        Assert.Equal(1, dashboard.UnreadNotificationCount);
        Assert.Contains(dashboard.PersonalizedOffers, offer => offer.Code == "VIPFLASH");
    }
}

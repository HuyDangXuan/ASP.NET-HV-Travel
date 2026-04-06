using HVTravel.Domain.Entities;
using HVTravel.Web.Models;
using HVTravel.Web.Services;

namespace HV_Travel.Web.Tests;

public class BookingJourneyPresenterTests
{
    [Fact]
    public void BuildStatusPage_ReturnsReservedPendingTransferReviewVariant()
    {
        var presenter = new BookingJourneyPresenter();
        var booking = BuildBooking();
        booking.Status = "PendingPayment";
        booking.PaymentStatus = "Pending";
        booking.TransferProofBase64 = "proof-base64";

        var page = presenter.BuildStatusPage(booking, null, BookingJourneyStage.Success, "+84 901 234 567", "support@hvtravel.vn");

        Assert.Equal("ReservedPendingTransferReview", page.Status.Variant);
        Assert.Equal("warning", page.Status.Tone);
        Assert.Contains(page.Summary.Rows, row => row.Label == "Trạng thái thanh toán" && row.Value == "Đang chờ xử lý");
        Assert.NotEmpty(page.Timeline.Items);
    }

    [Fact]
    public void BuildLookupPage_UsesSharedSummaryAndTimelineShape()
    {
        var presenter = new BookingJourneyPresenter();
        var booking = BuildBooking();

        var page = presenter.BuildLookupPage(booking, "+84 901 234 567", "support@hvtravel.vn");

        Assert.Equal("lookup", page.StageKey);
        Assert.NotEmpty(page.Summary.Rows);
        Assert.NotEmpty(page.Timeline.Items);
        Assert.Equal(booking.BookingCode, page.Status.Reference);
    }

    private static Booking BuildBooking()
    {
        return new Booking
        {
            Id = "booking-1",
            BookingCode = "HV20260405001",
            Status = "Confirmed",
            PaymentStatus = "Paid",
            TourId = "tour-1",
            CheckoutSessionId = "checkout-1",
            ContactInfo = new ContactInfo
            {
                Name = "Nguyen Van A",
                Email = "guest@example.com",
                Phone = "0901234567"
            },
            TourSnapshot = new TourSnapshot
            {
                Name = "Europe Grand Tour",
                Code = "EU-01",
                Duration = "10 ngày 9 đêm",
                StartDate = new DateTime(2026, 6, 12)
            },
            PricingBreakdown = new PricingBreakdown
            {
                Subtotal = 32000000m,
                DiscountTotal = 2000000m,
                GrandTotal = 30000000m
            },
            PaymentPlan = new PaymentPlan
            {
                PlanType = "Deposit",
                AmountDueNow = 9000000m,
                BalanceDue = 21000000m,
                DepositPercentage = 30m
            },
            CouponCode = "SUMMER10",
            ParticipantsCount = 2,
            Events =
            [
                new BookingEvent
                {
                    Type = "payment",
                    Title = "Đã tạo booking",
                    Description = "Booking đã được tạo trong hệ thống.",
                    Actor = "System",
                    OccurredAt = new DateTime(2026, 4, 5, 10, 0, 0, DateTimeKind.Utc),
                    VisibleToCustomer = true
                }
            ]
        };
    }
}


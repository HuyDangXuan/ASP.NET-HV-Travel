using HVTravel.Application.Interfaces;
using HVTravel.Domain.Entities;
using HVTravel.Domain.Interfaces;

namespace HV_Travel.Web.Tests;

public class TourCommerceContractTests
{
    [Fact]
    public void Tour_ExposesCommerceAndSeoContracts()
    {
        Assert.NotNull(typeof(Tour).GetProperty("Slug"));
        Assert.NotNull(typeof(Tour).GetProperty("Seo"));
        Assert.NotNull(typeof(Tour).GetProperty("CancellationPolicy"));
        Assert.NotNull(typeof(Tour).GetProperty("ConfirmationType"));
        Assert.NotNull(typeof(Tour).GetProperty("Highlights"));
        Assert.NotNull(typeof(Tour).GetProperty("MeetingPoint"));
        Assert.NotNull(typeof(Tour).GetProperty("SupplierRef"));
        Assert.NotNull(typeof(Tour).GetProperty("BadgeSet"));
        Assert.NotNull(typeof(Tour).GetProperty("Departures"));

        Assert.NotNull(typeof(TourDeparture).GetProperty("Id"));
        Assert.NotNull(typeof(TourDeparture).GetProperty("StartDate"));
        Assert.NotNull(typeof(TourDeparture).GetProperty("AdultPrice"));
        Assert.NotNull(typeof(TourDeparture).GetProperty("Capacity"));
        Assert.NotNull(typeof(TourDeparture).GetProperty("BookedCount"));
        Assert.NotNull(typeof(TourDeparture).GetProperty("ConfirmationType"));
        Assert.NotNull(typeof(TourDeparture).GetProperty("RemainingCapacity"));

        Assert.NotNull(typeof(SeoMetadata).GetProperty("Title"));
        Assert.NotNull(typeof(SeoMetadata).GetProperty("Description"));
        Assert.NotNull(typeof(SeoMetadata).GetProperty("CanonicalPath"));
        Assert.NotNull(typeof(TourCancellationPolicy).GetProperty("Summary"));
        Assert.NotNull(typeof(TourCancellationPolicy).GetProperty("IsFreeCancellation"));
    }

    [Fact]
    public void Booking_ExposesCheckoutPaymentAndFulfillmentContracts()
    {
        Assert.NotNull(typeof(Booking).GetProperty("DepartureId"));
        Assert.NotNull(typeof(Booking).GetProperty("PricingBreakdown"));
        Assert.NotNull(typeof(Booking).GetProperty("CouponCode"));
        Assert.NotNull(typeof(Booking).GetProperty("VoucherRedemptions"));
        Assert.NotNull(typeof(Booking).GetProperty("PaymentPlan"));
        Assert.NotNull(typeof(Booking).GetProperty("PaymentSessions"));
        Assert.NotNull(typeof(Booking).GetProperty("IssuedDocuments"));
        Assert.NotNull(typeof(Booking).GetProperty("FulfillmentStatus"));
        Assert.NotNull(typeof(Booking).GetProperty("FulfillmentItems"));
        Assert.NotNull(typeof(Booking).GetProperty("CheckoutSessionId"));

        Assert.NotNull(typeof(PricingBreakdown).GetProperty("Subtotal"));
        Assert.NotNull(typeof(PricingBreakdown).GetProperty("DiscountTotal"));
        Assert.NotNull(typeof(PricingBreakdown).GetProperty("GrandTotal"));
        Assert.NotNull(typeof(CouponRedemption).GetProperty("Code"));
        Assert.NotNull(typeof(CouponRedemption).GetProperty("DiscountAmount"));
        Assert.NotNull(typeof(PaymentPlan).GetProperty("PlanType"));
        Assert.NotNull(typeof(PaymentPlan).GetProperty("AmountDueNow"));
        Assert.NotNull(typeof(PaymentSession).GetProperty("IdempotencyKey"));
        Assert.NotNull(typeof(PaymentSession).GetProperty("Reference"));
        Assert.NotNull(typeof(IssuedDocument).GetProperty("DocumentType"));
        Assert.NotNull(typeof(FulfillmentItem).GetProperty("ItemType"));
        Assert.NotNull(typeof(CheckoutSession).GetProperty("BookingId"));
    }

    [Fact]
    public void CommerceServices_ExposeSearchPricingCheckoutInventoryPromotionAndAnalyticsContracts()
    {
        Assert.NotNull(typeof(ITourSearchService));
        Assert.NotNull(typeof(IPricingService));
        Assert.NotNull(typeof(ICheckoutService));
        Assert.NotNull(typeof(IPromotionEngine));
        Assert.NotNull(typeof(IInventoryService));
        Assert.NotNull(typeof(IAnalyticsTracker));
    }

    [Fact]
    public void TourRepository_ExposesDepartureAwareSearchOperations()
    {
        Assert.NotNull(typeof(ITourRepository).GetMethod("SearchAsync"));
        Assert.NotNull(typeof(ITourRepository).GetMethod("GetBySlugAsync"));
        Assert.NotNull(typeof(ITourRepository).GetMethod("ReserveDepartureAsync"));
    }
}

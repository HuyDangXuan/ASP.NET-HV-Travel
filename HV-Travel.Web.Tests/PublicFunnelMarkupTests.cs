namespace HV_Travel.Web.Tests;

public class PublicFunnelMarkupTests
{
    [Fact]
    public void PublicToursIndex_ExposesAdvancedFiltersFacetHooksAndUsesDedicatedTourCardPartial()
    {
        var content = File.ReadAllText(GetRepoPath(@"HV-Travel.Web\Views\PublicTours\Index.cshtml"));
        var filterPartial = File.ReadAllText(GetRepoPath(@"HV-Travel.Web\Views\PublicTours\_PublicToursFilter.cshtml"));

        Assert.Contains("PartialAsync(\"_PublicToursFilter\"", content);
        Assert.Contains("name=\"travellers\"", filterPartial);
        Assert.Contains("name=\"confirmationType\"", filterPartial);
        Assert.Contains("name=\"cancellationType\"", filterPartial);
        Assert.Contains("availableOnlyLabelText", filterPartial);
        Assert.Contains("promotionOnlyLabelText", filterPartial);
        Assert.Contains("applyButtonText", filterPartial);
        Assert.Contains("resetButtonText", filterPartial);
        Assert.Contains("best_value", filterPartial);
        Assert.Contains("RegionFacets", content);
        Assert.Contains("ConfirmationTypeFacets", content);
        Assert.Contains("asp-route-travellers", content);
        Assert.Contains("asp-route-confirmationType", content);
        Assert.Contains("asp-route-cancellationType", content);
        Assert.Contains("collectionChips", content);
        Assert.Contains("resultsPanel", content);
        Assert.Contains("[\"FilterPanel\"] = filterPanel", content);
        Assert.Contains("data-empty-text=\"@wishlistEmptyText\"", content);
        Assert.Contains("data-empty-text=\"@recentEmptyText\"", content);
        Assert.Contains("emptyStateCtaText", content);
        Assert.Contains("public-filter-form", filterPartial);
        Assert.Contains("public-filter-heading", filterPartial);
        Assert.Contains("public-filter-shell", filterPartial);
        Assert.Contains("public-filter-section", filterPartial);
        Assert.Contains("public-filter-actions", filterPartial);
        Assert.Contains("public-filter-grid", filterPartial);
        Assert.Contains("public-filter-row", filterPartial);
        Assert.Contains("public-custom-select", filterPartial);
        Assert.Contains("data-public-select-id", filterPartial);
        Assert.Contains("name=\"region\"", filterPartial);
        Assert.Contains("name=\"destination\"", filterPartial);
        Assert.Contains("name=\"departureMonth\"", filterPartial);
        Assert.Contains("name=\"sort\"", filterPartial);
        Assert.Contains("name=\"confirmationType\"", filterPartial);
        Assert.Contains("name=\"cancellationType\"", filterPartial);
        Assert.Contains("applyButtonText", filterPartial);
        Assert.Contains("resetButtonText", filterPartial);
        Assert.Contains("PartialAsync(\"_TourCard\"", content);
        Assert.Contains("lg:grid-cols-[360px_minmax(0,1fr)]", content);
        Assert.Contains("items-start", content);
        Assert.Contains("self-start", content);
    }

    [Fact]
    public void PublicToursIndex_LoadsDedicatedPublicCustomSelectScript()
    {
        var content = File.ReadAllText(GetRepoPath(@"HV-Travel.Web\Views\PublicTours\Index.cshtml"));
        var filterPartial = File.ReadAllText(GetRepoPath(@"HV-Travel.Web\Views\PublicTours\_PublicToursFilter.cshtml"));
        var script = File.ReadAllText(GetRepoPath(@"HV-Travel.Web\wwwroot\js\public-custom-select.js"));

        Assert.Contains("~/js/public-custom-select.js", content);
        Assert.Contains("data-public-select-id", script);
        Assert.Contains("togglePublicCustomSelect", script);
        Assert.Contains("selectPublicCustomOption", script);
    }

    [Fact]
    public void ServicesIndex_UsesPublicCustomSelectAndDatePickerControls()
    {
        var content = File.ReadAllText(GetRepoPath(@"HV-Travel.Web\Views\Services\Index.cshtml"));
        var selectScript = File.ReadAllText(GetRepoPath(@"HV-Travel.Web\wwwroot\js\public-custom-select.js"));
        var datePickerScript = File.ReadAllText(GetRepoPath(@"HV-Travel.Web\wwwroot\js\public-date-picker.js"));

        Assert.Contains("public-service-form", content);
        Assert.Contains("data-public-select-id=\"serviceType\"", content);
        Assert.Contains("asp-for=\"Request.ServiceType\"", content);
        Assert.Contains("data-public-date-input", content);
        Assert.Contains("asp-for=\"Request.DepartureDate\"", content);
        Assert.Contains("asp-for=\"Request.ReturnDate\"", content);
        Assert.Contains("~/js/public-custom-select.js", content);
        Assert.Contains("~/js/public-date-picker.js", content);
        Assert.Contains("togglePublicCustomSelect", selectScript);
        Assert.Contains("window.PublicDatePicker", datePickerScript);
        Assert.Contains("data-public-date-input", datePickerScript);
    }

    [Fact]
    public void ContactView_UsesPublicCustomSelectHooks()
    {
        var content = File.ReadAllText(GetRepoPath(@"HV-Travel.Web\Views\Home\Contact.cshtml"));
        var selectScript = File.ReadAllText(GetRepoPath(@"HV-Travel.Web\wwwroot\js\public-custom-select.js"));

        Assert.Contains("public-custom-select", content);
        Assert.Contains("data-public-select-id=\"contactSubject\"", content);
        Assert.Contains("asp-for=\"Subject\"", content);
        Assert.Contains("~/js/public-custom-select.js", content);
        Assert.Contains("togglePublicCustomSelect", selectScript);
        Assert.Contains("selectPublicCustomOption", selectScript);
    }

    [Fact]
    public void SharedTourCardHosts_UseSharedPartialAndSharedCardScript()
    {
        var home = File.ReadAllText(GetRepoPath(@"HV-Travel.Web\Views\Home\Index.cshtml"));
        var publicTours = File.ReadAllText(GetRepoPath(@"HV-Travel.Web\Views\PublicTours\Index.cshtml"));
        var sharedCardScript = File.ReadAllText(GetRepoPath(@"HV-Travel.Web\wwwroot\js\public-tour-card.js"));

        Assert.Contains("PartialAsync(\"~/Views/PublicTours/_TourCard.cshtml\"", home);
        Assert.Contains("public-tour-card-grid", home);
        Assert.Contains("~/js/public-tour-card.js", home);
        Assert.DoesNotContain("class=\"tour-card group", home);

        Assert.Contains("~/js/public-tour-card.js", publicTours);
        Assert.Contains("hvtravel_wishlist", sharedCardScript);
        Assert.Contains("record-view", sharedCardScript);
        Assert.Contains("wishlist-shell", sharedCardScript);
        Assert.Contains("data-tour-wishlist-toggle", sharedCardScript);
    }

    [Fact]
    public void PublicTourCardPartial_ExposesSampleDrivenCommerceMarkup()
    {
        var content = File.ReadAllText(GetRepoPath(@"HV-Travel.Web\Views\PublicTours\_TourCard.cshtml"));

        Assert.Contains("tour.EffectiveDepartures", content);
        Assert.Contains("tour.Slug", content);
        Assert.Contains("data-tour-wishlist-toggle", content);
        Assert.Contains("tour-card-heart", content);
        Assert.Contains("tour-card-code", content);
        Assert.Contains("tour-card-departure", content);
        Assert.Contains("tour-card-duration", content);
        Assert.Contains("tour-card-remaining", content);
        Assert.Contains("tour-card-original-price", content);
        Assert.Contains("data-tour-image-fallback", content);
        Assert.Contains("onerror=", content);
        Assert.Contains("Đặt ngay", content);
        Assert.Contains("4N3Đ", content);
        Assert.DoesNotContain("meetingPoint", content);
        Assert.DoesNotContain("countdown", content);
    }

    [Fact]
    public void BookingCreate_UsesJourneyDeskBuilderAndLiveQuoteHooks()
    {
        var content = File.ReadAllText(GetRepoPath(@"HV-Travel.Web\Views\Booking\Create.cshtml"));

        Assert.Contains("Model.Journey", content);
        Assert.Contains("PartialAsync(\"_JourneyHeader\"", content);
        Assert.Contains("PartialAsync(\"_JourneyStageBar\"", content);
        Assert.Contains("PartialAsync(\"_JourneySummaryRail\"", content);
        Assert.Contains("PartialAsync(\"_JourneySupportPanel\"", content);
        Assert.Contains("public-journey-shell", content);
        Assert.Contains("public-journey-desk", content);
        Assert.Contains("public-journey-panel", content);
        Assert.Contains("public-journey-quote", content);
        Assert.Contains("public-journey-summary", content);
        Assert.Contains("public-booking-runway", content);
        Assert.Contains("public-booking-intro", content);
        Assert.Contains("public-booking-overview-grid", content);
        Assert.Contains("public-booking-section", content);
        Assert.Contains("public-booking-finance-panel", content);
        Assert.Contains("public-booking-coupon-disclosure", content);
        Assert.Contains("public-booking-field", content);
        Assert.Contains("public-booking-party-grid", content);
        Assert.Contains("data-booking-selected-date", content);
        Assert.Contains("data-booking-selected-capacity", content);
        Assert.Contains("data-booking-selected-plan", content);
        Assert.Contains("asp-for=\"DepartureId\"", content);
        Assert.Contains("asp-for=\"CouponCode\"", content);
        Assert.Contains("asp-for=\"PaymentPlanType\"", content);
        Assert.Contains("Có mã ưu đãi?", content);
        Assert.Contains("Quote", content);
        Assert.Contains("fetch(", content);
        Assert.DoesNotContain("Stage 1", content);
        Assert.DoesNotContain("Stage 2", content);
        Assert.DoesNotContain("Stage 3", content);
    }

    [Fact]
    public void PaymentLookupAndStatusViews_UseSharedJourneyPartialsAndStatusHooks()
    {
        var payment = File.ReadAllText(GetRepoPath(@"HV-Travel.Web\Views\Booking\Payment.cshtml"));
        var success = File.ReadAllText(GetRepoPath(@"HV-Travel.Web\Views\Booking\Success.cshtml"));
        var failed = File.ReadAllText(GetRepoPath(@"HV-Travel.Web\Views\Booking\Failed.cshtml"));
        var lookup = File.ReadAllText(GetRepoPath(@"HV-Travel.Web\Views\BookingLookup\Index.cshtml"));
        var consultation = File.ReadAllText(GetRepoPath(@"HV-Travel.Web\Views\Booking\Consultation.cshtml"));

        Assert.Contains("PartialAsync(\"_JourneyHeader\"", payment);
        Assert.Contains("PartialAsync(\"_JourneySummaryRail\"", payment);
        Assert.Contains("PartialAsync(\"_JourneyTimeline\"", payment);
        Assert.Contains("PartialAsync(\"_JourneySupportPanel\"", payment);
        Assert.Contains("PartialAsync(\"_JourneyPaymentMethodSwitcher\"", payment);
        Assert.Contains("public-journey-payment-switcher", payment);
        Assert.Contains("public-booking-payment-shell", payment);
        Assert.Contains("public-booking-payment-glance", payment);
        Assert.Contains("public-booking-glance-card", payment);
        Assert.Contains("booking.PaymentSessions", payment);
        Assert.Contains("booking.CheckoutSessionId", payment);
        Assert.DoesNotContain("Payment desk", payment);
        Assert.DoesNotContain("Transfer proof", payment);

        Assert.Contains("PartialAsync(\"_JourneyStatusPanel\"", success);
        Assert.Contains("PartialAsync(\"_JourneySummaryRail\"", success);
        Assert.Contains("PartialAsync(\"_JourneyTimeline\"", success);
        Assert.Contains("public-journey-status-panel", success);
        Assert.Contains("public-booking-status-shell", success);
        Assert.Contains("public-booking-status-intro", success);

        Assert.Contains("PartialAsync(\"_JourneyStatusPanel\"", failed);
        Assert.Contains("public-journey-status-panel", failed);
        Assert.Contains("public-booking-status-shell", failed);
        Assert.Contains("public-booking-status-intro", failed);

        Assert.Contains("public-booking-lookup-shell", lookup);
        Assert.Contains("public-booking-lookup-intro", lookup);
        Assert.Contains("public-journey-status-panel", lookup);
        Assert.Contains("public-journey-timeline", lookup);
        Assert.Contains("Tra cứu booking", lookup);

        Assert.Contains("public-booking-support-shell", consultation);
        Assert.Contains("public-journey-support", consultation);
        Assert.Contains("public-journey-panel", consultation);
        Assert.Contains("TourInterest", consultation);
        Assert.DoesNotContain("Support desk", consultation);
    }

    [Fact]
    public void PublicToursDetails_UsesTourDossierPosterRailAndDepartureMatrixHooks()
    {
        var content = File.ReadAllText(GetRepoPath(@"HV-Travel.Web\Views\PublicTours\Details.cshtml"));

        Assert.Contains("public-tour-dossier-shell", content);
        Assert.Contains("public-tour-dossier-hero", content);
        Assert.Contains("public-tour-dossier-poster", content);
        Assert.Contains("public-tour-dossier-grid", content);
        Assert.Contains("public-tour-dossier-rail", content);
        Assert.Contains("public-tour-dossier-strip", content);
        Assert.Contains("public-tour-dossier-chapter", content);
        Assert.Contains("public-tour-dossier-departures", content);
        Assert.Contains("public-tour-dossier-booking-bar", content);
        Assert.Contains("asp-route-departureId", content);
        Assert.Contains("asp-route-startDate", content);
        Assert.Contains("asp-controller=\"Booking\"", content);
    }

    [Fact]
    public void PublicFunnelViews_DoNotUse_CommentPlaceholderHooks()
    {
        var index = File.ReadAllText(GetRepoPath(@"HV-Travel.Web\Views\PublicTours\Index.cshtml"));
        var create = File.ReadAllText(GetRepoPath(@"HV-Travel.Web\Views\Booking\Create.cshtml"));
        var payment = File.ReadAllText(GetRepoPath(@"HV-Travel.Web\Views\Booking\Payment.cshtml"));
        var success = File.ReadAllText(GetRepoPath(@"HV-Travel.Web\Views\Booking\Success.cshtml"));
        var failed = File.ReadAllText(GetRepoPath(@"HV-Travel.Web\Views\Booking\Failed.cshtml"));

        Assert.DoesNotContain("@* ContentPresentationViewHelper", index);
        Assert.DoesNotContain("@* public-top-section", create);
        Assert.DoesNotContain("@* public-top-section", payment);
        Assert.DoesNotContain("@* ContentPresentationViewHelper", success);
        Assert.DoesNotContain("@* ContentPresentationViewHelper", failed);
    }

    [Fact]
    public void PublicToursDetails_UsesUnicodeCustomerFacingPhrases()
    {
        var content = File.ReadAllText(GetRepoPath(@"HV-Travel.Web\Views\PublicTours\Details.cshtml"));

        Assert.DoesNotContain("Trang ch???", content);
        Assert.DoesNotContain("Tour du l???ch", content);
        Assert.DoesNotContain("X??c nh???n t???c th??", content);
        Assert.DoesNotContain("Gi?? t???", content);
        Assert.DoesNotContain("L???ch tr??nh chi ti???t", content);
    }

    [Fact]
    public void BookingViewModel_UsesUnicodeValidationMessages()
    {
        var content = File.ReadAllText(GetRepoPath(@"HV-Travel.Web\Models\BookingViewModel.cs"));

        Assert.DoesNotContain("Vui l??ng nh???p", content);
        Assert.DoesNotContain("kh??ng h???p l???", content);
        Assert.DoesNotContain("C???n ??t nh???t 1 ng?????i l???n", content);
    }

    [Fact]
    public void BookingWorkflowViews_UseUnicodeVietnameseCopyWithoutEnglishScaffolding()
    {
        var create = File.ReadAllText(GetRepoPath(@"HV-Travel.Web\Views\Booking\Create.cshtml"));
        var payment = File.ReadAllText(GetRepoPath(@"HV-Travel.Web\Views\Booking\Payment.cshtml"));
        var success = File.ReadAllText(GetRepoPath(@"HV-Travel.Web\Views\Booking\Success.cshtml"));
        var failed = File.ReadAllText(GetRepoPath(@"HV-Travel.Web\Views\Booking\Failed.cshtml"));
        var error = File.ReadAllText(GetRepoPath(@"HV-Travel.Web\Views\Booking\Error.cshtml"));
        var consultation = File.ReadAllText(GetRepoPath(@"HV-Travel.Web\Views\Booking\Consultation.cshtml"));
        var lookup = File.ReadAllText(GetRepoPath(@"HV-Travel.Web\Views\BookingLookup\Index.cshtml"));

        Assert.Contains("Có mã ưu đãi?", create);
        Assert.Contains("Chọn cách bạn muốn thanh toán", payment);
        Assert.Contains("Booking đã được ghi nhận", success);
        Assert.Contains("Thanh toán chưa hoàn tất", failed);
        Assert.Contains("Không thể hoàn tất thao tác", error);
        Assert.Contains("Gửi yêu cầu tư vấn", consultation);
        Assert.Contains("Tra cứu booking", lookup);

        Assert.DoesNotContain("Trip builder", create);
        Assert.DoesNotContain("Payment desk", payment);
        Assert.DoesNotContain("Booking status", success);
        Assert.DoesNotContain("Support desk", consultation);

        Assert.DoesNotContain("Trang ch???", create);
        Assert.DoesNotContain("T???i l???i", error);
        Assert.DoesNotContain("Th??? b???i", failed);
    }
    [Fact]
    public void PublicToursController_UsesUnicodePageTitle()
    {
        var content = File.ReadAllText(GetRepoPath(@"HV-Travel.Web\Controllers\PublicToursController.cs"));

        Assert.DoesNotContain("Tour Du L???ch", content);
        Assert.Contains("ViewData[\"Title\"]", content);
    }

    private static string GetRepoPath(string relativePath)
    {
        var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        return Path.Combine(repoRoot, relativePath);
    }
}




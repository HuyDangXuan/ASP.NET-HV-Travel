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
        Assert.Contains("availableOnlyLabelText", filterPartial);
        Assert.Contains("promotionOnlyLabelText", filterPartial);
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
    public void BookingCreate_ExposesDepartureCouponPlanAndQuotePreviewHooks()
    {
        var content = File.ReadAllText(GetRepoPath(@"HV-Travel.Web\Views\Booking\Create.cshtml"));

        Assert.Contains("tour.EffectiveDepartures", content);
        Assert.Contains("asp-for=\"DepartureId\"", content);
        Assert.Contains("asp-for=\"CouponCode\"", content);
        Assert.Contains("asp-for=\"PaymentPlanType\"", content);
        Assert.Contains("Quote", content);
        Assert.Contains("fetch(", content);
        Assert.Contains("amountDueNow", content);
        Assert.Contains("balanceDue", content);
    }

    [Fact]
    public void PaymentAndSuccess_ExposePaymentBreakdownSessionAndResumeHooks()
    {
        var payment = File.ReadAllText(GetRepoPath(@"HV-Travel.Web\Views\Booking\Payment.cshtml"));
        var success = File.ReadAllText(GetRepoPath(@"HV-Travel.Web\Views\Booking\Success.cshtml"));
        var failed = File.ReadAllText(GetRepoPath(@"HV-Travel.Web\Views\Booking\Failed.cshtml"));

        Assert.Contains("booking.PaymentPlan.AmountDueNow", payment);
        Assert.Contains("booking.PaymentPlan.BalanceDue", payment);
        Assert.Contains("booking.CouponCode", payment);
        Assert.Contains("booking.PaymentSessions", payment);
        Assert.Contains("booking.CheckoutSessionId", payment);
        Assert.Contains("booking.PaymentPlan.AmountDueNow", success);
        Assert.Contains("booking.PaymentPlan.BalanceDue", success);
        Assert.Contains("CustomerPortal", success);
        Assert.Contains("booking.CheckoutSessionId", failed);
        Assert.Contains("Resume", failed);
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







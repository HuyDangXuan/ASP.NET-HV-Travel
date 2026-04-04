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
        Assert.Contains("best_value", filterPartial);
        Assert.Contains("RegionFacets", content);
        Assert.Contains("ConfirmationTypeFacets", content);
        Assert.Contains("asp-route-travellers", content);
        Assert.Contains("asp-route-confirmationType", content);
        Assert.Contains("asp-route-cancellationType", content);
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
        Assert.Contains("ÃƒÆ’Ã¢â‚¬Å¾Ãƒâ€šÃ‚ÂÃƒÆ’Ã‚Â¡Ãƒâ€šÃ‚ÂºÃƒâ€šÃ‚Â·t ngay", content);
        Assert.Contains("4N3ÃƒÆ’Ã¢â‚¬Å¾Ãƒâ€šÃ‚Â", content);
        Assert.DoesNotContain("KhÃƒÆ’Ã‚Â¡Ãƒâ€šÃ‚Â»Ãƒâ€¦Ã‚Â¸i hÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â nh:", content);
        Assert.DoesNotContain("GiÃƒÆ’Ã‚Â¡Ãƒâ€šÃ‚Â»Ãƒâ€šÃ‚Â chÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â³t", content);
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

        Assert.Contains("Trang chÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â»ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â§", content);
        Assert.Contains("Tour du lÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â»ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€šÃ‚Â¹ch", content);
        Assert.Contains("CÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â²n ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â­t chÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â»ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â€šÂ¬Ã‚Â", content);
        Assert.Contains("ViÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â»ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€šÃ‚Â¡t Nam", content);
        Assert.Contains("ÃƒÆ’Ã†â€™ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¾ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¹Ã…â€œÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Âªm", content);
        Assert.Contains("ÃƒÆ’Ã†â€™ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¾ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¹Ã…â€œÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¡nh giÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¡", content);
    }

    [Fact]
    public void BookingViewModel_UsesUnicodeValidationMessages()
    {
        var content = File.ReadAllText(GetRepoPath(@"HV-Travel.Web\Models\BookingViewModel.cs"));

        Assert.Contains("Vui lÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â²ng nhÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚ÂºÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â­p hÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â»ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â tÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Âªn", content);
        Assert.Contains("Vui lÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â²ng nhÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚ÂºÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â­p email", content);
        Assert.Contains("Email khÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â´ng hÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â»ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â£p lÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â»ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€šÃ‚Â¡", content);
        Assert.Contains("Vui lÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â²ng nhÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚ÂºÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â­p sÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â»ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¹Ã…â€œ ÃƒÆ’Ã†â€™ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¾ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¹Ã…â€œiÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â»ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€šÃ‚Â¡n thoÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚ÂºÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¡i", content);
        Assert.Contains("SÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â»ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¹Ã…â€œ ÃƒÆ’Ã†â€™ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¾ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¹Ã…â€œiÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â»ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€šÃ‚Â¡n thoÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚ÂºÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¡i khÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â´ng hÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â»ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â£p lÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â»ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€šÃ‚Â¡", content);
        Assert.Contains("CÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚ÂºÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â§n ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â­t nhÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚ÂºÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¥t 1 ngÃƒÆ’Ã†â€™ÃƒÂ¢Ã¢â€šÂ¬Ã‚Â ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â°ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â»ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Âi lÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â»ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€šÃ‚Âºn", content);
    }

    [Fact]
    public void PublicToursController_UsesUnicodePageTitle()
    {
        var content = File.ReadAllText(GetRepoPath(@"HV-Travel.Web\Controllers\PublicToursController.cs"));

        Assert.Contains("Tour Du LÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â»ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€šÃ‚Â¹ch", content);
    }
    private static string GetRepoPath(string relativePath)
    {
        var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        return Path.Combine(repoRoot, relativePath);
    }
}



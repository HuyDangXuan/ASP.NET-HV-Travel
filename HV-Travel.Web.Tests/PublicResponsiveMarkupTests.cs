namespace HV_Travel.Web.Tests;

public class PublicResponsiveMarkupTests
{
    [Fact]
    public void PublicTheme_DefinesFluidResponsiveFoundation()
    {
        var content = File.ReadAllText(GetRepoPath(@"HV-Travel.Web\wwwroot\css\theme-ocean.css"));

        Assert.Contains("--public-fluid-display", content);
        Assert.Contains("--public-fluid-heading", content);
        Assert.Contains("--public-fluid-body", content);
        Assert.Contains(".public-page-shell", content);
        Assert.Contains(".public-hero-title", content);
        Assert.Contains(".public-card-grid", content);
        Assert.Contains("overflow-x: clip", content);
        Assert.Contains("clamp(", content);
    }

    [Fact]
    public void PublicTheme_DefinesSharedB2CTopOfPageHooks()
    {
        var content = File.ReadAllText(GetRepoPath(@"HV-Travel.Web\wwwroot\css\theme-ocean.css"));

        Assert.Contains(".public-page-hero", content);
        Assert.Contains(".public-top-section", content);
        Assert.Contains(".public-top-section--hero", content);
        Assert.Contains(".public-top-section--compact", content);
        Assert.Contains(".public-top-section-title", content);
        Assert.Contains(".public-top-section-lead", content);
        Assert.Contains(".public-header-compact", content);
        Assert.Contains(".public-page-hero-shell", content);
        Assert.Contains(".public-page-hero-badge", content);
        Assert.Contains(".public-page-hero-copy", content);
        Assert.Contains(".public-page-hero-actions", content);
        Assert.Contains(".public-nav-utility-link", content);
        Assert.Contains(".public-nav-action-cta", content);
    }

    [Fact]
    public void LayoutPublic_UsesResponsiveFoundationHooks()
    {
        var content = File.ReadAllText(GetRepoPath(@"HV-Travel.Web\Views\Shared\_LayoutPublic.cshtml"));

        Assert.Contains("public-page-shell", content);
        Assert.Contains("public-site-header", content);
        Assert.Contains("public-brand-lockup", content);
        Assert.Contains("public-primary-nav", content);
        Assert.Contains("public-secondary-nav", content);
        Assert.Contains("public-nav-more-toggle", content);
        Assert.Contains("public-account-menu", content);
        Assert.Contains("public-mobile-drawer", content);
        Assert.Contains("public-nav-actions", content);
        Assert.Contains("public-footer-grid", content);
    }

    [Fact]
    public void LayoutPublic_UsesSharedB2CHeaderAccentHooks()
    {
        var content = File.ReadAllText(GetRepoPath(@"HV-Travel.Web\Views\Shared\_LayoutPublic.cshtml"));

        Assert.Contains("public-header-compact", content);
        Assert.Contains("public-brand-title", content);
        Assert.Contains("public-nav-utility-link", content);
        Assert.Contains("public-nav-action-cta", content);
        Assert.Contains("public-header-control", content);
        Assert.Contains("public-header-panel", content);
        Assert.Contains("public-header-icon-button", content);
        Assert.Contains("public-mobile-nav-link", content);
    }

    [Fact]
    public void PublicHeader_DefinesDedicatedDarkModeHooks()
    {
        var content = File.ReadAllText(GetRepoPath(@"HV-Travel.Web\wwwroot\css\theme-ocean.css"));

        Assert.Contains(".public-header-shell", content);
        Assert.Contains(".public-header-panel", content);
        Assert.Contains(".public-header-control", content);
        Assert.Contains(".public-header-icon-button", content);
        Assert.Contains(".public-mobile-nav-link", content);
        Assert.Contains("body.public-page-shell.dark .public-header-panel", content);
        Assert.Contains("body.public-page-shell.dark .public-header-control", content);
        Assert.Contains("body.public-page-shell.dark .public-mobile-nav-link", content);
    }

    [Fact]
    public void PublicHeader_UsesUniformSolidDarkSurfacesInDarkMode()
    {
        var content = File.ReadAllText(GetRepoPath(@"HV-Travel.Web\wwwroot\css\theme-ocean.css"));

        Assert.Contains("html.dark body.public-page-shell .public-site-header", content);
        Assert.Contains("html.dark body.public-page-shell .public-header-control", content);
        Assert.Contains("html.dark body.public-page-shell .public-header-panel", content);
        Assert.Contains("html.dark body.public-page-shell .public-mobile-drawer", content);
        Assert.Contains("background: rgba(7, 17, 27, 0.96);", content);
        Assert.Contains("background: rgba(13, 27, 42, 0.96);", content);
        Assert.Contains("background: rgba(13, 27, 42, 0.98);", content);
        Assert.Contains("background: rgba(11, 92, 115, 0.18);", content);
    }

    [Fact]
    public void PublicHeader_RemovesDividerLinesInDarkMode()
    {
        var content = File.ReadAllText(GetRepoPath(@"HV-Travel.Web\wwwroot\css\theme-ocean.css"));

        Assert.Contains("border-bottom: none;", content);
        Assert.Contains("border-top: none;", content);
    }

    [Fact]
    public void PublicHeader_RemovesShadowArtifactsInDarkMode()
    {
        var content = File.ReadAllText(GetRepoPath(@"HV-Travel.Web\wwwroot\css\theme-ocean.css"));
        var layout = File.ReadAllText(GetRepoPath(@"HV-Travel.Web\Views\Shared\_LayoutPublic.cshtml"));

        Assert.Contains("html.dark body.public-page-shell .public-site-header::before", content);
        Assert.Contains("display: none;", content);
        Assert.Contains("box-shadow: none;", content);
        Assert.Contains("const syncHeaderScrollState = () =>", layout);
        Assert.Contains("header.classList.add('shadow-lg', 'shadow-slate-200/20');", layout);
        Assert.DoesNotContain("header.classList.add('shadow-lg', 'shadow-slate-200/20', 'dark:shadow-slate-900/30');", layout);
    }

    [Fact]
    public void CorePublicViews_AdoptFluidTypographyHooks()
    {
        AssertContains(@"HV-Travel.Web\Views\Home\Index.cshtml", "public-hero-title");
        AssertContains(@"HV-Travel.Web\Views\Home\Index.cshtml", "public-section-title");
        AssertContains(@"HV-Travel.Web\Views\Home\About.cshtml", "public-hero-title");
        AssertContains(@"HV-Travel.Web\Views\Home\Contact.cshtml", "public-form-card");
        AssertContains(@"HV-Travel.Web\Views\PublicTours\Index.cshtml", "public-search-shell");
        AssertContains(@"HV-Travel.Web\Views\PublicTours\Details.cshtml", "public-detail-sidebar");
        AssertContains(@"HV-Travel.Web\Views\CustomerAuth\Login.cshtml", "public-auth-title");
        AssertContains(@"HV-Travel.Web\Views\CustomerAuth\Register.cshtml", "public-auth-title");
    }

    [Fact]
    public void B2CTopPages_UseSharedHeroHooks()
    {
        var heroViews = new[]
        {
            @"HV-Travel.Web\Views\Home\About.cshtml",
            @"HV-Travel.Web\Views\Home\Contact.cshtml",
            @"HV-Travel.Web\Views\PublicTours\Index.cshtml",
            @"HV-Travel.Web\Views\Destinations\Index.cshtml",
            @"HV-Travel.Web\Views\Promotions\Index.cshtml",
            @"HV-Travel.Web\Views\Services\Index.cshtml",
            @"HV-Travel.Web\Views\Inspiration\Index.cshtml",
            @"HV-Travel.Web\Views\BookingLookup\Index.cshtml"
        };

        foreach (var heroView in heroViews)
        {
            AssertContains(heroView, "public-top-section");
            AssertContains(heroView, "public-top-section--hero");
            AssertContains(heroView, "public-top-section-title");
            AssertContains(heroView, "public-top-section-lead");
            AssertContains(heroView, "public-page-hero");
            AssertContains(heroView, "public-page-hero-shell");
            AssertContains(heroView, "public-page-hero-badge");
            AssertContains(heroView, "public-page-hero-copy");
        }
    }

    [Fact]
    public void KeyHubPages_UseCenteredHeroAlignment()
    {
        var centeredHeroViews = new[]
        {
            @"HV-Travel.Web\Views\Destinations\Index.cshtml",
            @"HV-Travel.Web\Views\Promotions\Index.cshtml",
            @"HV-Travel.Web\Views\Services\Index.cshtml",
            @"HV-Travel.Web\Views\Inspiration\Index.cshtml"
        };

        foreach (var centeredHeroView in centeredHeroViews)
        {
            AssertContains(centeredHeroView, "public-top-section-content public-page-hero-content is-centered");
        }
    }
    [Fact]
    public void DetailAndUtilityPublicViews_UseCompactTopSectionHooks()
    {
        var compactViews = new[]
        {
            @"HV-Travel.Web\Views\PublicTours\Details.cshtml",
            @"HV-Travel.Web\Views\Inspiration\Details.cshtml",
            @"HV-Travel.Web\Views\CustomerAuth\Login.cshtml",
            @"HV-Travel.Web\Views\CustomerAuth\Register.cshtml",
            @"HV-Travel.Web\Views\CustomerPortal\Index.cshtml",
            @"HV-Travel.Web\Views\Booking\Consultation.cshtml",
            @"HV-Travel.Web\Views\Booking\Create.cshtml",
            @"HV-Travel.Web\Views\Booking\Payment.cshtml",
            @"HV-Travel.Web\Views\Booking\Success.cshtml",
            @"HV-Travel.Web\Views\Booking\Failed.cshtml",
            @"HV-Travel.Web\Views\Booking\Error.cshtml"
        };

        foreach (var compactView in compactViews)
        {
            AssertContains(compactView, "public-top-section");
            AssertContains(compactView, "public-top-section--compact");
            AssertContains(compactView, "public-top-section-title");
            AssertContains(compactView, "public-top-section-lead");
        }
    }

    [Fact]
    public void BookingFlow_UsesResponsiveStepperAndSummaryLayout()
    {
        AssertContains(@"HV-Travel.Web\Views\Booking\Create.cshtml", "public-stepper");
        AssertContains(@"HV-Travel.Web\Views\Booking\Payment.cshtml", "public-stepper");
        AssertContains(@"HV-Travel.Web\Views\Booking\Success.cshtml", "public-stepper");
        AssertContains(@"HV-Travel.Web\Views\Booking\Create.cshtml", "public-summary-card");
        AssertContains(@"HV-Travel.Web\Views\Booking\Payment.cshtml", "public-summary-card");
        AssertContains(@"HV-Travel.Web\Views\Booking\Failed.cshtml", "public-action-stack");
        AssertContains(@"HV-Travel.Web\Views\Booking\Error.cshtml", "public-action-stack");
    }

    [Fact]
    public void SupportChat_UsesFluidResponsiveHooks()
    {
        var content = File.ReadAllText(GetRepoPath(@"HV-Travel.Web\Views\Shared\_SupportChatWidget.cshtml"));

        Assert.Contains("public-chat-panel", content);
        Assert.Contains("public-chat-onboarding", content);
        Assert.Contains("public-chat-thread", content);
        Assert.Contains("public-chat-bubble", content);
        Assert.Contains("public-chat-status", content);
        Assert.Contains("public-chat-composer", content);
    }

    private static void AssertContains(string relativePath, string expectedContent)
    {
        var content = File.ReadAllText(GetRepoPath(relativePath));
        Assert.Contains(expectedContent, content);
    }

    private static string GetRepoPath(string relativePath)
    {
        var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        return Path.Combine(repoRoot, relativePath);
    }
}


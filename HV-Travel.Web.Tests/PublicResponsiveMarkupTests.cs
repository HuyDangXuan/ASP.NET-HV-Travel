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
    public void LayoutPublic_UsesResponsiveFoundationHooks()
    {
        var content = File.ReadAllText(GetRepoPath(@"HV-Travel.Web\Views\Shared\_LayoutPublic.cshtml"));

        Assert.Contains("public-page-shell", content);
        Assert.Contains("public-site-header", content);
        Assert.Contains("public-brand-lockup", content);
        Assert.Contains("public-nav-actions", content);
        Assert.Contains("public-footer-grid", content);
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





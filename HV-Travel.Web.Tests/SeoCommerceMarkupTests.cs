namespace HV_Travel.Web.Tests;

public class SeoCommerceMarkupTests
{
    [Fact]
    public void LayoutPublic_DefinesCanonicalAndOpenGraphHooks()
    {
        var content = File.ReadAllText(GetRepoPath(@"HV-Travel.Web\Views\Shared\_LayoutPublic.cshtml"));

        Assert.Contains("ViewData[\"CanonicalUrl\"]", content);
        Assert.Contains("property=\"og:title\"", content);
        Assert.Contains("property=\"og:description\"", content);
        Assert.Contains("property=\"og:image\"", content);
    }

    [Fact]
    public void PublicTourDetails_UsesCommerceTrustAndStructuredDataHooks()
    {
        var content = File.ReadAllText(GetRepoPath(@"HV-Travel.Web\Views\PublicTours\Details.cshtml"));

        Assert.Contains("Model.Highlights", content);
        Assert.Contains("Model.CancellationPolicy", content);
        Assert.Contains("Model.MeetingPoint", content);
        Assert.Contains("Model.Departures", content);
        Assert.Contains("application/ld+json", content);
        Assert.Contains("verified booking", content, StringComparison.OrdinalIgnoreCase);
    }

    private static string GetRepoPath(string relativePath)
    {
        var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        return Path.Combine(repoRoot, relativePath);
    }
}

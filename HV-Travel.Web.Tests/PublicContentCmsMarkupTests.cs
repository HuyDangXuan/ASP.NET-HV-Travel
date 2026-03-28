namespace HV_Travel.Web.Tests;

public class PublicContentCmsMarkupTests
{
    [Fact]
    public void NewB2CPages_Load_Sections_From_PublicContentService()
    {
        AssertContains(@"HV-Travel.Web\Views\Destinations\Index.cshtml", "GetPageSectionsAsync(\"destinations\")");
        AssertContains(@"HV-Travel.Web\Views\Promotions\Index.cshtml", "GetPageSectionsAsync(\"promotions\")");
        AssertContains(@"HV-Travel.Web\Views\Services\Index.cshtml", "GetPageSectionsAsync(\"services\")");
        AssertContains(@"HV-Travel.Web\Views\Inspiration\Index.cshtml", "GetPageSectionsAsync(\"inspiration\")");
        AssertContains(@"HV-Travel.Web\Views\BookingLookup\Index.cshtml", "GetPageSectionsAsync(\"bookingLookup\")");
    }

    [Fact]
    public void NewB2CPages_Bind_Cms_Sections_Without_Removing_Dynamic_Data()
    {
        var destinations = File.ReadAllText(GetRepoPath(@"HV-Travel.Web\Views\Destinations\Index.cshtml"));
        Assert.Contains("collectionsIntro", destinations);
        Assert.Contains("regionsIntro", destinations);
        Assert.Contains("Model.Collections", destinations);
        Assert.Contains("Model.Regions", destinations);

        var promotions = File.ReadAllText(GetRepoPath(@"HV-Travel.Web\Views\Promotions\Index.cshtml"));
        Assert.Contains("flashSalesIntro", promotions);
        Assert.Contains("voucherIntro", promotions);
        Assert.Contains("seasonalDealsIntro", promotions);
        Assert.Contains("Model.FlashSales", promotions);
        Assert.Contains("Model.VoucherCampaigns", promotions);
        Assert.Contains("Model.SeasonalDeals", promotions);

        var services = File.ReadAllText(GetRepoPath(@"HV-Travel.Web\Views\Services\Index.cshtml"));
        Assert.Contains("serviceCards", services);
        Assert.Contains("quoteFormIntro", services);
        Assert.Contains("Model.ServiceCards", services);

        var inspiration = File.ReadAllText(GetRepoPath(@"HV-Travel.Web\Views\Inspiration\Index.cshtml"));
        Assert.Contains("featuredIntro", inspiration);
        Assert.Contains("latestIntro", inspiration);
        Assert.Contains("Model.FeaturedArticle", inspiration);
        Assert.Contains("Model.LatestArticles", inspiration);

        var bookingLookup = File.ReadAllText(GetRepoPath(@"HV-Travel.Web\Views\BookingLookup\Index.cshtml"));
        Assert.Contains("lookupForm", bookingLookup);
        Assert.Contains("readyState", bookingLookup);
        Assert.Contains("Model.HasResult", bookingLookup);
    }

    [Fact]
    public void Utility_And_Detail_Views_Remain_Outside_AdminContent()
    {
        AssertDoesNotContain(@"HV-Travel.Web\Views\Inspiration\Details.cshtml", "GetPageSectionsAsync(");
        AssertDoesNotContain(@"HV-Travel.Web\Views\CustomerPortal\Index.cshtml", "GetPageSectionsAsync(");
        AssertDoesNotContain(@"HV-Travel.Web\Views\CustomerAuth\Login.cshtml", "GetPageSectionsAsync(");
        AssertDoesNotContain(@"HV-Travel.Web\Views\CustomerAuth\Register.cshtml", "GetPageSectionsAsync(");
    }

    private static void AssertContains(string relativePath, string expectedContent)
    {
        var content = File.ReadAllText(GetRepoPath(relativePath));
        Assert.Contains(expectedContent, content);
    }

    private static void AssertDoesNotContain(string relativePath, string unexpectedContent)
    {
        var content = File.ReadAllText(GetRepoPath(relativePath));
        Assert.DoesNotContain(unexpectedContent, content);
    }

    private static string GetRepoPath(string relativePath)
    {
        var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        return Path.Combine(repoRoot, relativePath);
    }
}

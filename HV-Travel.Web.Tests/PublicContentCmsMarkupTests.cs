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
    public void HomeCarousel_Uses_Cms_Section_Admin_Partial_And_Public_Script()
    {
        var home = File.ReadAllText(GetRepoPath(@"HV-Travel.Web\Views\Home\Index.cshtml"));
        var adminContent = File.ReadAllText(GetRepoPath(@"HV-Travel.Web\Areas\Admin\Views\Content\Index.cshtml"));

        Assert.Contains("var carousel = sections.GetSection(\"carousel\")", home);
        Assert.Contains("PartialAsync(\"_HomeCarousel\"", home);
        Assert.Contains("~/js/home-carousel.js", home);

        Assert.Contains("PartialAsync(\"_HomeCarouselSectionEditor\"", adminContent);
        Assert.Contains("contentSection.SectionKey == \"carousel\"", adminContent);
        Assert.Contains("data-home-carousel-editor", adminContent);
    }

    [Fact]
    public void CmsManagedPublicViews_Use_ContentPresentationViewHelper_Hooks()
    {
        var sectionManagedViews = new[]
        {
            @"HV-Travel.Web\Views\Home\Index.cshtml",
            @"HV-Travel.Web\Views\Home\About.cshtml",
            @"HV-Travel.Web\Views\Home\Contact.cshtml",
            @"HV-Travel.Web\Views\PublicTours\Index.cshtml",
            @"HV-Travel.Web\Views\Destinations\Index.cshtml",
            @"HV-Travel.Web\Views\Promotions\Index.cshtml",
            @"HV-Travel.Web\Views\Services\Index.cshtml",
            @"HV-Travel.Web\Views\Inspiration\Index.cshtml",
            @"HV-Travel.Web\Views\Booking\Consultation.cshtml",
            @"HV-Travel.Web\Views\BookingLookup\Index.cshtml"
        };

        foreach (var view in sectionManagedViews)
        {
            AssertContains(view, "ContentPresentationViewHelper");
            AssertContains(view, "GetFieldTextStyle(");
        }

        var fieldSliceViews = new[]
        {
            @"HV-Travel.Web\Views\Booking\Success.cshtml",
            @"HV-Travel.Web\Views\Booking\Failed.cshtml",
            @"HV-Travel.Web\Views\Booking\Error.cshtml"
        };

        foreach (var view in fieldSliceViews)
        {
            AssertContains(view, "ContentPresentationViewHelper");
            AssertContains(view, "GetFieldTextStyle(");
        }
    }

    [Fact]
    public void Utility_And_Detail_Views_Remain_Outside_AdminContent()
    {
        AssertDoesNotContain(@"HV-Travel.Web\Views\Inspiration\Details.cshtml", "GetPageSectionsAsync(");
        AssertDoesNotContain(@"HV-Travel.Web\Views\CustomerPortal\Index.cshtml", "GetPageSectionsAsync(");
        AssertDoesNotContain(@"HV-Travel.Web\Views\CustomerAuth\Login.cshtml", "GetPageSectionsAsync(");
        AssertDoesNotContain(@"HV-Travel.Web\Views\CustomerAuth\Register.cshtml", "GetPageSectionsAsync(");
    }


    [Fact]
    public void AdminContentEditor_Exposes_Metadata_And_Field_Visibility_Toggles()
    {
        var adminContent = File.ReadAllText(GetRepoPath(@"HV-Travel.Web\Areas\Admin\Views\Content\Index.cshtml"));

        Assert.Contains("SiteSettings.Groups[i].IsTitleEnabled", adminContent);
        Assert.Contains("SiteSettings.Groups[i].IsDescriptionEnabled", adminContent);
        Assert.Contains("SiteSettings.Groups[i].Fields[j].IsEnabled", adminContent);
        Assert.Contains("Sections[i].IsTitleEnabled", adminContent);
        Assert.Contains("Sections[i].IsDescriptionEnabled", adminContent);
        Assert.Contains("Sections[i].Fields[j].IsEnabled", adminContent);
        Assert.Contains("SupportsFieldVisibilityToggle(", adminContent);

    }

    [Fact]
    public void PublicLayoutAndHome_Use_Visibility_Aware_Cms_Text_Hooks()
    {
        var layout = File.ReadAllText(GetRepoPath(@"HV-Travel.Web\Views\Shared\_LayoutPublic.cshtml"));
        var home = File.ReadAllText(GetRepoPath(@"HV-Travel.Web\Views\Home\Index.cshtml"));
        var helpers = File.ReadAllText(GetRepoPath(@"HV-Travel.Web\Services\ContentAccessExtensions.cs"));

        Assert.Contains("GetVisibleFieldValue", helpers);
        Assert.Contains("HasVisibleFieldValue", helpers);
        Assert.Contains("GetVisibleTitle", helpers);
        Assert.Contains("GetVisibleDescription", helpers);

        Assert.Contains("primaryNavItems = primaryNavItems.Where(item => !string.IsNullOrWhiteSpace(item.Label)).ToArray();", layout);
        Assert.Contains("!string.IsNullOrWhiteSpace(contactTitle)", layout);
        Assert.Contains("!string.IsNullOrWhiteSpace(featuredTitle)", home);
        Assert.Contains("!string.IsNullOrWhiteSpace(commitmentsTitle)", home);
    }
    [Fact]
    public void HomeCarousel_AdminEditor_Allows_Relative_And_Absolute_Link_Targets()
    {
        var partial = File.ReadAllText(GetRepoPath(@"HV-Travel.Web\Areas\Admin\Views\Content\_HomeCarouselSectionEditor.cshtml"));
        var helpers = File.ReadAllText(GetRepoPath(@"HV-Travel.Web\Services\ContentAccessExtensions.cs"));
        var service = File.ReadAllText(GetRepoPath(@"HV-Travel.Web\Services\PublicContentService.cs"));
        var publicCarousel = File.ReadAllText(GetRepoPath(@"HV-Travel.Web\Views\Home\_HomeCarousel.cshtml"));

        Assert.Contains("placeholder=\"/PublicTours, /Destinations hoặc https://example.com\"", partial);
        Assert.Contains("Link nội bộ bắt đầu bằng / hoặc URL đầy đủ https:// đều hợp lệ.", partial);
        Assert.DoesNotContain("type=\"url\" name=\"@InputName(sectionIndex, linkIndex)\"", partial);
        Assert.Contains("NormalizeCarouselLink", helpers);
        Assert.Contains("NormalizeCarouselLinkValue", service);
        Assert.Contains("NormalizeCarouselLink(slide.LinkUrl)", publicCarousel);
    }
    [Fact]
    public void GlobalHeaderAndPublicTours_Use_New_Cms_Field_Hooks()
    {
        var defaults = File.ReadAllText(GetRepoPath(@"HV-Travel.Web\Services\PublicContentDefaults.cs"));
        var layout = File.ReadAllText(GetRepoPath(@"HV-Travel.Web\Views\Shared\_LayoutPublic.cshtml"));
        var publicTours = File.ReadAllText(GetRepoPath(@"HV-Travel.Web\Views\PublicTours\Index.cshtml"));
        var filter = File.ReadAllText(GetRepoPath(@"HV-Travel.Web\Views\PublicTours\_PublicToursFilter.cshtml"));
        var home = File.ReadAllText(GetRepoPath(@"HV-Travel.Web\Views\Home\Index.cshtml"));

        Assert.Contains("navDestinationsLabel", defaults);
        Assert.Contains("bookingLookupLabel", defaults);
        Assert.Contains("moreLabel", defaults);
        Assert.Contains("openPortalLabel", defaults);
        Assert.Contains("logoutLabel", defaults);
        Assert.Contains("collectionChips", defaults);
        Assert.Contains("filterPanel", defaults);
        Assert.Contains("resultsPanel", defaults);
        Assert.Contains("emptyStateText", defaults);
        Assert.Contains("eyebrowText", defaults);

        Assert.Contains("!string.IsNullOrWhiteSpace(moreLabel)", layout);
        Assert.Contains("!string.IsNullOrWhiteSpace(bookingLookupLabel)", layout);
        Assert.Contains("!string.IsNullOrWhiteSpace(openPortalLabel)", layout);
        Assert.Contains("!string.IsNullOrWhiteSpace(logoutLabel)", layout);
        Assert.Contains("!string.IsNullOrWhiteSpace(registerLabel)", layout);
        Assert.Contains("!string.IsNullOrWhiteSpace(loginLabel)", layout);

        Assert.Contains("collectionChips", publicTours);
        Assert.Contains("resultsPanel", publicTours);
        Assert.Contains("emptyStateCtaText", publicTours);
        Assert.Contains("data-empty-text=\"@wishlistEmptyText\"", publicTours);
        Assert.Contains("data-empty-text=\"@recentEmptyText\"", publicTours);
        Assert.Contains("FilterPanel", publicTours);
        Assert.Contains("availableOnlyLabelText", filter);
        Assert.Contains("applyButtonText", filter);
        Assert.Contains("commitmentsEyebrowText", home);
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







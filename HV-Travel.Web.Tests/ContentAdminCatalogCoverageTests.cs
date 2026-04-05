using HVTravel.Web.Models;
using HVTravel.Web.Services;

namespace HV_Travel.Web.Tests;

public class ContentAdminCatalogCoverageTests
{
    [Fact]
    public void ContentAdminCatalog_Includes_NewB2CPages()
    {
        var tabKeys = ContentAdminCatalog.GetTabs().Select(tab => tab.Key).ToList();

        Assert.Contains("destinations", tabKeys);
        Assert.Contains("promotions", tabKeys);
        Assert.Contains("services", tabKeys);
        Assert.Contains("inspiration", tabKeys);
        Assert.Contains("bookingLookup", tabKeys);
        Assert.Contains("publicTourDetails", tabKeys);
        Assert.Contains("inspirationDetails", tabKeys);
        Assert.Contains("customerLogin", tabKeys);
        Assert.Contains("customerRegister", tabKeys);
        Assert.Contains("customerPortal", tabKeys);
    }

    [Theory]
    [InlineData("destinations", "destinations", new[] { "hero", "collectionsIntro", "regionsIntro" })]
    [InlineData("promotions", "promotions", new[] { "hero", "flashSalesIntro", "voucherIntro", "seasonalDealsIntro" })]
    [InlineData("services", "services", new[] { "hero", "serviceCards", "quoteFormIntro" })]
    [InlineData("inspiration", "inspiration", new[] { "hero", "featuredIntro", "latestIntro" })]
    [InlineData("bookingLookup", "bookingLookup", new[] { "hero", "lookupForm", "readyState" })]
    [InlineData("publicTourDetails", "publicTourDetails", new[] { "hero", "highlights", "overview", "inclusions", "schedule", "policies", "departures", "bookingPanel", "relatedTours" })]
    [InlineData("inspirationDetails", "inspirationDetails", new[] { "hero", "body", "tags" })]
    [InlineData("customerLogin", "customerLogin", new[] { "hero", "featureCards", "formIntro", "registerPrompt" })]
    [InlineData("customerRegister", "customerRegister", new[] { "hero", "benefits", "formIntro", "loginPrompt" })]
    [InlineData("customerPortal", "customerPortal", new[] { "hero", "stats", "bookingPanel", "reviewPanel", "voucherPanel", "travellerPanel", "notificationsPanel" })]
    public void ContentAdminCatalog_Resolves_NewB2CEditors(
        string tabKey,
        string pageKey,
        string[] expectedSectionKeys)
    {
        var editor = ContentAdminCatalog.Resolve(tabKey, null);

        Assert.Equal(tabKey, editor.TabKey);
        Assert.Equal(pageKey, editor.PageKey);
        Assert.Equal(expectedSectionKeys, editor.Sections.Select(section => section.SectionKey).ToArray());
    }

    [Theory]
    [InlineData("create", new[] { "createHero", "createStepper", "travellerForm", "pricingPanel" })]
    [InlineData("payment", new[] { "paymentHero", "paymentStepper", "paymentMethods", "transferProof", "orderSummary", "paymentTimeline" })]
    public void ContentAdminCatalog_Resolves_NewBookingFlowEditors(
        string subtabKey,
        string[] expectedSectionKeys)
    {
        var editor = ContentAdminCatalog.Resolve("booking", subtabKey);

        Assert.Equal("booking", editor.TabKey);
        Assert.Equal("booking", editor.PageKey);
        Assert.Equal(subtabKey, editor.SubtabKey);
        Assert.Equal(expectedSectionKeys, editor.Sections.Select(section => section.SectionKey).ToArray());
    }

    [Fact]
    public void ContentAdminEditorDefinition_NoLongerExposesPreviewMetadata()
    {
        var propertyNames = typeof(ContentAdminEditorDefinition)
            .GetProperties()
            .Select(property => property.Name)
            .ToHashSet(StringComparer.Ordinal);

        Assert.DoesNotContain("PreviewTarget", propertyNames);
        Assert.DoesNotContain("PreviewUnavailableReason", propertyNames);
    }

    [Theory]
    [InlineData("destinations", new[] { "hero", "collectionsIntro", "regionsIntro" })]
    [InlineData("promotions", new[] { "hero", "flashSalesIntro", "voucherIntro", "seasonalDealsIntro" })]
    [InlineData("services", new[] { "hero", "serviceCards", "quoteFormIntro" })]
    [InlineData("inspiration", new[] { "hero", "featuredIntro", "latestIntro" })]
    [InlineData("bookingLookup", new[] { "hero", "lookupForm", "readyState" })]
    [InlineData("publicTourDetails", new[] { "hero", "highlights", "overview", "inclusions", "schedule", "policies", "departures", "bookingPanel", "relatedTours" })]
    [InlineData("inspirationDetails", new[] { "hero", "body", "tags" })]
    [InlineData("customerLogin", new[] { "hero", "featureCards", "formIntro", "registerPrompt" })]
    [InlineData("customerRegister", new[] { "hero", "benefits", "formIntro", "loginPrompt" })]
    [InlineData("customerPortal", new[] { "hero", "stats", "bookingPanel", "reviewPanel", "voucherPanel", "travellerPanel", "notificationsPanel" })]
    public void PublicContentDefaults_Define_Inventory_And_SeedSections_For_NewB2CPages(
        string pageKey,
        string[] expectedSectionKeys)
    {
        Assert.True(PublicContentDefaults.Inventory.ContainsKey(pageKey));
        Assert.Equal(expectedSectionKeys, PublicContentDefaults.Inventory[pageKey]);

        var sections = PublicContentDefaults.CreateSectionsForPage(pageKey);

        Assert.Equal(expectedSectionKeys, sections.Select(section => section.SectionKey).ToArray());
    }

    [Fact]
    public void PublicContentDefaults_Extend_Booking_Inventory_For_Create_And_Payment()
    {
        Assert.True(PublicContentDefaults.Inventory.ContainsKey("booking"));
        Assert.Equal(
            new[]
            {
                "consultationHero",
                "consultationBenefits",
                "statusCopy",
                "createHero",
                "createStepper",
                "travellerForm",
                "pricingPanel",
                "paymentHero",
                "paymentStepper",
                "paymentMethods",
                "transferProof",
                "orderSummary",
                "paymentTimeline"
            },
            PublicContentDefaults.Inventory["booking"]);
    }

    [Fact]
    public void HomeCarousel_SourceContracts_Are_Defined_In_ContentFiles()
    {
        var defaults = File.ReadAllText(GetRepoPath(@"HV-Travel.Web\Services\PublicContentDefaults.cs"));
        var catalog = File.ReadAllText(GetRepoPath(@"HV-Travel.Web\Services\ContentAdminCatalog.cs"));

        Assert.Contains("[\"home\"] = new() { \"hero\", \"carousel\", \"stats\", \"featuredToursIntro\", \"commitments\", \"finalCta\" }", defaults);
        Assert.Contains("Section(\"home\", \"carousel\", \"Carousel trang chủ\", 2, new List<ContentField>", defaults);
        Assert.Contains("Text(\"slide1SourceType\", \"Nguồn ảnh slide 1\", \"external\")", defaults);
        Assert.Contains("Url(\"slide5ImageUrl\", \"Ảnh slide 5\", \"https://picsum.photos/id/1039/1600/900\")", defaults);
        Assert.Contains("Url(\"slide5LinkUrl\", \"Link slide 5\", \"/Inspiration\")", defaults);
        Assert.Contains("\"6 section chính của trang chủ.\"", catalog);
        Assert.Contains("Section(\"carousel\", \"Carousel trang chủ\", \"5 slide ảnh marketing nằm ngay dưới hero\")", catalog);
    }

    private static string GetRepoPath(string relativePath)
    {
        var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        return Path.Combine(repoRoot, relativePath);
    }
}

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
    }

    [Theory]
    [InlineData("destinations", "destinations", new[] { "hero", "collectionsIntro", "regionsIntro" })]
    [InlineData("promotions", "promotions", new[] { "hero", "flashSalesIntro", "voucherIntro", "seasonalDealsIntro" })]
    [InlineData("services", "services", new[] { "hero", "serviceCards", "quoteFormIntro" })]
    [InlineData("inspiration", "inspiration", new[] { "hero", "featuredIntro", "latestIntro" })]
    [InlineData("bookingLookup", "bookingLookup", new[] { "hero", "lookupForm", "readyState" })]
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
    public void PublicContentDefaults_Define_Inventory_And_SeedSections_For_NewB2CPages(
        string pageKey,
        string[] expectedSectionKeys)
    {
        Assert.True(PublicContentDefaults.Inventory.ContainsKey(pageKey));
        Assert.Equal(expectedSectionKeys, PublicContentDefaults.Inventory[pageKey]);

        var sections = PublicContentDefaults.CreateSectionsForPage(pageKey);

        Assert.Equal(expectedSectionKeys, sections.Select(section => section.SectionKey).ToArray());
    }
}

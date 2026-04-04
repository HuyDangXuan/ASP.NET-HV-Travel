using HVTravel.Domain.Entities;
using HVTravel.Web.Services;
using HV_Travel.Web.Tests.TestSupport;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;

namespace HV_Travel.Web.Tests;

public class ContentPresentationContractTests
{
    [Fact]
    public void ContentEntities_ExposePresentationContracts()
    {
        var sectionPresentationProperty = typeof(ContentSection).GetProperty("Presentation");
        var fieldStyleProperty = typeof(ContentField).GetProperty("Style");

        Assert.NotNull(sectionPresentationProperty);
        Assert.NotNull(fieldStyleProperty);

        AssertTypeHasProperties(sectionPresentationProperty!.PropertyType, "Container", "EyebrowText", "TitleText", "DescriptionText");

        var containerProperty = sectionPresentationProperty.PropertyType.GetProperty("Container");
        Assert.NotNull(containerProperty);
        AssertTypeHasProperties(containerProperty!.PropertyType, "Align", "BackgroundPreset", "CustomBackgroundHex");

        AssertTextStyleContract(sectionPresentationProperty.PropertyType.GetProperty("EyebrowText"));
        AssertTextStyleContract(sectionPresentationProperty.PropertyType.GetProperty("TitleText"));
        AssertTextStyleContract(sectionPresentationProperty.PropertyType.GetProperty("DescriptionText"));
        AssertTextStyleContract(fieldStyleProperty.PropertyType);
    }

    [Fact]
    public void PublicContentDefaults_SeedSections_InitializePresentationAndFieldStyles()
    {
        var heroSection = Assert.Single(PublicContentDefaults.CreateSectionsForPage("home").Where(section => section.SectionKey == "hero"));
        var firstField = Assert.IsType<ContentField>(heroSection.Fields[0]);

        var sectionPresentationProperty = typeof(ContentSection).GetProperty("Presentation");
        var fieldStyleProperty = typeof(ContentField).GetProperty("Style");

        Assert.NotNull(sectionPresentationProperty);
        Assert.NotNull(fieldStyleProperty);
        Assert.NotNull(sectionPresentationProperty!.GetValue(heroSection));
        Assert.NotNull(fieldStyleProperty!.GetValue(firstField));
    }

    [Fact]
    public async Task PublicContentService_PersistsPresentationAndFieldStyles_WhenSavingSections()
    {
        var siteSettingsRepository = new InMemoryRepository<SiteSettings>();
        var contentSectionRepository = new InMemoryRepository<ContentSection>();
        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var service = new PublicContentService(
            siteSettingsRepository,
            contentSectionRepository,
            memoryCache,
            new HttpContextAccessor());

        var heroSection = Assert.Single(PublicContentDefaults.CreateSectionsForPage("home").Where(section => section.SectionKey == "hero"));
        var badgeField = Assert.Single(heroSection.Fields.Where(field => field.Key == "badgeText"));

        AssertPropertyExists(typeof(ContentSection), "Presentation");
        AssertPropertyExists(typeof(ContentField), "Style");

        SetNestedPropertyValue(heroSection, "Presentation", "Container", "Align", "center");
        SetNestedPropertyValue(heroSection, "Presentation", "Container", "BackgroundPreset", "primary-soft");
        SetNestedPropertyValue(heroSection, "Presentation", "TitleText", "FontPreset", "Playfair Display");
        SetNestedPropertyValue(heroSection, "Presentation", "DescriptionText", "ColorPreset", "muted");
        SetNestedPropertyValue(badgeField, "Style", "Align", "right");
        SetNestedPropertyValue(badgeField, "Style", "ColorPreset", "accent");
        SetNestedPropertyValue(badgeField, "Style", "SizePreset", "xl");

        await service.SaveSectionsAsync(new[] { heroSection }, "home", null);

        var loadedHeroSection = Assert.Single((await service.GetPageSectionsForAdminAsync("home")).Where(section => section.SectionKey == "hero"));
        var loadedBadgeField = Assert.Single(loadedHeroSection.Fields.Where(field => field.Key == "badgeText"));

        Assert.Equal("center", GetNestedPropertyValue(loadedHeroSection, "Presentation", "Container", "Align"));
        Assert.Equal("primary-soft", GetNestedPropertyValue(loadedHeroSection, "Presentation", "Container", "BackgroundPreset"));
        Assert.Equal("Playfair Display", GetNestedPropertyValue(loadedHeroSection, "Presentation", "TitleText", "FontPreset"));
        Assert.Equal("muted", GetNestedPropertyValue(loadedHeroSection, "Presentation", "DescriptionText", "ColorPreset"));
        Assert.Equal("right", GetNestedPropertyValue(loadedBadgeField, "Style", "Align"));
        Assert.Equal("accent", GetNestedPropertyValue(loadedBadgeField, "Style", "ColorPreset"));
        Assert.Equal("xl", GetNestedPropertyValue(loadedBadgeField, "Style", "SizePreset"));
    }

    [Fact]
    public async Task PublicContentService_DefaultsMissingPresentation_WhenStoredDocumentsDoNotHaveStyleData()
    {
        var legacyHeroSection = Assert.Single(PublicContentDefaults.CreateSectionsForPage("home").Where(section => section.SectionKey == "hero"));
        var firstField = Assert.IsType<ContentField>(legacyHeroSection.Fields[0]);

        var sectionPresentationProperty = AssertPropertyExists(typeof(ContentSection), "Presentation");
        var fieldStyleProperty = AssertPropertyExists(typeof(ContentField), "Style");

        sectionPresentationProperty.SetValue(legacyHeroSection, null);
        fieldStyleProperty.SetValue(firstField, null);

        var siteSettingsRepository = new InMemoryRepository<SiteSettings>();
        var contentSectionRepository = new InMemoryRepository<ContentSection>(new[] { legacyHeroSection });
        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var service = new PublicContentService(
            siteSettingsRepository,
            contentSectionRepository,
            memoryCache,
            new HttpContextAccessor());

        var loadedHeroSection = Assert.Single((await service.GetPageSectionsForAdminAsync("home")).Where(section => section.SectionKey == "hero"));
        var loadedFirstField = Assert.IsType<ContentField>(loadedHeroSection.Fields[0]);

        Assert.NotNull(sectionPresentationProperty.GetValue(loadedHeroSection));
        Assert.NotNull(fieldStyleProperty.GetValue(loadedFirstField));
    }

    [Fact]
    public async Task PublicContentService_NormalizesMojibakeStoredSectionValues_WhenLoadingPublicPage()
    {
        var storedHeroSection = Assert.Single(PublicContentDefaults.CreateSectionsForPage("inspiration").Where(section => section.SectionKey == "hero"));
        storedHeroSection.Title = ToMojibake("Hero cẩm nang");
        storedHeroSection.Description = ToMojibake("Khu vực nội dung giúp đội marketing làm hub cho bài viết nổi bật.");

        var titleField = Assert.Single(storedHeroSection.Fields.Where(field => field.Key == "title"));
        titleField.Value = ToMojibake("Cẩm nang, visa tips, mùa lễ hội và những landing SEO có thể tự quản trị.");

        var siteSettingsRepository = new InMemoryRepository<SiteSettings>();
        var contentSectionRepository = new InMemoryRepository<ContentSection>(new[] { storedHeroSection });
        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var service = new PublicContentService(
            siteSettingsRepository,
            contentSectionRepository,
            memoryCache,
            new HttpContextAccessor());

        var sections = await service.GetPageSectionsAsync("inspiration");
        var heroSection = Assert.IsType<ContentSection>(sections["hero"]);
        var loadedTitleField = Assert.Single(heroSection.Fields.Where(field => field.Key == "title"));

        Assert.Equal("Hero cẩm nang", heroSection.Title);
        Assert.Equal("Khu vực nội dung giúp đội marketing làm hub cho bài viết nổi bật.", heroSection.Description);
        Assert.Equal("Cẩm nang, visa tips, mùa lễ hội và những landing SEO có thể tự quản trị.", loadedTitleField.Value);
    }

    [Fact]
    public async Task PublicContentService_NormalizesMojibakeStoredSiteSettings_WhenLoadingSettings()
    {
        var storedSiteSettings = PublicContentDefaults.CreateSiteSettings();
        var headerGroup = Assert.Single(storedSiteSettings.Groups.Where(group => group.GroupKey == "header"));
        headerGroup.Title = ToMojibake("Header");
        var navToursField = Assert.Single(headerGroup.Fields.Where(field => field.Key == "navToursLabel"));
        navToursField.Value = ToMojibake("Tour du lịch");
        navToursField.Label = ToMojibake("Nhãn tour");

        var siteSettingsRepository = new InMemoryRepository<SiteSettings>(new[] { storedSiteSettings });
        var contentSectionRepository = new InMemoryRepository<ContentSection>();
        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var service = new PublicContentService(
            siteSettingsRepository,
            contentSectionRepository,
            memoryCache,
            new HttpContextAccessor());

        var settings = await service.GetSiteSettingsAsync();
        var loadedHeaderGroup = Assert.Single(settings.Groups.Where(group => group.GroupKey == "header"));
        var loadedNavToursField = Assert.Single(loadedHeaderGroup.Fields.Where(field => field.Key == "navToursLabel"));

        Assert.Equal("Header", loadedHeaderGroup.Title);
        Assert.Equal("Nhãn tour", loadedNavToursField.Label);
        Assert.Equal("Tour du lịch", loadedNavToursField.Value);
    }
    private static void AssertTextStyleContract(Type? type)
    {
        Assert.NotNull(type);
        AssertTypeHasProperties(
            type!,
            "Align",
            "FontPreset",
            "CustomFontFamily",
            "SizePreset",
            "CustomSizeValue",
            "CustomSizeUnit",
            "ColorPreset",
            "CustomColorHex");
    }

    private static void AssertTextStyleContract(System.Reflection.PropertyInfo? property)
    {
        Assert.NotNull(property);
        AssertTextStyleContract(property!.PropertyType);
    }

    private static void AssertTypeHasProperties(Type type, params string[] propertyNames)
    {
        var actualProperties = type
            .GetProperties()
            .Select(property => property.Name)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var propertyName in propertyNames)
        {
            Assert.Contains(propertyName, actualProperties);
        }
    }

    private static System.Reflection.PropertyInfo AssertPropertyExists(Type type, string propertyName)
    {
        var property = type.GetProperty(propertyName);
        Assert.NotNull(property);
        return property!;
    }

    private static void SetNestedPropertyValue(object target, params object[] pathAndValue)
    {
        var value = pathAndValue[^1];
        var propertyPath = pathAndValue.Take(pathAndValue.Length - 1).Cast<string>().ToArray();
        var parent = WalkToParent(target, propertyPath);
        var property = parent.GetType().GetProperty(propertyPath[^1]);
        Assert.NotNull(property);
        property!.SetValue(parent, value);
    }

    private static object? GetNestedPropertyValue(object target, params string[] propertyPath)
    {
        object? current = target;
        foreach (var segment in propertyPath)
        {
            Assert.NotNull(current);
            var property = current!.GetType().GetProperty(segment);
            Assert.NotNull(property);
            current = property!.GetValue(current);
        }

        return current;
    }

    private static object WalkToParent(object target, IReadOnlyList<string> propertyPath)
    {
        object current = target;
        for (var index = 0; index < propertyPath.Count - 1; index++)
        {
            var property = current.GetType().GetProperty(propertyPath[index]);
            Assert.NotNull(property);
            current = property!.GetValue(current) ?? throw new Xunit.Sdk.XunitException($"Property '{propertyPath[index]}' was null.");
        }

        return current;
    }

    private static string ToMojibake(string value)
    {
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
        var bytes = System.Text.Encoding.UTF8.GetBytes(value);
        return System.Text.Encoding.GetEncoding(1252).GetString(bytes);
    }
}


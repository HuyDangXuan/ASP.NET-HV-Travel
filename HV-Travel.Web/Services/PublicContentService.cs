using System.Text;
using HVTravel.Domain.Entities;
using HVTravel.Domain.Interfaces;
using HVTravel.Web.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;

namespace HVTravel.Web.Services;

public class PublicContentService : IPublicContentService
{
    private static readonly string[] MojibakeMarkers =
    {
        "Ãƒ", "Ã‚", "Ã„", "Ã†", "Ã¡Âº", "Ã¡Â»", "Ã¢â‚¬â„¢", "Ã¢â‚¬Å“", "Ã¢â‚¬", "ï¿½"
    };

    private const string SiteSettingsCacheKey = "public-content-site-settings";
    private readonly IRepository<SiteSettings> _siteSettingsRepository;
    private readonly IRepository<ContentSection> _contentSectionRepository;
    private readonly IMemoryCache _memoryCache;
    private readonly IHttpContextAccessor _httpContextAccessor;

    static PublicContentService()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public PublicContentService(
        IRepository<SiteSettings> siteSettingsRepository,
        IRepository<ContentSection> contentSectionRepository,
        IMemoryCache memoryCache,
        IHttpContextAccessor httpContextAccessor)
    {
        _siteSettingsRepository = siteSettingsRepository;
        _contentSectionRepository = contentSectionRepository;
        _memoryCache = memoryCache;
        _httpContextAccessor = httpContextAccessor;
    }

    public IReadOnlyList<ContentTabOption> GetTabs() => ContentAdminCatalog.GetTabs();

    public Dictionary<string, List<string>> GetEditableInventory() => PublicContentDefaults.Inventory;

    public ContentAdminEditorDefinition ResolveAdminEditor(string? tab, string? subtab) => ContentAdminCatalog.Resolve(tab, subtab);

    public async Task<SiteSettings> GetSiteSettingsAsync()
    {
        if (_memoryCache.TryGetValue(SiteSettingsCacheKey, out SiteSettings? cached) && cached != null)
        {
            return cached;
        }

        var defaults = PublicContentDefaults.CreateSiteSettings();
        var stored = (await _siteSettingsRepository.FindAsync(s => s.SettingsKey == "default")).FirstOrDefault();
        var merged = MergeSiteSettings(defaults, stored);

        _memoryCache.Set(SiteSettingsCacheKey, merged, TimeSpan.FromMinutes(5));
        return merged;
    }

    public async Task<IReadOnlyDictionary<string, ContentSection>> GetPageSectionsAsync(string pageKey)
    {
        var cacheKey = $"public-content-page-{pageKey}";
        if (_memoryCache.TryGetValue(cacheKey, out Dictionary<string, ContentSection>? cached) && cached != null)
        {
            return cached;
        }

        var defaults = PublicContentDefaults.CreateSectionsForPage(pageKey);
        var storedSections = (await _contentSectionRepository.FindAsync(s => s.PageKey == pageKey && s.IsPublished)).ToList();

        var merged = defaults
            .Select(defaultSection => MergeSection(defaultSection, storedSections.FirstOrDefault(s => s.SectionKey == defaultSection.SectionKey)))
            .OrderBy(section => section.DisplayOrder)
            .ToDictionary(section => section.SectionKey, section => section);

        _memoryCache.Set(cacheKey, merged, TimeSpan.FromMinutes(5));
        return merged;
    }

    public async Task<List<ContentSection>> GetPageSectionsForAdminAsync(string pageKey)
    {
        var defaults = PublicContentDefaults.CreateSectionsForPage(pageKey);
        var storedSections = (await _contentSectionRepository.FindAsync(s => s.PageKey == pageKey)).ToList();

        return defaults
            .Select(defaultSection => MergeSection(defaultSection, storedSections.FirstOrDefault(s => s.SectionKey == defaultSection.SectionKey)))
            .OrderBy(section => section.DisplayOrder)
            .ToList();
    }

    public async Task SaveSiteSettingsAsync(SiteSettings siteSettings)
    {
        var existing = (await _siteSettingsRepository.FindAsync(s => s.SettingsKey == "default")).FirstOrDefault();
        siteSettings.SettingsKey = "default";
        siteSettings.UpdatedAt = DateTime.UtcNow;

        foreach (var group in siteSettings.Groups)
        {
            group.Title = NormalizeText(group.Title);
            group.Description = NormalizeText(group.Description);

            foreach (var field in group.Fields)
            {
                field.Value = NormalizeText(field.Value);
                field.Label = NormalizeText(field.Label);
                field.Placeholder = NormalizeText(field.Placeholder);
                ContentPresentationDefaults.EnsureField(field);
                field.Style = ContentPresentationSchema.SanitizeTextStyle(field.Style);
            }
        }

        if (existing == null)
        {
            await _siteSettingsRepository.AddAsync(siteSettings);
        }
        else
        {
            siteSettings.Id = existing.Id;
            await _siteSettingsRepository.UpdateAsync(existing.Id, siteSettings);
        }

        InvalidateCache();
    }

    public async Task SaveSectionsAsync(IEnumerable<ContentSection> sections, string tab, string? subtab)
    {
        var editor = ResolveAdminEditor(tab, subtab);
        if (editor.TabKey == "site")
        {
            return;
        }

        var postedSections = sections.ToList();
        var defaults = PublicContentDefaults.CreateSectionsForPage(editor.PageKey);
        var storedSections = (await _contentSectionRepository.FindAsync(s => s.PageKey == editor.PageKey)).ToList();

        foreach (var sectionDefinition in editor.Sections)
        {
            var postedSection = postedSections.FirstOrDefault(section => section.SectionKey == sectionDefinition.SectionKey);
            var defaultSection = defaults.FirstOrDefault(section => section.SectionKey == sectionDefinition.SectionKey);
            if (postedSection == null || defaultSection == null)
            {
                continue;
            }

            var existing = storedSections.FirstOrDefault(section => section.SectionKey == sectionDefinition.SectionKey);
            var mergedSection = MergeSection(defaultSection, existing);

            ApplyPostedSectionChanges(mergedSection, postedSection, sectionDefinition);
            ContentPresentationSchema.SanitizeSection(mergedSection, sectionDefinition.AllowSectionSettings);
            mergedSection.UpdatedAt = DateTime.UtcNow;

            if (existing == null)
            {
                await _contentSectionRepository.AddAsync(mergedSection);
            }
            else
            {
                mergedSection.Id = existing.Id;
                await _contentSectionRepository.UpdateAsync(existing.Id, mergedSection);
            }
        }

        InvalidateCache();
    }

    public void InvalidateCache()
    {
        _memoryCache.Remove(SiteSettingsCacheKey);
        foreach (var tab in PublicContentDefaults.Tabs.Where(t => t.Key != "site"))
        {
            _memoryCache.Remove($"public-content-page-{tab.Key}");
        }
    }

    private static SiteSettings MergeSiteSettings(SiteSettings defaults, SiteSettings? stored)
    {
        var merged = CloneSiteSettings(defaults);
        if (stored == null)
        {
            return merged;
        }

        merged.Id = stored.Id;
        merged.UpdatedAt = stored.UpdatedAt;

        foreach (var group in merged.Groups)
        {
            var storedGroup = stored.Groups.FirstOrDefault(g => g.GroupKey == group.GroupKey);
            if (storedGroup == null)
            {
                continue;
            }

            var normalizedGroupTitle = NormalizeText(storedGroup.Title);
            var normalizedGroupDescription = NormalizeText(storedGroup.Description);
            group.Title = string.IsNullOrWhiteSpace(normalizedGroupTitle) ? group.Title : normalizedGroupTitle;
            group.Description = string.IsNullOrWhiteSpace(normalizedGroupDescription) ? group.Description : normalizedGroupDescription;
            group.IsEnabled = storedGroup.IsEnabled;
            group.IsTitleEnabled = storedGroup.IsTitleEnabled;
            group.IsDescriptionEnabled = storedGroup.IsDescriptionEnabled;
            group.DisplayOrder = storedGroup.DisplayOrder;

            foreach (var field in group.Fields)
            {
                var storedField = storedGroup.Fields.FirstOrDefault(f => f.Key == field.Key);
                if (storedField == null)
                {
                    continue;
                }

                var normalizedFieldValue = NormalizeText(storedField.Value);
                var normalizedFieldLabel = NormalizeText(storedField.Label);
                var normalizedPlaceholder = NormalizeText(storedField.Placeholder);
                field.Value = string.IsNullOrWhiteSpace(normalizedFieldValue) ? field.Value : normalizedFieldValue;
                field.Label = string.IsNullOrWhiteSpace(normalizedFieldLabel) ? field.Label : normalizedFieldLabel;
                field.FieldType = string.IsNullOrWhiteSpace(storedField.FieldType) ? field.FieldType : storedField.FieldType;
                field.Placeholder = string.IsNullOrWhiteSpace(normalizedPlaceholder) ? field.Placeholder : normalizedPlaceholder;
                field.IsEnabled = storedField.IsEnabled;
                field.Style = ContentPresentationSchema.SanitizeTextStyle(storedField.Style);
            }
        }

        return merged;
    }

    private static ContentSection MergeSection(ContentSection defaults, ContentSection? stored)
    {
        var merged = CloneSection(defaults);
        if (stored == null)
        {
            ContentPresentationDefaults.EnsureSection(merged);
            return merged;
        }

        merged.Id = stored.Id;
        var normalizedSectionTitle = NormalizeText(stored.Title);
        var normalizedSectionDescription = NormalizeText(stored.Description);
        merged.Title = string.IsNullOrWhiteSpace(normalizedSectionTitle) ? merged.Title : normalizedSectionTitle;
        merged.Description = string.IsNullOrWhiteSpace(normalizedSectionDescription) ? merged.Description : normalizedSectionDescription;
        merged.IsTitleEnabled = stored.IsTitleEnabled;
        merged.IsDescriptionEnabled = stored.IsDescriptionEnabled;
        merged.IsEnabled = stored.IsEnabled;
        merged.IsPublished = stored.IsPublished;
        merged.DisplayOrder = stored.DisplayOrder;
        merged.UpdatedAt = stored.UpdatedAt;
        merged.Presentation = ContentPresentationDefaults.CloneSectionPresentation(stored.Presentation);

        foreach (var field in merged.Fields)
        {
            var storedField = stored.Fields.FirstOrDefault(f => f.Key == field.Key);
            if (storedField == null)
            {
                continue;
            }

            var normalizedFieldValue = NormalizeText(storedField.Value);
            var normalizedFieldLabel = NormalizeText(storedField.Label);
            var normalizedPlaceholder = NormalizeText(storedField.Placeholder);
            field.Value = string.IsNullOrWhiteSpace(normalizedFieldValue) ? field.Value : normalizedFieldValue;
            field.Label = string.IsNullOrWhiteSpace(normalizedFieldLabel) ? field.Label : normalizedFieldLabel;
            field.FieldType = string.IsNullOrWhiteSpace(storedField.FieldType) ? field.FieldType : storedField.FieldType;
            field.Placeholder = string.IsNullOrWhiteSpace(normalizedPlaceholder) ? field.Placeholder : normalizedPlaceholder;
            field.Style = ContentPresentationDefaults.CloneTextStyle(storedField.Style);
        }

        ContentPresentationDefaults.EnsureSection(merged);
        return merged;
    }

    private static void ApplyPostedSectionChanges(ContentSection target, ContentSection posted, ContentAdminSectionDefinition definition)
    {
        ContentPresentationDefaults.EnsureSection(target);
        ContentPresentationDefaults.EnsureSection(posted);

        if (definition.AllowSectionSettings)
        {
            target.Title = NormalizeText(posted.Title);
            target.Description = NormalizeText(posted.Description);
            target.IsTitleEnabled = posted.IsTitleEnabled;
            target.IsDescriptionEnabled = posted.IsDescriptionEnabled;
            target.IsEnabled = posted.IsEnabled;
            target.IsPublished = posted.IsPublished;
            target.DisplayOrder = posted.DisplayOrder;
            target.Presentation = ContentPresentationDefaults.CloneSectionPresentation(posted.Presentation);
        }

        HashSet<string>? editableKeys = definition.EditableFieldKeys.Count > 0
            ? new HashSet<string>(definition.EditableFieldKeys, StringComparer.OrdinalIgnoreCase)
            : null;

        foreach (var postedField in posted.Fields)
        {
            if (editableKeys != null && !editableKeys.Contains(postedField.Key))
            {
                continue;
            }

            var targetField = target.Fields.FirstOrDefault(field => string.Equals(field.Key, postedField.Key, StringComparison.OrdinalIgnoreCase));
            if (targetField == null)
            {
                var newField = CloneField(postedField);
                ContentPresentationDefaults.EnsureField(newField);
                target.Fields.Add(newField);
                continue;
            }

            var normalizedPostedLabel = NormalizeText(postedField.Label);
            var normalizedPostedPlaceholder = NormalizeText(postedField.Placeholder);
            var normalizedFieldValue = NormalizeText(postedField.Value);
            if (string.Equals(target.SectionKey, "carousel", StringComparison.OrdinalIgnoreCase)
                && postedField.Key.EndsWith("LinkUrl", StringComparison.OrdinalIgnoreCase))
            {
                normalizedFieldValue = NormalizeCarouselLinkValue(normalizedFieldValue);
            }

            targetField.Value = normalizedFieldValue;
            targetField.Label = string.IsNullOrWhiteSpace(normalizedPostedLabel) ? targetField.Label : normalizedPostedLabel;
            targetField.FieldType = string.IsNullOrWhiteSpace(postedField.FieldType) ? targetField.FieldType : postedField.FieldType;
            targetField.Placeholder = string.IsNullOrWhiteSpace(normalizedPostedPlaceholder) ? targetField.Placeholder : normalizedPostedPlaceholder;
            targetField.IsEnabled = postedField.IsEnabled;
            targetField.Style = ContentPresentationDefaults.CloneTextStyle(postedField.Style);
        }
    }

    private static SiteSettings CloneSiteSettings(SiteSettings source)
    {
        return new SiteSettings
        {
            Id = source.Id,
            SettingsKey = source.SettingsKey,
            UpdatedAt = source.UpdatedAt,
            Groups = source.Groups.Select(CloneGroup).ToList()
        };
    }

    private static SiteSettingsGroup CloneGroup(SiteSettingsGroup source)
    {
        return new SiteSettingsGroup
        {
            GroupKey = source.GroupKey,
            Title = NormalizeText(source.Title),
            Description = NormalizeText(source.Description),
            IsEnabled = source.IsEnabled,
            IsTitleEnabled = source.IsTitleEnabled,
            IsDescriptionEnabled = source.IsDescriptionEnabled,
            DisplayOrder = source.DisplayOrder,
            Fields = source.Fields.Select(CloneField).ToList()
        };
    }

    private static ContentSection CloneSection(ContentSection source)
    {
        var clone = new ContentSection
        {
            Id = source.Id,
            PageKey = source.PageKey,
            SectionKey = source.SectionKey,
            Title = NormalizeText(source.Title),
            Description = NormalizeText(source.Description),
            IsEnabled = source.IsEnabled,
            IsTitleEnabled = source.IsTitleEnabled,
            IsDescriptionEnabled = source.IsDescriptionEnabled,
            IsPublished = source.IsPublished,
            DisplayOrder = source.DisplayOrder,
            UpdatedAt = source.UpdatedAt,
            Presentation = ContentPresentationDefaults.CloneSectionPresentation(source.Presentation),
            Fields = source.Fields.Select(CloneField).ToList()
        };

        ContentPresentationDefaults.EnsureSection(clone);
        return clone;
    }

    private static ContentField CloneField(ContentField source)
    {
        var clone = new ContentField
        {
            Key = source.Key,
            Label = NormalizeText(source.Label),
            FieldType = source.FieldType,
            Value = NormalizeText(source.Value),
            Placeholder = NormalizeText(source.Placeholder),
            IsEnabled = source.IsEnabled,
            Style = ContentPresentationDefaults.CloneTextStyle(source.Style)
        };

        ContentPresentationDefaults.EnsureField(clone);
        return clone;
    }

    private static string NormalizeText(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value ?? string.Empty;
        }

        if (!LooksLikeMojibake(value))
        {
            return value;
        }

        try
        {
            var repaired = Encoding.UTF8.GetString(Encoding.GetEncoding(1252).GetBytes(value));
            return ScoreCandidate(repaired) >= ScoreCandidate(value) ? repaired : value;
        }
        catch (ArgumentException)
        {
            return value;
        }
    }

    private static string NormalizeCarouselLinkValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var trimmed = value.Trim();
        if (trimmed.StartsWith("/", StringComparison.Ordinal))
        {
            return trimmed;
        }

        if (Uri.TryCreate(trimmed, UriKind.Absolute, out var absoluteUri)
            && (string.Equals(absoluteUri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
                || string.Equals(absoluteUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)))
        {
            return trimmed;
        }

        return string.Empty;
    }

    private static bool LooksLikeMojibake(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return false;
        }

        foreach (var marker in MojibakeMarkers ?? Array.Empty<string>())
        {
            if (!string.IsNullOrEmpty(marker) && value.Contains(marker, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static int ScoreCandidate(string value)
    {
        const string vietnameseChars = "ÄƒÃ¢Ä‘ÃªÃ´Æ¡Æ°Ã¡Ã áº£Ã£áº¡áº¯áº±áº³áºµáº·áº¥áº§áº©áº«áº­Ã©Ã¨áº»áº½áº¹áº¿á»á»ƒá»…á»‡Ã­Ã¬á»‰Ä©á»‹Ã³Ã²á»Ãµá»á»‘á»“á»•á»—á»™á»›á»á»Ÿá»¡á»£ÃºÃ¹á»§Å©á»¥á»©á»«á»­á»¯á»±Ã½á»³á»·á»¹á»µÄ‚Ã‚ÄÃŠÃ”Æ Æ¯ÃÃ€áº¢Ãƒáº áº®áº°áº²áº´áº¶áº¤áº¦áº¨áºªáº¬Ã‰Ãˆáººáº¼áº¸áº¾á»€á»‚á»„á»†ÃÃŒá»ˆÄ¨á»ŠÃ“Ã’á»ŽÃ•á»Œá»á»’á»”á»–á»˜á»šá»œá»žá» á»¢ÃšÃ™á»¦Å¨á»¤á»¨á»ªá»¬á»®á»°Ãá»²á»¶á»¸á»´";
        var suspiciousPenalty = 0;
        foreach (var marker in MojibakeMarkers ?? Array.Empty<string>())
        {
            if (!string.IsNullOrEmpty(marker) && value.Contains(marker, StringComparison.Ordinal))
            {
                suspiciousPenalty += 10;
            }
        }

        var vietnameseBonus = value.Count(character => vietnameseChars.Contains(character));
        return vietnameseBonus - suspiciousPenalty;
    }
}









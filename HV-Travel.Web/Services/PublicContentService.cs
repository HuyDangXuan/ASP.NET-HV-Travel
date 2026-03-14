using HVTravel.Domain.Entities;
using HVTravel.Domain.Interfaces;
using HVTravel.Web.Models;
using Microsoft.Extensions.Caching.Memory;

namespace HVTravel.Web.Services;

public class PublicContentService : IPublicContentService
{
    private const string SiteSettingsCacheKey = "public-content-site-settings";
    private readonly IRepository<SiteSettings> _siteSettingsRepository;
    private readonly IRepository<ContentSection> _contentSectionRepository;
    private readonly IMemoryCache _memoryCache;

    public PublicContentService(
        IRepository<SiteSettings> siteSettingsRepository,
        IRepository<ContentSection> contentSectionRepository,
        IMemoryCache memoryCache)
    {
        _siteSettingsRepository = siteSettingsRepository;
        _contentSectionRepository = contentSectionRepository;
        _memoryCache = memoryCache;
    }

    public IReadOnlyList<ContentTabOption> GetTabs() => PublicContentDefaults.Tabs;

    public Dictionary<string, List<string>> GetEditableInventory() => PublicContentDefaults.Inventory;

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
        var storedSections = (await _contentSectionRepository.FindAsync(s => s.PageKey == pageKey && s.IsPublished))
            .ToList();

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

    public async Task SaveSectionsAsync(IEnumerable<ContentSection> sections)
    {
        foreach (var section in sections)
        {
            section.UpdatedAt = DateTime.UtcNow;
            var existing = (await _contentSectionRepository.FindAsync(s => s.PageKey == section.PageKey && s.SectionKey == section.SectionKey)).FirstOrDefault();
            if (existing == null)
            {
                await _contentSectionRepository.AddAsync(section);
            }
            else
            {
                section.Id = existing.Id;
                await _contentSectionRepository.UpdateAsync(existing.Id, section);
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

            group.Title = string.IsNullOrWhiteSpace(storedGroup.Title) ? group.Title : storedGroup.Title;
            group.Description = string.IsNullOrWhiteSpace(storedGroup.Description) ? group.Description : storedGroup.Description;
            group.IsEnabled = storedGroup.IsEnabled;
            group.DisplayOrder = storedGroup.DisplayOrder;

            foreach (var field in group.Fields)
            {
                var storedField = storedGroup.Fields.FirstOrDefault(f => f.Key == field.Key);
                if (storedField == null)
                {
                    continue;
                }

                field.Value = string.IsNullOrWhiteSpace(storedField.Value) ? field.Value : storedField.Value;
                field.Label = string.IsNullOrWhiteSpace(storedField.Label) ? field.Label : storedField.Label;
                field.FieldType = string.IsNullOrWhiteSpace(storedField.FieldType) ? field.FieldType : storedField.FieldType;
            }
        }

        return merged;
    }

    private static ContentSection MergeSection(ContentSection defaults, ContentSection? stored)
    {
        var merged = CloneSection(defaults);
        if (stored == null)
        {
            return merged;
        }

        merged.Id = stored.Id;
        merged.Title = string.IsNullOrWhiteSpace(stored.Title) ? merged.Title : stored.Title;
        merged.Description = string.IsNullOrWhiteSpace(stored.Description) ? merged.Description : stored.Description;
        merged.IsEnabled = stored.IsEnabled;
        merged.IsPublished = stored.IsPublished;
        merged.DisplayOrder = stored.DisplayOrder;
        merged.UpdatedAt = stored.UpdatedAt;

        foreach (var field in merged.Fields)
        {
            var storedField = stored.Fields.FirstOrDefault(f => f.Key == field.Key);
            if (storedField == null)
            {
                continue;
            }

            field.Value = string.IsNullOrWhiteSpace(storedField.Value) ? field.Value : storedField.Value;
            field.Label = string.IsNullOrWhiteSpace(storedField.Label) ? field.Label : storedField.Label;
            field.FieldType = string.IsNullOrWhiteSpace(storedField.FieldType) ? field.FieldType : storedField.FieldType;
        }

        return merged;
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
            Title = source.Title,
            Description = source.Description,
            IsEnabled = source.IsEnabled,
            DisplayOrder = source.DisplayOrder,
            Fields = source.Fields.Select(CloneField).ToList()
        };
    }

    private static ContentSection CloneSection(ContentSection source)
    {
        return new ContentSection
        {
            Id = source.Id,
            PageKey = source.PageKey,
            SectionKey = source.SectionKey,
            Title = source.Title,
            Description = source.Description,
            IsEnabled = source.IsEnabled,
            IsPublished = source.IsPublished,
            DisplayOrder = source.DisplayOrder,
            UpdatedAt = source.UpdatedAt,
            Fields = source.Fields.Select(CloneField).ToList()
        };
    }

    private static ContentField CloneField(ContentField source)
    {
        return new ContentField
        {
            Key = source.Key,
            Label = source.Label,
            FieldType = source.FieldType,
            Value = source.Value,
            Placeholder = source.Placeholder
        };
    }
}

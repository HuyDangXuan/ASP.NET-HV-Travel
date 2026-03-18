using HVTravel.Domain.Entities;
using HVTravel.Domain.Interfaces;
using HVTravel.Web.Models;
using HVTravel.Web.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;

namespace HVTravel.Web.Services;

public class PublicContentService : IPublicContentService
{
    private const string SiteSettingsCacheKey = "public-content-site-settings";
    private readonly IRepository<SiteSettings> _siteSettingsRepository;
    private readonly IRepository<ContentSection> _contentSectionRepository;
    private readonly IMemoryCache _memoryCache;
    private readonly IHttpContextAccessor _httpContextAccessor;

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
        var previewSnapshot = await GetActivePreviewSnapshotAsync();
        if (previewSnapshot?.SiteSettings != null)
        {
            return CloneSiteSettings(previewSnapshot.SiteSettings);
        }

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
        var previewSnapshot = await GetActivePreviewSnapshotAsync();

        var cacheKey = $"public-content-page-{pageKey}";
        if (_memoryCache.TryGetValue(cacheKey, out Dictionary<string, ContentSection>? cached) && cached != null)
        {
            return previewSnapshot == null
                ? cached
                : ApplyPreviewSections(cached, previewSnapshot, pageKey);
        }

        var defaults = PublicContentDefaults.CreateSectionsForPage(pageKey);
        var storedSections = (await _contentSectionRepository.FindAsync(s => s.PageKey == pageKey && s.IsPublished))
            .ToList();

        var merged = defaults
            .Select(defaultSection => MergeSection(defaultSection, storedSections.FirstOrDefault(s => s.SectionKey == defaultSection.SectionKey)))
            .OrderBy(section => section.DisplayOrder)
            .ToDictionary(section => section.SectionKey, section => section);

        _memoryCache.Set(cacheKey, merged, TimeSpan.FromMinutes(5));
        return previewSnapshot == null
            ? merged
            : ApplyPreviewSections(merged, previewSnapshot, pageKey);
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

    public async Task<ContentPreviewSnapshot> BuildPreviewSnapshotAsync(
        string tab,
        string? subtab,
        string previewToken,
        SiteSettings? siteSettings,
        IEnumerable<ContentSection>? sections)
    {
        var editor = ResolveAdminEditor(tab, subtab);

        return new ContentPreviewSnapshot
        {
            PreviewToken = previewToken,
            Tab = editor.TabKey,
            Subtab = editor.SubtabKey,
            PageKey = editor.PageKey,
            SiteSettings = siteSettings == null ? null : CloneSiteSettings(siteSettings),
            Sections = sections?.Select(CloneSection).ToList() ?? new List<ContentSection>(),
            UpdatedAtUtc = DateTime.UtcNow
        };
    }

    public Task<string> StorePreviewSnapshotAsync(ContentPreviewSnapshot snapshot)
    {
        var cacheKey = BuildPreviewCacheKey(snapshot.PreviewToken, GetCurrentUserScopeKey(_httpContextAccessor.HttpContext?.User));
        _memoryCache.Set(cacheKey, snapshot, TimeSpan.FromMinutes(ContentPreviewConstants.CacheTtlMinutes));
        return Task.FromResult(snapshot.PreviewToken);
    }

    public Task<ContentPreviewSnapshot?> GetPreviewSnapshotAsync(string previewToken)
    {
        return Task.FromResult(GetPreviewSnapshotForUser(previewToken, _httpContextAccessor.HttpContext?.User));
    }

    public void InvalidateCache()
    {
        _memoryCache.Remove(SiteSettingsCacheKey);
        foreach (var tab in PublicContentDefaults.Tabs.Where(t => t.Key != "site"))
        {
            _memoryCache.Remove($"public-content-page-{tab.Key}");
        }
    }

    private async Task<ContentPreviewSnapshot?> GetActivePreviewSnapshotAsync()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var previewToken = httpContext?.Request.Query[ContentPreviewConstants.QueryKey].ToString();
        if (string.IsNullOrWhiteSpace(previewToken))
        {
            return null;
        }

        var authResult = await httpContext!.AuthenticateAsync(AuthSchemes.AdminScheme);
        if (!authResult.Succeeded)
        {
            return null;
        }

        return GetPreviewSnapshotForUser(previewToken, authResult.Principal);
    }

    private static IReadOnlyDictionary<string, ContentSection> ApplyPreviewSections(
        IReadOnlyDictionary<string, ContentSection> baseSections,
        ContentPreviewSnapshot previewSnapshot,
        string pageKey)
    {
        if (!string.Equals(previewSnapshot.PageKey, pageKey, StringComparison.OrdinalIgnoreCase) || previewSnapshot.Sections.Count == 0)
        {
            return baseSections;
        }

        var editor = ContentAdminCatalog.Resolve(previewSnapshot.Tab, previewSnapshot.Subtab);
        var previewSectionMap = previewSnapshot.Sections.ToDictionary(section => section.SectionKey, section => section, StringComparer.OrdinalIgnoreCase);
        var merged = baseSections.ToDictionary(entry => entry.Key, entry => CloneSection(entry.Value), StringComparer.OrdinalIgnoreCase);

        foreach (var definition in editor.Sections)
        {
            if (!previewSectionMap.TryGetValue(definition.SectionKey, out var previewSection))
            {
                continue;
            }

            if (!merged.TryGetValue(definition.SectionKey, out var targetSection))
            {
                targetSection = new ContentSection
                {
                    PageKey = pageKey,
                    SectionKey = definition.SectionKey
                };
                merged[definition.SectionKey] = targetSection;
            }

            ApplyPostedSectionChanges(targetSection, previewSection, definition);
        }

        return merged
            .OrderBy(entry => entry.Value.DisplayOrder)
            .ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.OrdinalIgnoreCase);
    }

    private static string BuildPreviewCacheKey(string previewToken, string userScopeKey)
        => $"public-content-preview::{userScopeKey}::{previewToken}";

    private ContentPreviewSnapshot? GetPreviewSnapshotForUser(string previewToken, ClaimsPrincipal? principal)
    {
        var cacheKey = BuildPreviewCacheKey(previewToken, GetCurrentUserScopeKey(principal));
        return _memoryCache.TryGetValue(cacheKey, out ContentPreviewSnapshot? snapshot) ? snapshot : null;
    }

    private string GetCurrentUserScopeKey(ClaimsPrincipal? user)
    {
        return user?.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? user?.Identity?.Name
               ?? "anonymous-admin";
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

    private static void ApplyPostedSectionChanges(ContentSection target, ContentSection posted, ContentAdminSectionDefinition definition)
    {
        if (definition.AllowSectionSettings)
        {
            target.Title = posted.Title;
            target.Description = posted.Description;
            target.IsEnabled = posted.IsEnabled;
            target.IsPublished = posted.IsPublished;
            target.DisplayOrder = posted.DisplayOrder;
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
                target.Fields.Add(CloneField(postedField));
                continue;
            }

            targetField.Value = postedField.Value;
            targetField.Label = string.IsNullOrWhiteSpace(postedField.Label) ? targetField.Label : postedField.Label;
            targetField.FieldType = string.IsNullOrWhiteSpace(postedField.FieldType) ? targetField.FieldType : postedField.FieldType;
            targetField.Placeholder = string.IsNullOrWhiteSpace(postedField.Placeholder) ? targetField.Placeholder : postedField.Placeholder;
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

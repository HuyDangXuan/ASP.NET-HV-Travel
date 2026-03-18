using HVTravel.Domain.Entities;
using HVTravel.Web.Models;

namespace HVTravel.Web.Services;

public interface IPublicContentService
{
    IReadOnlyList<ContentTabOption> GetTabs();

    Dictionary<string, List<string>> GetEditableInventory();

    ContentAdminEditorDefinition ResolveAdminEditor(string? tab, string? subtab);

    Task<SiteSettings> GetSiteSettingsAsync();

    Task<IReadOnlyDictionary<string, ContentSection>> GetPageSectionsAsync(string pageKey);

    Task<List<ContentSection>> GetPageSectionsForAdminAsync(string pageKey);

    Task SaveSiteSettingsAsync(SiteSettings siteSettings);

    Task SaveSectionsAsync(IEnumerable<ContentSection> sections, string tab, string? subtab);

    Task<ContentPreviewSnapshot> BuildPreviewSnapshotAsync(
        string tab,
        string? subtab,
        string previewToken,
        SiteSettings? siteSettings,
        IEnumerable<ContentSection>? sections);

    Task<string> StorePreviewSnapshotAsync(ContentPreviewSnapshot snapshot);

    Task<ContentPreviewSnapshot?> GetPreviewSnapshotAsync(string previewToken);

    void InvalidateCache();
}

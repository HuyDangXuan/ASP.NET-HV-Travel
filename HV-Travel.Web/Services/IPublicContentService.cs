using HVTravel.Domain.Entities;
using HVTravel.Web.Models;

namespace HVTravel.Web.Services;

public interface IPublicContentService
{
    IReadOnlyList<ContentTabOption> GetTabs();

    Dictionary<string, List<string>> GetEditableInventory();

    Task<SiteSettings> GetSiteSettingsAsync();

    Task<IReadOnlyDictionary<string, ContentSection>> GetPageSectionsAsync(string pageKey);

    Task<List<ContentSection>> GetPageSectionsForAdminAsync(string pageKey);

    Task SaveSiteSettingsAsync(SiteSettings siteSettings);

    Task SaveSectionsAsync(IEnumerable<ContentSection> sections);

    void InvalidateCache();
}

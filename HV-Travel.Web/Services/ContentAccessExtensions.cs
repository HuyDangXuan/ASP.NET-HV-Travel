using HVTravel.Domain.Entities;

namespace HVTravel.Web.Services;

public static class ContentAccessExtensions
{
    public static SiteSettingsGroup? GetGroup(this SiteSettings settings, string groupKey)
    {
        return settings.Groups.FirstOrDefault(g => g.GroupKey == groupKey);
    }

    public static ContentSection? GetSection(this IReadOnlyDictionary<string, ContentSection> sections, string sectionKey)
    {
        return sections.TryGetValue(sectionKey, out var section) ? section : null;
    }

    public static string GetFieldValue(this IEnumerable<ContentField>? fields, string key, string fallback = "")
    {
        return fields?.FirstOrDefault(f => f.Key == key)?.Value ?? fallback;
    }
}

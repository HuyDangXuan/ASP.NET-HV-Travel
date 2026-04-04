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
        return GetVisibleFieldValue(fields, key, fallback);
    }

    public static string GetVisibleFieldValue(this IEnumerable<ContentField>? fields, string key, string fallback = "")
    {
        var field = fields?.FirstOrDefault(f => string.Equals(f.Key, key, StringComparison.OrdinalIgnoreCase));
        if (field == null)
        {
            return fallback;
        }

        if (!field.IsEnabled)
        {
            return string.Empty;
        }

        return field.Value;
    }

    public static bool HasVisibleFieldValue(this IEnumerable<ContentField>? fields, string key)
    {
        var field = fields?.FirstOrDefault(f => string.Equals(f.Key, key, StringComparison.OrdinalIgnoreCase));
        return field?.IsEnabled == true && !string.IsNullOrWhiteSpace(field.Value);
    }

    public static string NormalizeCarouselLink(string? value)
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

    public static string GetVisibleTitle(this ContentSection? section, string fallback = "")
    {
        if (section == null)
        {
            return fallback;
        }

        if (!section.IsTitleEnabled)
        {
            return string.Empty;
        }

        return string.IsNullOrWhiteSpace(section.Title) ? fallback : section.Title;
    }

    public static string GetVisibleDescription(this ContentSection? section, string fallback = "")
    {
        if (section == null)
        {
            return fallback;
        }

        if (!section.IsDescriptionEnabled)
        {
            return string.Empty;
        }

        return string.IsNullOrWhiteSpace(section.Description) ? fallback : section.Description;
    }

    public static string GetVisibleTitle(this SiteSettingsGroup? group, string fallback = "")
    {
        if (group == null)
        {
            return fallback;
        }

        if (!group.IsTitleEnabled)
        {
            return string.Empty;
        }

        return string.IsNullOrWhiteSpace(group.Title) ? fallback : group.Title;
    }

    public static string GetVisibleDescription(this SiteSettingsGroup? group, string fallback = "")
    {
        if (group == null)
        {
            return fallback;
        }

        if (!group.IsDescriptionEnabled)
        {
            return string.Empty;
        }

        return string.IsNullOrWhiteSpace(group.Description) ? fallback : group.Description;
    }
}


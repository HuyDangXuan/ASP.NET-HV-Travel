using System.Globalization;
using System.Text.RegularExpressions;
using HVTravel.Domain.Entities;

namespace HVTravel.Web.Services;

public static partial class ContentPresentationSchema
{
    public static IReadOnlyList<string> FontPresets { get; } = new[]
    {
        "Plus Jakarta Sans",
        "Be Vietnam Pro",
        "Manrope",
        "Playfair Display",
        "Merriweather"
    };

    public static IReadOnlyList<string> SectionAlignments { get; } = new[] { string.Empty, "left", "center", "right" };

    public static IReadOnlyList<string> TextAlignments { get; } = new[] { string.Empty, "left", "center", "right", "justify" };

    public static IReadOnlyList<string> ColorPresets { get; } = new[] { string.Empty, "default", "muted", "primary", "accent", "inverse", "success", "warning", "danger", "custom" };

    public static IReadOnlyList<string> BackgroundPresets { get; } = new[] { string.Empty, "default", "transparent", "surface-light", "surface-dark", "primary-soft", "accent-soft", "hero-gradient", "custom" };

    public static IReadOnlyList<string> SizePresets { get; } = new[] { string.Empty, "xs", "sm", "base", "lg", "xl", "2xl", "3xl", "4xl", "custom" };

    public static IReadOnlyList<string> SizeUnits { get; } = new[] { "px", "rem" };

    public static void SanitizeSection(ContentSection section, bool allowSectionSettings)
    {
        ContentPresentationDefaults.EnsureSection(section);

        if (allowSectionSettings)
        {
            section.Presentation.Container = SanitizeContainer(section.Presentation.Container);
            section.Presentation.EyebrowText = SanitizeTextStyle(section.Presentation.EyebrowText);
            section.Presentation.TitleText = SanitizeTextStyle(section.Presentation.TitleText);
            section.Presentation.DescriptionText = SanitizeTextStyle(section.Presentation.DescriptionText);
        }

        foreach (var field in section.Fields)
        {
            field.Style = SanitizeTextStyle(field.Style);
        }
    }

    public static ContentContainerStyle SanitizeContainer(ContentContainerStyle? source)
    {
        var style = ContentPresentationDefaults.CloneContainerStyle(source);
        style.Align = NormalizeToken(style.Align, SectionAlignments);
        style.BackgroundPreset = NormalizeToken(style.BackgroundPreset, BackgroundPresets, "default");
        style.CustomBackgroundHex = string.Equals(style.BackgroundPreset, "custom", StringComparison.OrdinalIgnoreCase)
            ? SanitizeHex(style.CustomBackgroundHex)
            : string.Empty;

        if (string.Equals(style.BackgroundPreset, "custom", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(style.CustomBackgroundHex))
        {
            style.BackgroundPreset = "default";
        }

        return style;
    }

    public static ContentTextStyle SanitizeTextStyle(ContentTextStyle? source)
    {
        var style = ContentPresentationDefaults.CloneTextStyle(source);
        style.Align = NormalizeToken(style.Align, TextAlignments);
        style.FontPreset = NormalizeToken(style.FontPreset, FontPresets);
        style.CustomFontFamily = SanitizeFontFamily(style.CustomFontFamily);
        style.SizePreset = NormalizeToken(style.SizePreset, SizePresets);
        style.CustomSizeUnit = NormalizeToken(style.CustomSizeUnit, SizeUnits);
        style.ColorPreset = NormalizeToken(style.ColorPreset, ColorPresets, "default");
        style.CustomColorHex = string.Equals(style.ColorPreset, "custom", StringComparison.OrdinalIgnoreCase)
            ? SanitizeHex(style.CustomColorHex)
            : string.Empty;

        if (!string.Equals(style.SizePreset, "custom", StringComparison.OrdinalIgnoreCase))
        {
            style.CustomSizeValue = null;
            style.CustomSizeUnit = string.Empty;
        }
        else if (!style.CustomSizeValue.HasValue || style.CustomSizeValue.Value <= 0 || string.IsNullOrWhiteSpace(style.CustomSizeUnit))
        {
            style.SizePreset = string.Empty;
            style.CustomSizeValue = null;
            style.CustomSizeUnit = string.Empty;
        }

        if (string.Equals(style.ColorPreset, "custom", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(style.CustomColorHex))
        {
            style.ColorPreset = "default";
        }

        return style;
    }

    public static bool SupportsFieldStyle(ContentField field)
    {
        if (field == null)
        {
            return false;
        }

        if (!string.Equals(field.FieldType, "text", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(field.FieldType, "textarea", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }

    public static bool SupportsFieldVisibilityToggle(ContentField field)
    {
        if (field == null || string.IsNullOrWhiteSpace(field.Key))
        {
            return false;
        }

        if (field.Key.EndsWith("ImageUrl", StringComparison.OrdinalIgnoreCase)
            || field.Key.EndsWith("LinkUrl", StringComparison.OrdinalIgnoreCase)
            || field.Key.EndsWith("SourceType", StringComparison.OrdinalIgnoreCase)
            || string.Equals(field.Key, "mapEmbedUrl", StringComparison.OrdinalIgnoreCase)
            || field.Key.EndsWith("facebookUrl", StringComparison.OrdinalIgnoreCase)
            || field.Key.EndsWith("instagramUrl", StringComparison.OrdinalIgnoreCase)
            || field.Key.EndsWith("youtubeUrl", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (string.Equals(field.FieldType, "text", StringComparison.OrdinalIgnoreCase)
            || string.Equals(field.FieldType, "textarea", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return string.Equals(field.FieldType, "url", StringComparison.OrdinalIgnoreCase)
            && string.Equals(field.Key, "email", StringComparison.OrdinalIgnoreCase);
    }
    public static bool IsButtonLikeField(ContentField field)
    {
        if (field == null || string.IsNullOrWhiteSpace(field.Key))
        {
            return false;
        }

        return field.Key.Contains("cta", StringComparison.OrdinalIgnoreCase)
            || field.Key.Contains("submit", StringComparison.OrdinalIgnoreCase)
            || field.Key.Contains("exploreTours", StringComparison.OrdinalIgnoreCase)
            || field.Key.Contains("viewAll", StringComparison.OrdinalIgnoreCase);
    }

    public static string FormatCustomSize(decimal? value, string? unit)
    {
        if (!value.HasValue || value.Value <= 0 || string.IsNullOrWhiteSpace(unit))
        {
            return string.Empty;
        }

        return FormattableString.Invariant($"{value.Value:0.###}{unit}");
    }

    public static string GetFontStack(string? preset)
    {
        return preset switch
        {
            "Plus Jakarta Sans" => "\"Plus Jakarta Sans\", sans-serif",
            "Be Vietnam Pro" => "\"Be Vietnam Pro\", sans-serif",
            "Manrope" => "\"Manrope\", sans-serif",
            "Playfair Display" => "\"Playfair Display\", serif",
            "Merriweather" => "\"Merriweather\", serif",
            _ => string.Empty
        };
    }

    private static string NormalizeToken(string? value, IReadOnlyList<string> allowed, string fallback = "")
    {
        var candidate = (value ?? string.Empty).Trim();
        return allowed.Contains(candidate, StringComparer.OrdinalIgnoreCase) ? candidate : fallback;
    }

    private static string SanitizeHex(string? value)
    {
        var candidate = (value ?? string.Empty).Trim();
        return HexPattern().IsMatch(candidate) ? candidate.ToUpperInvariant() : string.Empty;
    }

    private static string SanitizeFontFamily(string? value)
    {
        var candidate = (value ?? string.Empty).Trim();
        return FontFamilyPattern().IsMatch(candidate) ? candidate : string.Empty;
    }

    [GeneratedRegex("^#(?:[0-9a-fA-F]{6}|[0-9a-fA-F]{8})$", RegexOptions.Compiled)]
    private static partial Regex HexPattern();

    [GeneratedRegex("^[A-Za-z0-9\"' ,.-]{0,120}$", RegexOptions.Compiled)]
    private static partial Regex FontFamilyPattern();
}



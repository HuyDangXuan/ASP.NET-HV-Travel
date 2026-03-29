using System.Text;
using HVTravel.Domain.Entities;

namespace HVTravel.Web.Services;

public static class ContentPresentationViewHelper
{
    public static string GetSectionClass(ContentSection? section)
    {
        if (section == null)
        {
            return string.Empty;
        }

        ContentPresentationDefaults.EnsureSection(section);

        var classes = new List<string> { "cms-managed-section" };
        if (!string.IsNullOrWhiteSpace(section.Presentation.Container.Align))
        {
            classes.Add($"cms-text-{section.Presentation.Container.Align.ToLowerInvariant()}");
        }

        if (HasSectionBackground(section.Presentation.Container))
        {
            classes.Add("cms-managed-section--bg");
        }

        return string.Join(" ", classes);
    }

    public static string GetSectionTextClass(ContentSection? section)
    {
        if (section == null)
        {
            return string.Empty;
        }

        ContentPresentationDefaults.EnsureSection(section);
        return string.IsNullOrWhiteSpace(section.Presentation.Container.Align)
            ? string.Empty
            : $"cms-text-{section.Presentation.Container.Align.ToLowerInvariant()}";
    }

    public static string GetSectionBackgroundClass(ContentSection? section)
    {
        if (section == null)
        {
            return string.Empty;
        }

        ContentPresentationDefaults.EnsureSection(section);
        return HasSectionBackground(section.Presentation.Container)
            ? "cms-managed-section cms-managed-section--bg"
            : string.Empty;
    }

    public static string GetSectionStyle(ContentSection? section)
    {
        if (section == null)
        {
            return string.Empty;
        }

        ContentPresentationDefaults.EnsureSection(section);
        var container = ContentPresentationSchema.SanitizeContainer(section.Presentation.Container);
        var builder = new StringBuilder();
        AppendStyle(builder, "text-align", container.Align);

        var background = ResolveBackground(container);
        AppendStyle(builder, "--cms-section-bg", background);
        return builder.ToString();
    }

    public static string GetRoleTextStyle(ContentSection? section, string role)
    {
        if (section == null)
        {
            return string.Empty;
        }

        ContentPresentationDefaults.EnsureSection(section);
        var roleStyle = role?.ToLowerInvariant() switch
        {
            "eyebrow" => section.Presentation.EyebrowText,
            "title" => section.Presentation.TitleText,
            _ => section.Presentation.DescriptionText
        };

        return BuildTextStyle(roleStyle, null);
    }

    public static string GetFieldTextStyle(ContentSection? section, string fieldKey)
    {
        if (section == null || string.IsNullOrWhiteSpace(fieldKey))
        {
            return string.Empty;
        }

        ContentPresentationDefaults.EnsureSection(section);
        var field = section.Fields.FirstOrDefault(item => string.Equals(item.Key, fieldKey, StringComparison.OrdinalIgnoreCase));
        if (field == null || ContentPresentationSchema.IsButtonLikeField(field))
        {
            return string.Empty;
        }

        var roleStyle = InferRoleStyle(section, fieldKey);
        return BuildTextStyle(field.Style, roleStyle);
    }

    private static ContentTextStyle? InferRoleStyle(ContentSection section, string fieldKey)
    {
        if (fieldKey.Contains("badge", StringComparison.OrdinalIgnoreCase)
            || fieldKey.Contains("eyebrow", StringComparison.OrdinalIgnoreCase))
        {
            return section.Presentation.EyebrowText;
        }

        if (fieldKey.Contains("title", StringComparison.OrdinalIgnoreCase)
            || fieldKey.Contains("headline", StringComparison.OrdinalIgnoreCase)
            || fieldKey.Contains("highlight", StringComparison.OrdinalIgnoreCase))
        {
            return section.Presentation.TitleText;
        }

        if (fieldKey.Contains("description", StringComparison.OrdinalIgnoreCase)
            || fieldKey.EndsWith("Text", StringComparison.OrdinalIgnoreCase)
            || fieldKey.Contains("content", StringComparison.OrdinalIgnoreCase)
            || fieldKey.Contains("message", StringComparison.OrdinalIgnoreCase))
        {
            return section.Presentation.DescriptionText;
        }

        return null;
    }

    private static string BuildTextStyle(ContentTextStyle? source, ContentTextStyle? fallback)
    {
        var primary = ContentPresentationSchema.SanitizeTextStyle(source);
        var defaultStyle = fallback == null
            ? ContentPresentationDefaults.CreateTextStyle()
            : ContentPresentationSchema.SanitizeTextStyle(fallback);

        var align = !string.IsNullOrWhiteSpace(primary.Align) ? primary.Align : defaultStyle.Align;
        var fontFamily = !string.IsNullOrWhiteSpace(primary.CustomFontFamily)
            ? primary.CustomFontFamily
            : (!string.IsNullOrWhiteSpace(primary.FontPreset)
                ? ContentPresentationSchema.GetFontStack(primary.FontPreset)
                : (!string.IsNullOrWhiteSpace(defaultStyle.CustomFontFamily)
                    ? defaultStyle.CustomFontFamily
                    : ContentPresentationSchema.GetFontStack(defaultStyle.FontPreset)));

        var size = ResolveFontSize(primary);
        if (string.IsNullOrWhiteSpace(size))
        {
            size = ResolveFontSize(defaultStyle);
        }

        var color = ResolveTextColor(primary);
        if (string.IsNullOrWhiteSpace(color))
        {
            color = ResolveTextColor(defaultStyle);
        }

        var builder = new StringBuilder();
        AppendStyle(builder, "text-align", align);
        AppendStyle(builder, "font-family", fontFamily);
        AppendStyle(builder, "font-size", size);
        AppendStyle(builder, "color", color);
        return builder.ToString();
    }

    private static string ResolveFontSize(ContentTextStyle style)
    {
        if (string.Equals(style.SizePreset, "custom", StringComparison.OrdinalIgnoreCase))
        {
            return ContentPresentationSchema.FormatCustomSize(style.CustomSizeValue, style.CustomSizeUnit);
        }

        return style.SizePreset switch
        {
            "xs" => "0.75rem",
            "sm" => "0.875rem",
            "base" => "1rem",
            "lg" => "1.125rem",
            "xl" => "1.25rem",
            "2xl" => "1.5rem",
            "3xl" => "1.875rem",
            "4xl" => "2.25rem",
            _ => string.Empty
        };
    }

    private static string ResolveTextColor(ContentTextStyle style)
    {
        if (string.Equals(style.ColorPreset, "custom", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(style.CustomColorHex))
        {
            return style.CustomColorHex;
        }

        return style.ColorPreset switch
        {
            "muted" => "rgb(100 116 139)",
            "primary" => "rgb(14 165 233)",
            "accent" => "rgb(34 211 238)",
            "inverse" => "#FFFFFF",
            "success" => "rgb(22 163 74)",
            "warning" => "rgb(202 138 4)",
            "danger" => "rgb(220 38 38)",
            _ => string.Empty
        };
    }

    private static bool HasSectionBackground(ContentContainerStyle style)
    {
        var background = ResolveBackground(style);
        return !string.IsNullOrWhiteSpace(background);
    }

    private static string ResolveBackground(ContentContainerStyle style)
    {
        if (string.Equals(style.BackgroundPreset, "custom", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(style.CustomBackgroundHex))
        {
            return style.CustomBackgroundHex;
        }

        return style.BackgroundPreset switch
        {
            "transparent" => "transparent",
            "surface-light" => "rgba(248, 250, 252, 0.92)",
            "surface-dark" => "rgba(15, 23, 42, 0.92)",
            "primary-soft" => "linear-gradient(135deg, rgba(14, 165, 233, 0.12), rgba(56, 189, 248, 0.08))",
            "accent-soft" => "linear-gradient(135deg, rgba(34, 211, 238, 0.12), rgba(59, 130, 246, 0.08))",
            "hero-gradient" => "linear-gradient(135deg, #07111b 0%, #0d1b2a 42%, #12324b 100%)",
            _ => string.Empty
        };
    }

    private static void AppendStyle(StringBuilder builder, string property, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        if (builder.Length > 0)
        {
            builder.Append(' ');
        }

        builder.Append(property);
        builder.Append(':');
        builder.Append(' ');
        builder.Append(value);
        builder.Append(';');
    }
}

using System.Net;
using System.Text.RegularExpressions;

namespace HVTravel.Web.Services;

public static partial class RichTextContentFormatter
{
    [GeneratedRegex(@"<\s*br\s*/?\s*>", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex BreakTagRegex();

    [GeneratedRegex(@"</?(?:p|div|li|ul|ol|h[1-6]|blockquote|section|article|tr|td|th)\b[^>]*>", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex BlockTagRegex();

    [GeneratedRegex(@"<[^>]+>", RegexOptions.Compiled)]
    private static partial Regex AnyTagRegex();

    [GeneratedRegex(@"\s+", RegexOptions.Compiled)]
    private static partial Regex WhitespaceRegex();

    public static string ToTrustedHtml(string? html)
    {
        return string.IsNullOrWhiteSpace(html) ? string.Empty : DecodeHtmlLayers(html).Trim();
    }

    public static string ToPlainText(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return string.Empty;
        }

        var decodedInput = DecodeHtmlLayers(html);
        var withBreaks = BreakTagRegex().Replace(decodedInput, " ");
        withBreaks = BlockTagRegex().Replace(withBreaks, " ");
        var withoutTags = AnyTagRegex().Replace(withBreaks, " ");
        var decoded = WebUtility.HtmlDecode(withoutTags).Replace('\u00A0', ' ');
        return WhitespaceRegex().Replace(decoded, " ").Trim();
    }

    public static string ToPlainTextSummary(string? html, int maxLength = 160)
    {
        var plainText = ToPlainText(html);
        if (plainText.Length <= maxLength)
        {
            return plainText;
        }

        if (maxLength <= 0)
        {
            return string.Empty;
        }

        var shortened = plainText[..maxLength].TrimEnd();
        if (plainText.Length > maxLength && !char.IsWhiteSpace(plainText[maxLength]))
        {
            var lastSpace = shortened.LastIndexOf(' ');
            if (lastSpace > 0)
            {
                shortened = shortened[..lastSpace];
            }
        }

        return shortened.TrimEnd() + "…";
    }

    private static string DecodeHtmlLayers(string value)
    {
        var current = value;
        for (var i = 0; i < 3; i++)
        {
            var decoded = WebUtility.HtmlDecode(current);
            if (string.Equals(decoded, current, StringComparison.Ordinal))
            {
                break;
            }

            current = decoded;
        }

        return current;
    }
}

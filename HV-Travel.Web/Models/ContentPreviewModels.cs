using HVTravel.Domain.Entities;

namespace HVTravel.Web.Models;

public static class ContentPreviewConstants
{
    public const string QueryKey = "contentPreviewToken";
    public const int CacheTtlMinutes = 20;
}

public class ContentPreviewSnapshot
{
    public string PreviewToken { get; set; } = string.Empty;

    public string Tab { get; set; } = string.Empty;

    public string? Subtab { get; set; }

    public string PageKey { get; set; } = string.Empty;

    public SiteSettings? SiteSettings { get; set; }

    public List<ContentSection> Sections { get; set; } = new();

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class ContentPreviewResponse
{
    public bool Success { get; set; }

    public string PreviewToken { get; set; } = string.Empty;

    public string LivePreviewUrl { get; set; } = string.Empty;

    public string ReloadKey { get; set; } = string.Empty;

    public string StatusMessage { get; set; } = string.Empty;
}

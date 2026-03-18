using HVTravel.Domain.Entities;

namespace HVTravel.Web.Models;

public class ContentManagementViewModel
{
    public string ActiveTab { get; set; } = "site";

    public string SelectedSubtab { get; set; } = string.Empty;

    public IReadOnlyList<ContentTabOption> Tabs { get; set; } = Array.Empty<ContentTabOption>();

    public IReadOnlyList<ContentSubtabOption> Subtabs { get; set; } = Array.Empty<ContentSubtabOption>();

    public IReadOnlyList<ContentNavigationChipViewModel> PrimaryTabs { get; set; } = Array.Empty<ContentNavigationChipViewModel>();

    public IReadOnlyList<ContentNavigationChipViewModel> SecondaryTabs { get; set; } = Array.Empty<ContentNavigationChipViewModel>();

    public SiteSettings SiteSettings { get; set; } = new();

    public List<ContentSection> Sections { get; set; } = new();

    public List<ContentSectionEditorCardViewModel> SectionCards { get; set; } = new();

    public string CurrentEditorTitle { get; set; } = string.Empty;

    public string CurrentEditorDescription { get; set; } = string.Empty;

    public List<string> CurrentBreadcrumb { get; set; } = new();

    public string CurrentScopeSummary { get; set; } = string.Empty;

    public ContentPreviewTarget? PreviewTarget { get; set; }

    public string? PreviewUrl { get; set; }

    public string? PreviewUnavailableReason { get; set; }

    public string PreviewSessionKey { get; set; } = string.Empty;

    public string? LivePreviewUrl { get; set; }

    public bool CanInlinePreview { get; set; }

    public string? InlinePreviewUnavailableReason { get; set; }

    public string LivePreviewModeLabel { get; set; } = string.Empty;

    public string? PreviewDebugUrl { get; set; }

    public bool PreviewDiagnosticsEnabled { get; set; }

    public List<ContentDependencyNoteViewModel> DependencyNotes { get; set; } = new();

    public bool IsSiteEditor => ActiveTab == "site";

    public int VisibleSectionCount => Sections.Count;

    public int VisibleFieldCount => Sections.Sum(section => section.Fields.Count);
}

public class ContentTabOption
{
    public string Key { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;
}

public class ContentSubtabOption
{
    public string Key { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;
}

public class ContentNavigationChipViewModel
{
    public string Key { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;

    public string ShortDescription { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;

    public bool IsActive { get; set; }
}

public class ContentPreviewTarget
{
    public string Controller { get; set; } = string.Empty;

    public string Action { get; set; } = string.Empty;

    public Dictionary<string, string?> RouteValues { get; set; } = new();
}

public class ContentDependencyNoteViewModel
{
    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string? NavigateTab { get; set; }

    public string? NavigateSubtab { get; set; }
}

public class ContentSectionEditorCardViewModel
{
    public string SectionKey { get; set; } = string.Empty;

    public string DisplayTitle { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string ScopeLabel { get; set; } = string.Empty;

    public bool AllowSectionSettings { get; set; } = true;

    public bool IsFieldSlice { get; set; }
}

public class ContentAdminEditorDefinition
{
    public string TabKey { get; set; } = string.Empty;

    public string? SubtabKey { get; set; }

    public string PageKey { get; set; } = string.Empty;

    public string NavigationLabel { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string ScopeSummary { get; set; } = string.Empty;

    public List<string> Breadcrumb { get; set; } = new();

    public ContentPreviewTarget? PreviewTarget { get; set; }

    public string? PreviewUnavailableReason { get; set; }

    public IReadOnlyList<ContentSubtabOption> Subtabs { get; set; } = Array.Empty<ContentSubtabOption>();

    public IReadOnlyList<ContentDependencyNoteViewModel> DependencyNotes { get; set; } = Array.Empty<ContentDependencyNoteViewModel>();

    public IReadOnlyList<ContentAdminSectionDefinition> Sections { get; set; } = Array.Empty<ContentAdminSectionDefinition>();
}

public class ContentAdminSectionDefinition
{
    public string SectionKey { get; set; } = string.Empty;

    public string CardTitle { get; set; } = string.Empty;

    public string CardDescription { get; set; } = string.Empty;

    public string ScopeLabel { get; set; } = string.Empty;

    public bool AllowSectionSettings { get; set; } = true;

    public IReadOnlyList<string> EditableFieldKeys { get; set; } = Array.Empty<string>();
}

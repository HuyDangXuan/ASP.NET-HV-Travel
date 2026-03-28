using HVTravel.Domain.Entities;
using HVTravel.Web.Models;
using HVTravel.Web.Security;
using HVTravel.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace HVTravel.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(AuthenticationSchemes = AuthSchemes.AdminScheme, Roles = "Admin,Manager,Staff")]
public class ContentController : Controller
{
    private readonly IPublicContentService _publicContentService;

    public ContentController(IPublicContentService publicContentService)
    {
        _publicContentService = publicContentService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string tab = "site", string? subtab = null)
    {
        ViewData["Title"] = "Nội Dung Website";

        var editor = _publicContentService.ResolveAdminEditor(tab, subtab);
        var previewSessionKey = Guid.NewGuid().ToString("N");
        var siteSettings = await _publicContentService.GetSiteSettingsAsync();
        var pageSections = editor.TabKey == "site"
            ? new List<ContentSection>()
            : await _publicContentService.GetPageSectionsForAdminAsync(editor.PageKey);
        var filteredSections = BuildSectionsForEditor(pageSections, editor);
        var canInlinePreview = editor.PreviewTarget != null && string.IsNullOrWhiteSpace(editor.PreviewUnavailableReason);
        var previewUrl = BuildPreviewUrl(editor.PreviewTarget);

        var viewModel = new ContentManagementViewModel
        {
            ActiveTab = editor.TabKey,
            SelectedSubtab = editor.SubtabKey ?? string.Empty,
            Tabs = _publicContentService.GetTabs(),
            Subtabs = editor.Subtabs,
            PrimaryTabs = BuildPrimaryTabs(editor.TabKey, editor.SubtabKey),
            SecondaryTabs = BuildSecondaryTabs(editor.TabKey, editor.SubtabKey, editor.Subtabs),
            SiteSettings = siteSettings,
            Sections = filteredSections,
            SectionCards = filteredSections
                .Select(section => editor.Sections.First(definition => definition.SectionKey == section.SectionKey))
                .Select(definition => new ContentSectionEditorCardViewModel
                {
                    SectionKey = definition.SectionKey,
                    DisplayTitle = definition.CardTitle,
                    Description = definition.CardDescription,
                    ScopeLabel = definition.ScopeLabel,
                    AllowSectionSettings = definition.AllowSectionSettings,
                    IsFieldSlice = definition.EditableFieldKeys.Count > 0 && !definition.AllowSectionSettings
                })
                .ToList(),
            CurrentEditorTitle = editor.Title,
            CurrentEditorDescription = editor.Description,
            CurrentBreadcrumb = editor.Breadcrumb.ToList(),
            CurrentScopeSummary = editor.ScopeSummary,
            PreviewTarget = editor.PreviewTarget,
            PreviewUrl = previewUrl,
            PreviewUnavailableReason = editor.PreviewUnavailableReason,
            PreviewSessionKey = previewSessionKey,
            LivePreviewUrl = canInlinePreview ? previewUrl : null,
            CanInlinePreview = canInlinePreview,
            InlinePreviewUnavailableReason = canInlinePreview ? null : editor.PreviewUnavailableReason,
            LivePreviewModeLabel = canInlinePreview
                ? "Tự động đồng bộ bản nháp sau khi bạn dừng gõ."
                : "Màn hình này chưa hỗ trợ preview inline.",
            PreviewDebugUrl = previewUrl,
            PreviewDiagnosticsEnabled = canInlinePreview,
            DependencyNotes = editor.DependencyNotes.ToList()
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveSiteSettings(SiteSettingsFormModel form)
    {
        await _publicContentService.SaveSiteSettingsAsync(form.SiteSettings);
        TempData["ContentSuccess"] = "Đã lưu cấu hình site-wide.";
        return RedirectToAction(nameof(Index), new { tab = "site" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveSections(ContentSectionsFormModel form)
    {
        await _publicContentService.SaveSectionsAsync(form.Sections, form.ActiveTab, form.SelectedSubtab);
        TempData["ContentSuccess"] = "Đã lưu nội dung trang.";

        var routeValues = new RouteValueDictionary
        {
            ["tab"] = form.ActiveTab
        };
        if (!string.IsNullOrWhiteSpace(form.SelectedSubtab))
        {
            routeValues["subtab"] = form.SelectedSubtab;
        }

        return RedirectToAction(nameof(Index), routeValues);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PreviewSiteSettings(string previewSessionKey, SiteSettingsFormModel form, string tab = "site", string? subtab = null)
    {
        var editor = _publicContentService.ResolveAdminEditor(tab, subtab);
        if (editor.PreviewTarget == null)
        {
            return BadRequest(new ContentPreviewResponse
            {
                Success = false,
                StatusMessage = editor.PreviewUnavailableReason ?? "Editor này chưa có live preview."
            });
        }

        var snapshot = await _publicContentService.BuildPreviewSnapshotAsync(
            tab,
            subtab,
            previewSessionKey,
            form.SiteSettings,
            null);

        await _publicContentService.StorePreviewSnapshotAsync(snapshot);

        return Json(new ContentPreviewResponse
        {
            Success = true,
            PreviewToken = previewSessionKey,
            LivePreviewUrl = BuildPreviewUrl(editor.PreviewTarget, previewSessionKey, snapshot.UpdatedAtUtc),
            ReloadKey = snapshot.UpdatedAtUtc.Ticks.ToString(),
            StatusMessage = "Preview đang hiển thị bản nháp chưa lưu."
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PreviewSections(string previewSessionKey, ContentSectionsFormModel form)
    {
        var editor = _publicContentService.ResolveAdminEditor(form.ActiveTab, form.SelectedSubtab);
        if (editor.PreviewTarget == null)
        {
            return BadRequest(new ContentPreviewResponse
            {
                Success = false,
                StatusMessage = editor.PreviewUnavailableReason ?? "Editor này chưa có live preview."
            });
        }

        var snapshot = await _publicContentService.BuildPreviewSnapshotAsync(
            form.ActiveTab,
            form.SelectedSubtab,
            previewSessionKey,
            null,
            form.Sections);

        await _publicContentService.StorePreviewSnapshotAsync(snapshot);

        return Json(new ContentPreviewResponse
        {
            Success = true,
            PreviewToken = previewSessionKey,
            LivePreviewUrl = BuildPreviewUrl(editor.PreviewTarget, previewSessionKey, snapshot.UpdatedAtUtc),
            ReloadKey = snapshot.UpdatedAtUtc.Ticks.ToString(),
            StatusMessage = "Preview đang hiển thị bản nháp chưa lưu."
        });
    }

    private IReadOnlyList<ContentNavigationChipViewModel> BuildPrimaryTabs(string activeTab, string? selectedSubtab)
    {
        return _publicContentService.GetTabs()
            .Select(tab => new ContentNavigationChipViewModel
            {
                Key = tab.Key,
                Label = tab.Label,
                ShortDescription = BuildChipDescription(tab.Description),
                Url = Url.Action(nameof(Index), "Content", new
                {
                    area = "Admin",
                    tab = tab.Key,
                    subtab = tab.Key == "booking"
                        ? (string.IsNullOrWhiteSpace(selectedSubtab) ? "consultation" : selectedSubtab)
                        : null
                }) ?? string.Empty,
                IsActive = string.Equals(tab.Key, activeTab, StringComparison.OrdinalIgnoreCase)
            })
            .ToList();
    }

    private IReadOnlyList<ContentNavigationChipViewModel> BuildSecondaryTabs(
        string activeTab,
        string? selectedSubtab,
        IReadOnlyList<ContentSubtabOption> subtabs)
    {
        if (!string.Equals(activeTab, "booking", StringComparison.OrdinalIgnoreCase) || subtabs.Count == 0)
        {
            return Array.Empty<ContentNavigationChipViewModel>();
        }

        return subtabs
            .Select(subtab => new ContentNavigationChipViewModel
            {
                Key = subtab.Key,
                Label = subtab.Label,
                ShortDescription = BuildChipDescription(subtab.Description),
                Url = Url.Action(nameof(Index), "Content", new
                {
                    area = "Admin",
                    tab = activeTab,
                    subtab = subtab.Key
                }) ?? string.Empty,
                IsActive = string.Equals(subtab.Key, selectedSubtab, StringComparison.OrdinalIgnoreCase)
            })
            .ToList();
    }

    private static string BuildChipDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return string.Empty;
        }

        var separators = new[] { ',', ';', '.' };
        var firstChunk = description.Split(separators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(firstChunk))
        {
            return firstChunk;
        }

        return description.Length <= 34 ? description : $"{description[..31]}...";
    }

    private string? BuildPreviewUrl(ContentPreviewTarget? previewTarget, string? previewToken = null, DateTime? previewTimestampUtc = null)
    {
        if (previewTarget == null)
        {
            return null;
        }

        var routeValues = previewTarget.RouteValues.Count == 0
            ? new Dictionary<string, object?>()
            : previewTarget.RouteValues.ToDictionary(item => item.Key, item => (object?)item.Value);

        routeValues["area"] = string.Empty;

        if (!string.IsNullOrWhiteSpace(previewToken))
        {
            routeValues[ContentPreviewConstants.QueryKey] = previewToken;
        }

        if (previewTimestampUtc.HasValue)
        {
            routeValues["previewTs"] = previewTimestampUtc.Value.Ticks;
        }

        return Url.Action(previewTarget.Action, previewTarget.Controller, routeValues);
    }

    private static List<ContentSection> BuildSectionsForEditor(
        IEnumerable<ContentSection> sections,
        ContentAdminEditorDefinition editor)
    {
        var sectionMap = sections.ToDictionary(section => section.SectionKey, section => section, StringComparer.OrdinalIgnoreCase);
        var filteredSections = new List<ContentSection>();

        foreach (var definition in editor.Sections)
        {
            if (!sectionMap.TryGetValue(definition.SectionKey, out var section))
            {
                continue;
            }

            filteredSections.Add(CloneSectionForEditor(section, definition));
        }

        return filteredSections;
    }

    private static ContentSection CloneSectionForEditor(
        ContentSection source,
        ContentAdminSectionDefinition definition)
    {
        var editableKeys = definition.EditableFieldKeys.Count > 0
            ? new HashSet<string>(definition.EditableFieldKeys, StringComparer.OrdinalIgnoreCase)
            : null;

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
            Fields = source.Fields
                .Where(field => editableKeys == null || editableKeys.Contains(field.Key))
                .Select(field => new ContentField
                {
                    Key = field.Key,
                    Label = field.Label,
                    FieldType = field.FieldType,
                    Value = field.Value,
                    Placeholder = field.Placeholder
                })
                .ToList()
        };
    }
}


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
        var siteSettings = await _publicContentService.GetSiteSettingsAsync();
        var pageSections = editor.TabKey == "site"
            ? new List<ContentSection>()
            : await _publicContentService.GetPageSectionsForAdminAsync(editor.PageKey);
        var filteredSections = BuildSectionsForEditor(pageSections, editor);

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

        var clone = new ContentSection
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
            Presentation = ContentPresentationDefaults.CloneSectionPresentation(source.Presentation),
            Fields = source.Fields
                .Where(field => editableKeys == null || editableKeys.Contains(field.Key))
                .Select(field => new ContentField
                {
                    Key = field.Key,
                    Label = field.Label,
                    FieldType = field.FieldType,
                    Value = field.Value,
                    Placeholder = field.Placeholder,
                    Style = ContentPresentationDefaults.CloneTextStyle(field.Style)
                })
                .ToList()
        };

        ContentPresentationDefaults.EnsureSection(clone);
        return clone;
    }
}

using HVTravel.Web.Models;
using HVTravel.Web.Security;
using HVTravel.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
    public async Task<IActionResult> Index(string tab = "site")
    {
        ViewData["Title"] = "Nội Dung Website";

        var normalizedTab = _publicContentService.GetTabs().Any(t => t.Key == tab) ? tab : "site";
        var viewModel = new ContentManagementViewModel
        {
            ActiveTab = normalizedTab,
            Tabs = _publicContentService.GetTabs(),
            Inventory = _publicContentService.GetEditableInventory(),
            SiteSettings = await _publicContentService.GetSiteSettingsAsync(),
            Sections = normalizedTab == "site"
                ? new List<HVTravel.Domain.Entities.ContentSection>()
                : await _publicContentService.GetPageSectionsForAdminAsync(normalizedTab)
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
        await _publicContentService.SaveSectionsAsync(form.Sections);
        TempData["ContentSuccess"] = "Đã lưu nội dung trang.";
        return RedirectToAction(nameof(Index), new { tab = form.ActiveTab });
    }
}

using HVTravel.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace HVTravel.Web.ViewComponents;

public class AdminNotificationDropdownViewComponent : ViewComponent
{
    private readonly AdminNotificationDropdownService _notificationDropdownService;

    public AdminNotificationDropdownViewComponent(AdminNotificationDropdownService notificationDropdownService)
    {
        _notificationDropdownService = notificationDropdownService;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var model = await _notificationDropdownService.GetDropdownAsync();
        return View(model);
    }
}

using HVTravel.Application.Interfaces;
using HVTravel.Application.Models;
using HVTravel.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HVTravel.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(AuthenticationSchemes = AuthSchemes.AdminScheme, Roles = "Admin,Manager,Staff")]
[Route("Admin/[controller]")]
public class CloudinaryController : Controller
{
    private readonly ICloudinaryAssetBrowserService _assetBrowserService;

    public CloudinaryController(ICloudinaryAssetBrowserService assetBrowserService)
    {
        _assetBrowserService = assetBrowserService;
    }

    [HttpGet("Assets")]
    public async Task<IActionResult> Assets(
        [FromQuery] string folder = "",
        [FromQuery] string search = "",
        [FromQuery] string cursor = "",
        [FromQuery] int maxResults = 24,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _assetBrowserService.GetAssetsAsync(new CloudinaryAssetBrowseRequest
            {
                Folder = folder,
                Search = search,
                Cursor = cursor,
                MaxResults = maxResults
            }, cancellationToken);

            return Json(result);
        }
        catch (Exception)
        {
            Response.StatusCode = StatusCodes.Status502BadGateway;
            return Json(new { message = "Không thể tải danh sách ảnh từ Cloudinary." });
        }
    }
}

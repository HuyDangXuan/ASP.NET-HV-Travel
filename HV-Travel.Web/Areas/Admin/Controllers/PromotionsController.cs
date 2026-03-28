using HVTravel.Domain.Entities;
using HVTravel.Domain.Interfaces;
using HVTravel.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HVTravel.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(AuthenticationSchemes = AuthSchemes.AdminScheme, Roles = "Admin,Manager,Staff")]
public class PromotionsController : Controller
{
    private readonly IRepository<Promotion> _promotionRepository;

    public PromotionsController(IRepository<Promotion> promotionRepository)
    {
        _promotionRepository = promotionRepository;
    }

    public async Task<IActionResult> Index()
    {
        ViewData["AdminSection"] = "promotions";
        ViewData["Title"] = "Promotions";
        var items = (await _promotionRepository.GetAllAsync()).OrderByDescending(item => item.ValidTo).ToList();
        return View(items);
    }

    [HttpGet]
    public IActionResult Create()
    {
        ViewData["AdminSection"] = "promotions";
        ViewData["Title"] = "Tạo promotion";
        return View(new Promotion { ValidFrom = DateTime.UtcNow, ValidTo = DateTime.UtcNow.AddMonths(1), IsActive = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Promotion promotion)
    {
        ViewData["AdminSection"] = "promotions";
        ViewData["Title"] = "Tạo promotion";
        if (!ModelState.IsValid)
        {
            return View(promotion);
        }

        promotion.Title = string.IsNullOrWhiteSpace(promotion.Title) ? promotion.Code : promotion.Title.Trim();
        await _promotionRepository.AddAsync(promotion);
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        ViewData["AdminSection"] = "promotions";
        ViewData["Title"] = "Sửa promotion";
        var promotion = await _promotionRepository.GetByIdAsync(id);
        if (promotion == null)
        {
            return NotFound();
        }

        return View(promotion);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, Promotion promotion)
    {
        if (id != promotion.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            ViewData["AdminSection"] = "promotions";
            ViewData["Title"] = "Sửa promotion";
            return View(promotion);
        }

        promotion.Title = string.IsNullOrWhiteSpace(promotion.Title) ? promotion.Code : promotion.Title.Trim();
        await _promotionRepository.UpdateAsync(id, promotion);
        return RedirectToAction(nameof(Index));
    }
}

using HVTravel.Domain.Entities;
using HVTravel.Domain.Interfaces;
using HVTravel.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HVTravel.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(AuthenticationSchemes = AuthSchemes.AdminScheme, Roles = "Admin,Manager,Staff")]
public class ContentHubController : Controller
{
    private readonly IRepository<TravelArticle> _articleRepository;

    public ContentHubController(IRepository<TravelArticle> articleRepository)
    {
        _articleRepository = articleRepository;
    }

    public async Task<IActionResult> Index()
    {
        ViewData["AdminSection"] = "contenthub";
        ViewData["Title"] = "Content hub";
        var items = (await _articleRepository.GetAllAsync()).OrderByDescending(item => item.PublishedAt).ToList();
        return View(items);
    }

    [HttpGet]
    public IActionResult Create()
    {
        ViewData["AdminSection"] = "contenthub";
        ViewData["Title"] = "Tạo bài viết";
        return View(new TravelArticle { PublishedAt = DateTime.UtcNow, IsPublished = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TravelArticle article)
    {
        ViewData["AdminSection"] = "contenthub";
        ViewData["Title"] = "Tạo bài viết";
        if (!ModelState.IsValid)
        {
            return View(article);
        }

        article.Slug = NormalizeSlug(article.Slug, article.Title);
        article.UpdatedAt = DateTime.UtcNow;
        article.CreatedAt = DateTime.UtcNow;
        await _articleRepository.AddAsync(article);
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        ViewData["AdminSection"] = "contenthub";
        ViewData["Title"] = "Sửa bài viết";
        var article = await _articleRepository.GetByIdAsync(id);
        if (article == null)
        {
            return NotFound();
        }

        return View(article);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, TravelArticle article)
    {
        if (id != article.Id)
        {
            return BadRequest();
        }

        ViewData["AdminSection"] = "contenthub";
        ViewData["Title"] = "Sửa bài viết";
        if (!ModelState.IsValid)
        {
            return View(article);
        }

        article.Slug = NormalizeSlug(article.Slug, article.Title);
        article.UpdatedAt = DateTime.UtcNow;
        await _articleRepository.UpdateAsync(id, article);
        return RedirectToAction(nameof(Index));
    }

    private static string NormalizeSlug(string slug, string title)
    {
        var raw = string.IsNullOrWhiteSpace(slug) ? title : slug;
        raw = raw.ToLowerInvariant();
        var chars = raw.Select(ch => char.IsLetterOrDigit(ch) ? ch : '-').ToArray();
        return string.Join(string.Empty, new string(chars).Split('-', StringSplitOptions.RemoveEmptyEntries));
    }
}


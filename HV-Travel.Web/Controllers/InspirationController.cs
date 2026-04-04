using HVTravel.Domain.Entities;
using HVTravel.Domain.Interfaces;
using HVTravel.Web.Models;
using HVTravel.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace HVTravel.Web.Controllers;

public class InspirationController : Controller
{
    private readonly IRepository<TravelArticle> _articleRepository;

    public InspirationController(IRepository<TravelArticle> articleRepository)
    {
        _articleRepository = articleRepository;
    }

    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Cẩm nang & nội dung khám phá";
        ViewData["ActivePage"] = "Inspiration";

        var articles = (await _articleRepository.GetAllAsync())
            .Where(article => article.IsPublished)
            .OrderByDescending(article => article.Featured)
            .ThenByDescending(article => article.PublishedAt)
            .ToList();

        articles.ForEach(article => PublicTextSanitizer.NormalizeArticleForDisplay(article));

        return View(new ContentHubIndexViewModel
        {
            FeaturedArticle = articles.FirstOrDefault(),
            LatestArticles = articles.Skip(1).Take(9).ToList(),
            Categories = articles
                .Select(article => article.Category)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(value => value)
                .ToList()
        });
    }

    public async Task<IActionResult> Details(string slug)
    {
        var article = (await _articleRepository.FindAsync(item => item.Slug == slug && item.IsPublished)).FirstOrDefault();
        if (article == null)
        {
            return NotFound();
        }

        article = PublicTextSanitizer.NormalizeArticleForDisplay(article);

        ViewData["Title"] = article.Title;
        ViewData["ActivePage"] = "Inspiration";
        return View(article);
    }
}
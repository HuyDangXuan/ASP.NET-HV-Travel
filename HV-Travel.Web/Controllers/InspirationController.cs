using System.Globalization;
using System.Text;
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

        var featuredArticle = articles.FirstOrDefault();
        var featuredArticleId = featuredArticle?.Id;
        var remainingArticles = articles
            .Where(article => !string.Equals(article.Id, featuredArticleId, StringComparison.Ordinal))
            .ToList();
        var categoryCarousels = remainingArticles
            .GroupBy(article => string.IsNullOrWhiteSpace(article.Category) ? "Khác" : article.Category, StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var orderedArticles = group
                    .OrderByDescending(article => article.PublishedAt)
                    .ToList();
                var categoryName = orderedArticles
                    .Select(article => article.Category)
                    .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

                categoryName = string.IsNullOrWhiteSpace(categoryName) ? "Khác" : categoryName;

                return new ArticleCategoryCarouselViewModel
                {
                    AnchorId = NormalizeAnchor(categoryName),
                    CategoryName = categoryName,
                    ArticleCount = orderedArticles.Count,
                    Articles = orderedArticles.Take(6).ToList()
                };
            })
            .Where(group => group.Articles.Any())
            .OrderByDescending(group => group.Articles.First().PublishedAt)
            .ToList();

        return View(new ContentHubIndexViewModel
        {
            FeaturedArticle = featuredArticle,
            LatestArticles = remainingArticles.Take(9).ToList(),
            Categories = categoryCarousels
                .Select(group => group.CategoryName)
                .ToList(),
            CategoryCarousels = categoryCarousels
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

    private static string NormalizeAnchor(string value)
    {
        var normalizedValue = PublicTextSanitizer.NormalizeText(value)
            .Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalizedValue.Length);

        foreach (var character in normalizedValue)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        var raw = builder
            .ToString()
            .Normalize(NormalizationForm.FormC)
            .ToLowerInvariant();
        var chars = raw
            .Select(ch => char.IsLetterOrDigit(ch) ? ch : '-')
            .ToArray();
        var slug = string.Join(string.Empty, new string(chars).Split('-', StringSplitOptions.RemoveEmptyEntries));

        return $"category-{(string.IsNullOrWhiteSpace(slug) ? "khac" : slug)}";
    }
}

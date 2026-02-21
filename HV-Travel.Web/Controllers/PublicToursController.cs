using Microsoft.AspNetCore.Mvc;
using HVTravel.Domain.Interfaces;
using HVTravel.Domain.Entities;

namespace HVTravel.Web.Controllers;

public class PublicToursController : Controller
{
    private readonly IRepository<Tour> _tourRepository;

    public PublicToursController(IRepository<Tour> tourRepository)
    {
        _tourRepository = tourRepository;
    }

    public async Task<IActionResult> Index(string? category, string? search, string? sort, int page = 1)
    {
        ViewData["ActivePage"] = "Tours";
        ViewData["Title"] = "Tour Du Lịch";

        var allTours = await _tourRepository.GetAllAsync();
        var tours = allTours.Where(t => t.Status == "Active").AsEnumerable();

        // Filter by category
        if (!string.IsNullOrEmpty(category))
        {
            tours = tours.Where(t => t.Category != null && t.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
            ViewData["CurrentCategory"] = category;
        }

        // Search
        if (!string.IsNullOrEmpty(search))
        {
            tours = tours.Where(t =>
                (t.Name != null && t.Name.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                (t.Description != null && t.Description.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                (t.Destination?.City != null && t.Destination.City.Contains(search, StringComparison.OrdinalIgnoreCase))
            );
            ViewData["CurrentSearch"] = search;
        }

        // Sort
        tours = sort switch
        {
            "price_asc" => tours.OrderBy(t => t.Price?.Adult ?? 0),
            "price_desc" => tours.OrderByDescending(t => t.Price?.Adult ?? 0),
            "rating" => tours.OrderByDescending(t => t.Rating),
            "newest" => tours.OrderByDescending(t => t.CreatedAt),
            _ => tours.OrderByDescending(t => t.Rating)
        };
        ViewData["CurrentSort"] = sort;

        // Gather categories for filter
        var categories = allTours
            .Where(t => t.Status == "Active" && !string.IsNullOrEmpty(t.Category))
            .Select(t => t.Category)
            .Distinct()
            .OrderBy(c => c)
            .ToList();
        ViewData["Categories"] = categories;

        // Pagination
        int pageSize = 9;
        var tourList = tours.ToList();
        int totalItems = tourList.Count;
        int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        page = Math.Max(1, Math.Min(page, totalPages == 0 ? 1 : totalPages));

        var pagedTours = tourList.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        ViewData["CurrentPage"] = page;
        ViewData["TotalPages"] = totalPages;
        ViewData["TotalItems"] = totalItems;

        return View(pagedTours);
    }

    public async Task<IActionResult> Details(string id)
    {
        ViewData["ActivePage"] = "Tours";

        if (string.IsNullOrEmpty(id))
            return RedirectToAction("Index");

        var tour = await _tourRepository.GetByIdAsync(id);
        if (tour == null)
            return NotFound();

        ViewData["Title"] = tour.Name;

        // Get related tours
        var allTours = await _tourRepository.GetAllAsync();
        var relatedTours = allTours
            .Where(t => t.Status == "Active" && t.Id != id && t.Category == tour.Category)
            .OrderByDescending(t => t.Rating)
            .Take(3)
            .ToList();
        ViewData["RelatedTours"] = relatedTours;

        return View(tour);
    }
}

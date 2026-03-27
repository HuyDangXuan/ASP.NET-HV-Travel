using HVTravel.Domain.Entities;
using HVTravel.Domain.Interfaces;
using HVTravel.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace HVTravel.Web.Controllers;

public class PublicToursController : Controller
{
    private readonly IRepository<Tour> _tourRepository;

    public PublicToursController(IRepository<Tour> tourRepository)
    {
        _tourRepository = tourRepository;
    }

    public async Task<IActionResult> Index(
        string? search,
        string? sort,
        string? region = null,
        string? destination = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        int? departureMonth = null,
        int? maxDays = null,
        string? collection = null,
        bool availableOnly = false,
        bool promotionOnly = false,
        int page = 1)
    {
        ViewData["ActivePage"] = "Tours";
        ViewData["Title"] = "Tour Du Lịch";

        var allTours = (await _tourRepository.GetAllAsync())
            .Where(t => IsPubliclyVisible(t.Status))
            .ToList();

        ViewData["Regions"] = allTours
            .Select(t => t.Destination?.Region)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value)
            .ToList();
        ViewData["Destinations"] = allTours
            .Select(t => t.Destination?.City)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value)
            .ToList();

        IEnumerable<Tour> tours = allTours;

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim();
            tours = tours.Where(t =>
                (t.Name != null && t.Name.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase)) ||
                RichTextContentFormatter.ToPlainText(t.ShortDescription).Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) ||
                RichTextContentFormatter.ToPlainText(t.Description).Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) ||
                (t.Destination?.City != null && t.Destination.City.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase)) ||
                (t.Destination?.Region != null && t.Destination.Region.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase)) ||
                (t.Destination?.Country != null && t.Destination.Country.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase))
            );
            ViewData["CurrentSearch"] = normalizedSearch;
        }

        if (!string.IsNullOrWhiteSpace(region))
        {
            tours = tours.Where(t => string.Equals(t.Destination?.Region, region, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(destination))
        {
            tours = tours.Where(t => string.Equals(t.Destination?.City, destination, StringComparison.OrdinalIgnoreCase));
        }

        if (minPrice.HasValue)
        {
            tours = tours.Where(t => (t.Price?.Adult ?? 0) >= minPrice.Value);
        }

        if (maxPrice.HasValue)
        {
            tours = tours.Where(t => (t.Price?.Adult ?? 0) <= maxPrice.Value);
        }

        if (departureMonth.HasValue)
        {
            tours = tours.Where(t => (t.StartDates ?? new List<DateTime>()).Any(date => date.Month == departureMonth.Value));
        }

        if (maxDays.HasValue)
        {
            tours = tours.Where(t => (t.Duration?.Days ?? int.MaxValue) <= maxDays.Value);
        }

        if (!string.IsNullOrWhiteSpace(collection))
        {
            tours = collection.Trim().ToLowerInvariant() switch
            {
                "domestic" => tours.Where(t => string.Equals(t.Destination?.Country, "Vietnam", StringComparison.OrdinalIgnoreCase) || string.Equals(t.Destination?.Country, "Việt Nam", StringComparison.OrdinalIgnoreCase)),
                "international" => tours.Where(t => !string.Equals(t.Destination?.Country, "Vietnam", StringComparison.OrdinalIgnoreCase) && !string.Equals(t.Destination?.Country, "Việt Nam", StringComparison.OrdinalIgnoreCase)),
                "premium" => tours.Where(t => (t.Price?.Adult ?? 0) >= 10000000),
                "budget" => tours.Where(t => (t.Price?.Adult ?? 0) <= 3000000),
                "family" => tours.Where(t => t.MaxParticipants >= 10 && (t.Duration?.Days ?? 0) <= 6),
                "couple" => tours.Where(t => t.MaxParticipants <= 12 && (t.Duration?.Days ?? 0) <= 5),
                "seasonal" => tours.Where(t => (t.StartDates ?? new List<DateTime>()).Any(date => date >= DateTime.UtcNow && date <= DateTime.UtcNow.AddDays(90))),
                "deal" => tours.Where(t => (t.Price?.Discount ?? 0) > 0),
                _ => tours
            };
        }

        if (availableOnly)
        {
            tours = tours.Where(t => t.RemainingSpots > 0 && !string.Equals(t.Status, "SoldOut", StringComparison.OrdinalIgnoreCase));
        }

        if (promotionOnly)
        {
            tours = tours.Where(t => (t.Price?.Discount ?? 0) > 0);
        }

        tours = sort switch
        {
            "price_asc" => tours.OrderBy(t => t.Price?.Adult ?? 0),
            "price_desc" => tours.OrderByDescending(t => t.Price?.Adult ?? 0),
            "rating" => tours.OrderByDescending(t => t.Rating),
            "newest" => tours.OrderByDescending(t => t.CreatedAt),
            "departure" => tours.OrderBy(t => t.StartDates?.Where(d => d >= DateTime.UtcNow).DefaultIfEmpty(DateTime.MaxValue).Min()),
            _ => tours.OrderByDescending(t => t.Rating)
        };

        ViewData["CurrentSort"] = sort;
        ViewData["CurrentRegion"] = region;
        ViewData["CurrentDestination"] = destination;
        ViewData["CurrentMinPrice"] = minPrice;
        ViewData["CurrentMaxPrice"] = maxPrice;
        ViewData["CurrentDepartureMonth"] = departureMonth;
        ViewData["CurrentMaxDays"] = maxDays;
        ViewData["CurrentCollection"] = collection;
        ViewData["CurrentAvailableOnly"] = availableOnly;
        ViewData["CurrentPromotionOnly"] = promotionOnly;

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
        if (tour == null || !IsPubliclyVisible(tour.Status))
            return NotFound();

        ViewData["Title"] = tour.Name;

        var allTours = await _tourRepository.GetAllAsync();
        var relatedTours = allTours
            .Where(t => IsPubliclyVisible(t.Status) && t.Id != id)
            .OrderByDescending(t => string.Equals(t.Destination?.City, tour.Destination?.City, StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(t => string.Equals(t.Destination?.Region, tour.Destination?.Region, StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(t => (t.Price?.Discount ?? 0) > 0)
            .ThenByDescending(t => t.Rating)
            .Take(4)
            .ToList();
        ViewData["RelatedTours"] = relatedTours;

        return View(tour);
    }

    private static bool IsPubliclyVisible(string? status)
    {
        return status is "Active" or "ComingSoon" or "SoldOut";
    }
}

using HVTravel.Domain.Entities;
using HVTravel.Domain.Interfaces;
using HVTravel.Domain.Models;
using HVTravel.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace HVTravel.Web.Controllers;

public class PublicToursController : Controller
{
    private readonly ITourRepository _tourRepository;

    public PublicToursController(ITourRepository tourRepository)
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
        int travellers = 0,
        string? confirmationType = null,
        string? cancellationType = null,
        int page = 1)
    {
        ViewData["ActivePage"] = "Tours";
        ViewData["Title"] = "Tour du lịch";

        var result = await _tourRepository.SearchAsync(new TourSearchRequest
        {
            Search = search,
            Sort = sort,
            Region = region,
            Destination = destination,
            MinPrice = minPrice,
            MaxPrice = maxPrice,
            DepartureMonth = departureMonth,
            MaxDays = maxDays,
            Collection = collection,
            AvailableOnly = availableOnly,
            PromotionOnly = promotionOnly,
            Travellers = travellers,
            ConfirmationType = confirmationType,
            CancellationType = cancellationType,
            Page = page,
            PageSize = 9,
            PublicOnly = true
        });

        ViewData["Regions"] = result.Regions.Select(option => option.Value).ToList();
        ViewData["RegionFacets"] = result.Regions;
        ViewData["Destinations"] = result.Destinations.Select(option => option.Value).ToList();
        ViewData["DestinationFacets"] = result.Destinations;
        ViewData["ConfirmationTypeFacets"] = result.ConfirmationTypes;
        ViewData["CancellationTypeFacets"] = result.CancellationTypes;

        ViewData["CurrentSearch"] = search?.Trim();
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
        ViewData["CurrentTravellers"] = travellers;
        ViewData["CurrentConfirmationType"] = confirmationType;
        ViewData["CurrentCancellationType"] = cancellationType;
        ViewData["CurrentPage"] = result.CurrentPage;
        ViewData["TotalPages"] = result.TotalPages;
        ViewData["TotalItems"] = result.TotalItems;

        result.Items.ForEach(tour => PublicTextSanitizer.NormalizeTourForDisplay(tour));

        return View(result.Items);
    }

    public async Task<IActionResult> Details(string id)
    {
        ViewData["ActivePage"] = "Tours";

        if (string.IsNullOrWhiteSpace(id))
        {
            return RedirectToAction(nameof(Index));
        }

        var tour = await _tourRepository.GetByIdAsync(id) ?? await _tourRepository.GetBySlugAsync(id);
        if (tour == null || !IsPubliclyVisible(tour.Status))
        {
            return NotFound();
        }

        tour = PublicTextSanitizer.NormalizeTourForDisplay(tour);

        var description = tour.Seo?.Description;
        if (string.IsNullOrWhiteSpace(description))
        {
            description = RichTextContentFormatter.ToPlainTextSummary(tour.ShortDescription ?? tour.Description, 160);
        }

        ViewData["Title"] = string.IsNullOrWhiteSpace(tour.Seo?.Title) ? tour.Name : tour.Seo.Title;
        ViewData["Description"] = description;
        ViewData["CanonicalUrl"] = Url.Action(nameof(Details), "PublicTours", new { id = string.IsNullOrWhiteSpace(tour.Slug) ? tour.Id : tour.Slug }, Request.Scheme);
        ViewData["OpenGraphTitle"] = ViewData["Title"];
        ViewData["OpenGraphDescription"] = description;
        ViewData["OpenGraphImage"] = !string.IsNullOrWhiteSpace(tour.Seo?.OpenGraphImageUrl)
            ? tour.Seo.OpenGraphImageUrl
            : tour.Images?.FirstOrDefault();

        var searchContext = await _tourRepository.SearchAsync(new TourSearchRequest
        {
            Region = tour.Destination?.Region,
            Page = 1,
            PageSize = 12,
            PublicOnly = true
        });

        var relatedTours = searchContext.Items
            .Where(item => item.Id != tour.Id)
            .OrderByDescending(item => string.Equals(item.Destination?.City, tour.Destination?.City, StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(item => string.Equals(item.Destination?.Region, tour.Destination?.Region, StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(item => item.EffectiveDepartures.Any(departure => departure.RemainingCapacity is > 0 and <= 5))
            .ThenByDescending(item => item.Rating)
            .Take(4)
            .ToList();

        relatedTours.ForEach(tour => PublicTextSanitizer.NormalizeTourForDisplay(tour));
        ViewData["RelatedTours"] = relatedTours;
        return View(tour);
    }

    private static bool IsPubliclyVisible(string? status)
    {
        return status is "Active" or "ComingSoon" or "SoldOut";
    }
}


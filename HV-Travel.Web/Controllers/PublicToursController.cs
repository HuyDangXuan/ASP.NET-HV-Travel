using HVTravel.Domain.Entities;
using HVTravel.Domain.Interfaces;
using HVTravel.Domain.Models;
using HVTravel.Web.Services;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;

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

        var request = new TourSearchRequest
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
        };

        var result = await _tourRepository.SearchAsync(request);

        var regionValues = new List<string>();
        foreach (var option in result.Regions)
        {
            regionValues.Add(option.Value);
        }

        var destinationValues = new List<string>();
        foreach (var option in result.Destinations)
        {
            destinationValues.Add(option.Value);
        }

        ViewData["Regions"] = regionValues;
        ViewData["RegionFacets"] = result.Regions;
        ViewData["Destinations"] = destinationValues;
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

        foreach (var item in result.Items)
        {
            PublicTextSanitizer.NormalizeTourForDisplay(item);
        }

        return View(result.Items);
    }

    public async Task<IActionResult> Details(string id)
    {
        ViewData["ActivePage"] = "Tours";

        if (string.IsNullOrWhiteSpace(id))
        {
            return RedirectToAction(nameof(Index));
        }

        Tour? tour = null;
        if (ObjectId.TryParse(id, out _))
        {
            tour = await _tourRepository.GetByIdAsync(id);
        }

        if (tour == null)
        {
            tour = await _tourRepository.GetBySlugAsync(id);
        }

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
            : tour.Images.FirstOrDefault();

        var searchContext = await _tourRepository.SearchAsync(new TourSearchRequest
        {
            Region = tour.Destination?.Region,
            Page = 1,
            PageSize = 12,
            PublicOnly = true
        });

        var relatedTours = new List<Tour>();
        foreach (var item in searchContext.Items)
        {
            if (item.Id == tour.Id)
            {
                continue;
            }

            PublicTextSanitizer.NormalizeTourForDisplay(item);
            relatedTours.Add(item);
            if (relatedTours.Count == 4)
            {
                break;
            }
        }

        var dossierPresenter = new PublicTourDossierPresenter();
        return View(dossierPresenter.Build(tour, relatedTours));
    }

    private static bool IsPubliclyVisible(string? status)
    {
        return status is "Active" or "ComingSoon" or "SoldOut";
    }
}

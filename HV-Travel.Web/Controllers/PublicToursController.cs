using HVTravel.Domain.Entities;
using HVTravel.Application.Interfaces;
using HVTravel.Application.Models;
using HVTravel.Application.Services;
using HVTravel.Domain.Interfaces;
using HVTravel.Domain.Models;
using HVTravel.Web.Models;
using HVTravel.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace HVTravel.Web.Controllers;

public class PublicToursController : Controller
{
    private readonly ITourRepository _tourRepository;
    private readonly ITourSearchService _tourSearchService;
    private readonly IRouteRecommendationService _routeRecommendationService;

    public PublicToursController(
        ITourRepository tourRepository,
        ITourSearchService? tourSearchService = null,
        IRouteRecommendationService? routeRecommendationService = null)
    {
        _tourRepository = tourRepository;
        _routeRecommendationService = routeRecommendationService ?? new RouteRecommendationService(new RouteInsightService());
        _tourSearchService = tourSearchService ?? new TourSearchService(_tourRepository, _routeRecommendationService);
    }

    public async Task<IActionResult> Index(
        string? search,
        string? sort,
        string? routeStyle = null,
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
        var normalizedRouteStyle = RouteRecommendationStyles.Normalize(routeStyle);
        var normalizedSort = NormalizeSort(sort);
        var useRecommendationRanking = string.Equals(normalizedSort, "recommended", StringComparison.OrdinalIgnoreCase);

        var result = await _tourSearchService.SearchAsync(new TourSearchRequest
        {
            Search = search,
            Sort = normalizedSort,
            RouteStyle = normalizedRouteStyle,
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
            UseRecommendationRanking = useRecommendationRanking,
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
        ViewData["CurrentSort"] = normalizedSort;
        ViewData["CurrentRouteStyle"] = normalizedRouteStyle;
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

    public async Task<IActionResult> Details(string id, string? routeStyle = null)
    {
        ViewData["ActivePage"] = "Tours";

        if (string.IsNullOrWhiteSpace(id))
        {
            return RedirectToAction(nameof(Index));
        }

        var normalizedRouteStyle = RouteRecommendationStyles.Normalize(routeStyle);
        var tour = await ResolveTourByPublicIdentifierAsync(id);
        if (tour == null || !IsPubliclyVisible(tour.Status))
        {
            return NotFound();
        }

        tour = PublicTextSanitizer.NormalizeTourForDisplay(tour);
        ViewData["RouteOverview"] = PublicTourRouteOverviewBuilder.Build(tour);
        ViewData["CurrentRouteStyle"] = normalizedRouteStyle;

        var description = tour.Seo?.Description;
        if (string.IsNullOrWhiteSpace(description))
        {
            description = RichTextContentFormatter.ToPlainTextSummary(tour.ShortDescription ?? tour.Description, 160);
        }

        ViewData["Title"] = string.IsNullOrWhiteSpace(tour.Seo?.Title) ? tour.Name : tour.Seo.Title;
        ViewData["Description"] = description;
        ViewData["CanonicalUrl"] = Url.Action(nameof(Details), "PublicTours", new { id = PublicTourIdentifierHelper.GetDetailIdentifier(tour) }, Request.Scheme);
        ViewData["OpenGraphTitle"] = ViewData["Title"];
        ViewData["OpenGraphDescription"] = description;
        ViewData["OpenGraphImage"] = !string.IsNullOrWhiteSpace(tour.Seo?.OpenGraphImageUrl)
            ? tour.Seo.OpenGraphImageUrl
            : tour.Images?.FirstOrDefault();
        ViewData["RelatedToursRouteStyleText"] = GetRelatedToursRouteStyleText(normalizedRouteStyle);

        var searchContext = await _tourSearchService.SearchAsync(new TourSearchRequest
        {
            Region = tour.Destination?.Region,
            Page = 1,
            PageSize = int.MaxValue,
            UseRecommendationRanking = true,
            PublicOnly = true
        });

        var relatedTours = _routeRecommendationService
            .Recommend(searchContext.Items, new RouteRecommendationRequest
            {
                RouteStyle = normalizedRouteStyle,
                CurrentTourId = tour.Id,
                CurrentCity = tour.Destination?.City,
                CurrentRegion = tour.Destination?.Region
            })
            .Items
            .Take(4)
            .ToList();

        relatedTours.ForEach(tour => PublicTextSanitizer.NormalizeTourForDisplay(tour));
        ViewData["RelatedTours"] = relatedTours;
        return View(tour);
    }

    private async Task<Tour?> ResolveTourByPublicIdentifierAsync(string id)
    {
        if (!PublicTourIdentifierHelper.IsObjectId(id))
        {
            return await _tourRepository.GetBySlugAsync(id);
        }

        return await _tourRepository.GetByIdAsync(id) ?? await _tourRepository.GetBySlugAsync(id);
    }

    private static bool IsPubliclyVisible(string? status)
    {
        return status is "Active" or "ComingSoon" or "SoldOut";
    }

    private static string NormalizeSort(string? sort)
    {
        return string.IsNullOrWhiteSpace(sort) ? "recommended" : sort.Trim();
    }

    private static string GetRelatedToursRouteStyleText(string routeStyle)
    {
        return routeStyle switch
        {
            RouteRecommendationStyles.Compact => "Gợi ý ưu tiên hành trình gọn và giảm thời gian di chuyển.",
            RouteRecommendationStyles.Highlights => "Gợi ý ưu tiên các tour có điểm dừng nổi bật và trải nghiệm đậm hơn.",
            _ => "Gợi ý ưu tiên lịch trình cân bằng giữa trải nghiệm, chi phí và thời lượng."
        };
    }
}


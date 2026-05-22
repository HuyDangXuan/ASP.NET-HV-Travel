using HVTravel.Application.Interfaces;
using HVTravel.Application.Models;
using HVTravel.Domain.Entities;
using HVTravel.Domain.Interfaces;
using HVTravel.Domain.Models;
using HVTravel.Web.Models;
using HVTravel.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace HVTravel.Web.Controllers;

public class TripPlannerController : Controller
{
    private const int CandidatePoolSize = 60;

    private readonly ITourRepository _tourRepository;
    private readonly ITripPlannerService _tripPlannerService;

    public TripPlannerController(ITourRepository tourRepository, ITripPlannerService tripPlannerService)
    {
        _tourRepository = tourRepository;
        _tripPlannerService = tripPlannerService;
    }

    [HttpGet]
    public IActionResult Index(string? routeStyle = null)
    {
        ViewData["ActivePage"] = "Tours";
        ViewData["Title"] = "Lập kế hoạch";
        ViewData["CurrentRouteStyle"] = RouteRecommendationStyles.Normalize(routeStyle);
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Preview([FromBody] TripPlannerPreviewRequest request)
    {
        request ??= new TripPlannerPreviewRequest();

        var warnings = new List<string>();
        var selectedTours = new List<Tour>();
        foreach (var identifier in request.TourIds.Where(value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.Ordinal))
        {
            var tour = await ResolvePublicTourAsync(identifier.Trim());
            if (tour == null)
            {
                warnings.Add($"Tour \"{identifier}\" không tìm thấy hoặc chưa được công khai.");
                continue;
            }

            selectedTours.Add(PublicTextSanitizer.NormalizeTourForDisplay(tour));
        }

        var candidateSearch = await _tourRepository.SearchAsync(new TourSearchRequest
        {
            Region = selectedTours.FirstOrDefault()?.Destination?.Region,
            Page = 1,
            PageSize = CandidatePoolSize,
            PublicOnly = true,
            UseRecommendationRanking = false
        });

        var candidateTours = candidateSearch.Items
            .Where(tour => IsPubliclyVisible(tour.Status))
            .Select(PublicTextSanitizer.NormalizeTourForDisplay)
            .ToList();

        var plan = _tripPlannerService.Build(new TripPlannerRequest
        {
            SelectedTours = selectedTours,
            CandidateTours = candidateTours,
            RouteStyle = request.RouteStyle,
            Travellers = request.Travellers,
            MaxDays = request.MaxDays
        });

        warnings.AddRange(plan.Warnings.Select(warning => warning.Message));

        return Json(new TripPlannerPreviewResponse
        {
            Plan = plan,
            Warnings = warnings.Distinct(StringComparer.Ordinal).ToList()
        });
    }

    private async Task<Tour?> ResolvePublicTourAsync(string identifier)
    {
        Tour? tour;
        if (PublicTourIdentifierHelper.IsObjectId(identifier))
        {
            tour = await _tourRepository.GetByIdAsync(identifier) ?? await _tourRepository.GetBySlugAsync(identifier);
        }
        else
        {
            tour = await _tourRepository.GetBySlugAsync(identifier);
        }

        return tour != null && IsPubliclyVisible(tour.Status) ? tour : null;
    }

    private static bool IsPubliclyVisible(string? status)
    {
        return status is "Active" or "ComingSoon" or "SoldOut";
    }
}

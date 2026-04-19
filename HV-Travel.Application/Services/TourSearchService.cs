using HVTravel.Application.Interfaces;
using HVTravel.Application.Models;
using HVTravel.Domain.Interfaces;
using HVTravel.Domain.Models;

namespace HVTravel.Application.Services;

public class TourSearchService : ITourSearchService
{
    private readonly ITourRepository _tourRepository;
    private readonly IRouteRecommendationService _routeRecommendationService;

    public TourSearchService(ITourRepository tourRepository, IRouteRecommendationService? routeRecommendationService = null)
    {
        _tourRepository = tourRepository;
        _routeRecommendationService = routeRecommendationService ?? new RouteRecommendationService(new RouteInsightService());
    }

    public async Task<TourSearchResult> SearchAsync(TourSearchRequest request)
    {
        request ??= new TourSearchRequest();
        request.RouteStyle = RouteRecommendationStyles.Normalize(request.RouteStyle);

        if (!request.UseRecommendationRanking)
        {
            return await _tourRepository.SearchAsync(request);
        }

        var candidateRequest = CloneRequest(request);
        candidateRequest.Page = 1;
        candidateRequest.PageSize = int.MaxValue;
        candidateRequest.UseRecommendationRanking = true;

        var candidateResult = await _tourRepository.SearchAsync(candidateRequest);
        var recommendation = _routeRecommendationService.Recommend(candidateResult.Items, new RouteRecommendationRequest
        {
            RouteStyle = request.RouteStyle,
            Travellers = request.Travellers
        });

        var rankedItems = recommendation.Items.ToList();
        var pageSize = Math.Max(1, request.PageSize);
        var totalItems = rankedItems.Count;
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        var currentPage = Math.Max(1, Math.Min(request.Page, totalPages == 0 ? 1 : totalPages));
        var pagedItems = rankedItems
            .Skip((currentPage - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new TourSearchResult
        {
            Items = pagedItems,
            TotalItems = totalItems,
            TotalPages = totalPages,
            CurrentPage = currentPage,
            Regions = candidateResult.Regions,
            Destinations = candidateResult.Destinations,
            ConfirmationTypes = candidateResult.ConfirmationTypes,
            CancellationTypes = candidateResult.CancellationTypes
        };
    }

    private static TourSearchRequest CloneRequest(TourSearchRequest request)
    {
        return new TourSearchRequest
        {
            Search = request.Search,
            Sort = request.Sort,
            RouteStyle = request.RouteStyle,
            Region = request.Region,
            Destination = request.Destination,
            MinPrice = request.MinPrice,
            MaxPrice = request.MaxPrice,
            DepartureMonth = request.DepartureMonth,
            MaxDays = request.MaxDays,
            Collection = request.Collection,
            AvailableOnly = request.AvailableOnly,
            PromotionOnly = request.PromotionOnly,
            Travellers = request.Travellers,
            ConfirmationType = request.ConfirmationType,
            CancellationType = request.CancellationType,
            UseRecommendationRanking = request.UseRecommendationRanking,
            PublicOnly = request.PublicOnly,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }
}

using HVTravel.Application.Interfaces;
using HVTravel.Domain.Interfaces;
using HVTravel.Domain.Models;

namespace HVTravel.Application.Services;

public class TourSearchService : ITourSearchService
{
    private readonly ITourRepository _tourRepository;

    public TourSearchService(ITourRepository tourRepository)
    {
        _tourRepository = tourRepository;
    }

    public Task<TourSearchResult> SearchAsync(TourSearchRequest request)
    {
        return _tourRepository.SearchAsync(request);
    }
}

using HVTravel.Application.Interfaces;
using HVTravel.Domain.Interfaces;
using HVTravel.Domain.Models;

namespace HVTravel.Application.Services;

public class MongoTourSearchBackend : ITourSearchBackend
{
    private readonly ITourRepository _tourRepository;

    public MongoTourSearchBackend(ITourRepository tourRepository)
    {
        _tourRepository = tourRepository;
    }

    public string Name => "mongo";

    public int Priority => 0;

    public Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    public Task<TourSearchResult> SearchAsync(TourSearchRequest request, CancellationToken cancellationToken = default)
    {
        return _tourRepository.SearchAsync(request);
    }
}

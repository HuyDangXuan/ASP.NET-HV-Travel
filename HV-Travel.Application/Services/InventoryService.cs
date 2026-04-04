using HVTravel.Application.Interfaces;
using HVTravel.Domain.Interfaces;

namespace HVTravel.Application.Services;

public class InventoryService : IInventoryService
{
    private readonly ITourRepository _tourRepository;

    public InventoryService(ITourRepository tourRepository)
    {
        _tourRepository = tourRepository;
    }

    public Task<bool> ReserveDepartureAsync(string tourId, string departureId, int travellerCount)
    {
        return _tourRepository.ReserveDepartureAsync(tourId, departureId, travellerCount);
    }
}

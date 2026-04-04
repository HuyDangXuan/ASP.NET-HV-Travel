using HVTravel.Domain.Entities;
using HVTravel.Domain.Models;

namespace HVTravel.Domain.Interfaces
{
    public interface ITourRepository : IRepository<Tour>
    {
        Task<TourSearchResult> SearchAsync(TourSearchRequest request);
        Task<Tour?> GetBySlugAsync(string slug);

        /// <summary>
        /// Atomically increments the participant count of a tour if spots are available.
        /// </summary>
        Task<bool> IncrementParticipantsAsync(string tourId, int count);

        /// <summary>
        /// Atomically reserves seats on a specific departure when available.
        /// </summary>
        Task<bool> ReserveDepartureAsync(string tourId, string departureId, int travellerCount);
    }
}

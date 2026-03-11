using System.Threading.Tasks;
using HVTravel.Domain.Entities;

namespace HVTravel.Domain.Interfaces
{
    public interface ITourRepository : IRepository<Tour>
    {
        /// <summary>
        /// atomically increments the participant count of a tour if spots are available.
        /// </summary>
        /// <param name="tourId">The ID of the tour.</param>
        /// <param name="count">Number of participants to add.</param>
        /// <returns>True if the update was successful, False if the tour is full or doesn't exist.</returns>
        Task<bool> IncrementParticipantsAsync(string tourId, int count);
    }
}

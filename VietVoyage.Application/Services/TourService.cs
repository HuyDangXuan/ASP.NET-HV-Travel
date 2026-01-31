using System.Linq;
using VietVoyage.Application.Interfaces;
using VietVoyage.Domain.Entities;
using VietVoyage.Domain.Interfaces;

namespace VietVoyage.Application.Services
{
    public class TourService : ITourService
    {
        private readonly IRepository<Tour> _tourRepository;

        public TourService(IRepository<Tour> tourRepository)
        {
            _tourRepository = tourRepository;
        }

        public async Task<IEnumerable<Tour>> GetAllToursAsync()
        {
            return await _tourRepository.GetAllAsync();
        }

        public async Task<Tour> GetTourByIdAsync(string id)
        {
            return await _tourRepository.GetByIdAsync(id);
        }

        public async Task CreateTourAsync(Tour tour)
        {
            await _tourRepository.AddAsync(tour);
        }

        public async Task UpdateTourAsync(Tour tour)
        {
            await _tourRepository.UpdateAsync(tour.Id, tour);
        }

        public async Task DeleteTourAsync(string id)
        {
            await _tourRepository.DeleteAsync(id);
        }
    }
}

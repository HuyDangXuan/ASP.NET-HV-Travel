using System.Collections.Generic;
using System.Threading.Tasks;
using VietVoyage.Domain.Entities;

namespace VietVoyage.Application.Interfaces
{
    public interface IAuthService
    {
        Task<User> ValidateUserAsync(string email, string password);
        Task<User> RegisterAsync(User user);
    }

    public interface ITourService
    {
        Task<IEnumerable<Tour>> GetAllToursAsync();
        Task<Tour> GetTourByIdAsync(string id);
        Task CreateTourAsync(Tour tour);
        Task UpdateTourAsync(Tour tour);
        Task DeleteTourAsync(string id);
    }

    public interface IDashboardService
    {
        Task<object> GetRevenueStatsAsync(string range);
        Task<object> GetRecentBookingsAsync();
    }
}

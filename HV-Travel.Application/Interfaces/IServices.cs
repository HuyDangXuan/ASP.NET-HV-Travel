using System.Collections.Generic;
using System.Threading.Tasks;
using HVTravel.Application.Models;
using HVTravel.Domain.Entities;

namespace HVTravel.Application.Interfaces
{
    public interface IAuthService
    {
        Task<User> ValidateUserAsync(string email, string password);
        Task<User> RegisterAsync(User user);
        Task<bool> CheckEmailExistsAsync(string email);
        Task<bool> UpdatePasswordAsync(string email, string newPassword);
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
        Task<DashboardRevenueOverviewResult> GetRevenueOverviewAsync(DashboardRevenueRange defaultRange);
        Task<DashboardRevenueStatsResult> GetRevenueStatsAsync(DashboardRevenueRange range);
        Task<IEnumerable<Booking>> GetRecentBookingsAsync();
    }
}

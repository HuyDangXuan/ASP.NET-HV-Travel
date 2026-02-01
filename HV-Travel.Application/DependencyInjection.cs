using Microsoft.Extensions.DependencyInjection;
using HVTravel.Application.Services;
using HVTravel.Application.Interfaces;

namespace HVTravel.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<ITourService, TourService>();
            services.AddScoped<IDashboardService, DashboardService>();
            return services;
        }
    }
}

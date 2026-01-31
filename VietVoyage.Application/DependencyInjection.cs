using Microsoft.Extensions.DependencyInjection;
using VietVoyage.Application.Services;
using VietVoyage.Application.Interfaces;

namespace VietVoyage.Application
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

using HVTravel.Application.Interfaces;
using HVTravel.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace HVTravel.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<ITourService, TourService>();
            services.AddScoped<IDashboardService, DashboardService>();

            services.AddScoped<ITourSearchService, TourSearchService>();
            services.AddScoped<IPromotionEngine, PromotionEngine>();
            services.AddScoped<IPricingService, PricingService>();
            services.AddScoped<IInventoryService, InventoryService>();
            services.AddScoped<ICheckoutService, CheckoutService>();
            services.AddScoped<IRouteTravelEstimator, RouteTravelEstimator>();
            services.AddScoped<IRouteInsightService, RouteInsightService>();
            services.AddScoped<IRouteOptimizationService, RouteOptimizationService>();
            services.AddScoped<IRouteRecommendationService, RouteRecommendationService>();
            services.AddScoped<ITripPlannerService, TripPlannerService>();
            services.AddScoped<IAnalyticsTracker, NoopAnalyticsTracker>();

            return services;
        }
    }
}

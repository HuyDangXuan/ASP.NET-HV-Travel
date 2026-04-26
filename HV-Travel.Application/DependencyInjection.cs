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
            services.AddScoped<IBookingLookupSearchService, BookingLookupSearchService>();
            services.AddScoped<IAdminTourSearchService, AdminTourSearchService>();
            services.AddScoped<IAdminBookingSearchService, AdminBookingSearchService>();
            services.AddScoped<IAdminUserSearchService, AdminUserSearchService>();
            services.AddScoped<IAdminReviewSearchService, AdminReviewSearchService>();
            services.AddScoped<IAdminServiceLeadSearchService, AdminServiceLeadSearchService>();
            services.AddScoped<IAdminCustomerSearchService, AdminCustomerSearchService>();
            services.AddScoped<IAdminPaymentSearchService, AdminPaymentSearchService>();

            services.AddScoped<ITourSearchBackend, MongoTourSearchBackend>();
            services.AddScoped<ITourSearchBackend, MeilisearchTourSearchBackend>();
            services.AddScoped<ITourSearchService, TourSearchService>();
            services.AddScoped<SearchIndexingService>();
            services.AddScoped<ITourSearchIndexingService>(sp => sp.GetRequiredService<SearchIndexingService>());
            services.AddScoped<ISearchIndexingService>(sp => sp.GetRequiredService<SearchIndexingService>());
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

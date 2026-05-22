using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using HVTravel.Application.Interfaces;
using HVTravel.Application.Models;
using HVTravel.Domain.Interfaces;
using HVTravel.Infrastructure.Data;
using HVTravel.Infrastructure.Data.Serialization;
using HVTravel.Infrastructure.Repositories;
using HVTravel.Infrastructure.Services;

namespace HVTravel.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            MongoSerializationBootstrapper.Register();
            services.AddSingleton<MongoContext>();
            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            services.AddScoped<ITourRepository, TourRepository>();
            services.AddSingleton<IOptions<MeilisearchOptions>>(_ =>
                Options.Create(configuration.GetSection("Meilisearch").Get<MeilisearchOptions>() ?? new MeilisearchOptions()));
            services.AddScoped<IMeilisearchDocumentIndexClient, MeilisearchDocumentIndexClient>();
            services.AddScoped<IMeilisearchTourIndexClient, MeilisearchTourIndexClient>();
            services.AddScoped<HVTravel.Application.Interfaces.IEmailService, HVTravel.Infrastructure.Services.EmailService>();
            services.AddScoped<HVTravel.Application.Interfaces.ICloudinaryAssetBrowserService>(sp => new HVTravel.Infrastructure.Services.CloudinaryAssetBrowserService(new HttpClient(), sp.GetRequiredService<IConfiguration>()));
            
            // Register specific repositories here if needed
            
            return services;
        }
    }
}

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using HVTravel.Domain.Interfaces;
using HVTravel.Infrastructure.Data;
using HVTravel.Infrastructure.Repositories;

namespace HVTravel.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<MongoContext>();
            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            
            // Register specific repositories here if needed
            
            return services;
        }
    }
}

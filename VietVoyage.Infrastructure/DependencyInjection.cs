using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VietVoyage.Domain.Interfaces;
using VietVoyage.Infrastructure.Data;
using VietVoyage.Infrastructure.Repositories;

namespace VietVoyage.Infrastructure
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

using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using VietVoyage.Domain.Entities;
using VietVoyage.Domain.Interfaces;

namespace VietVoyage.Infrastructure.Data
{
    public static class DbInitializer
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            var userRepository = serviceProvider.GetRequiredService<IRepository<User>>();

            var adminUser = await userRepository.FindAsync(u => u.Email == "admin@hvtravel.com");
            if (!adminUser.Any())
            {
                var user = new User
                {
                    Email = "admin@hvtravel.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                    Role = "Admin",
                    FullName = "Super Admin",
                    CreatedAt = DateTime.UtcNow
                };
                await userRepository.AddAsync(user);
            }
            else 
            {
                // Fix existing bad password if necessary
                var user = adminUser.First();
                if (!user.PasswordHash.StartsWith("$2"))
                {
                    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123");
                    await userRepository.UpdateAsync(user.Id, user);
                }
            }
        }
    }
}

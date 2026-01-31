using System.Linq;
using VietVoyage.Application.Interfaces;
using VietVoyage.Domain.Entities;
using VietVoyage.Domain.Interfaces;

namespace VietVoyage.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IRepository<User> _userRepository;

        public AuthService(IRepository<User> userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<User> ValidateUserAsync(string email, string password)
        {
            var users = await _userRepository.FindAsync(u => u.Email == email);
            var user = users.FirstOrDefault();

            if (user != null && BCrypt.Net.BCrypt.Verify(password, user.PasswordHash)) 
            {
                return user;
            }
            return null;
        }

        public async Task<User> RegisterAsync(User user)
        {
            // Check if email exists
            var existingUsers = await _userRepository.FindAsync(u => u.Email == user.Email);
            if (existingUsers.Any())
            {
                throw new Exception("Email already exists");
            }

            // Hash password
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash);
            
            await _userRepository.AddAsync(user);
            return user;
        }
    }
}

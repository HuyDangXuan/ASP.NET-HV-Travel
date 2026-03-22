using System.Linq.Expressions;
using System.Security.Claims;
using HVTravel.Domain.Entities;
using HVTravel.Domain.Interfaces;
using HVTravel.Domain.Models;
using HVTravel.Web.Models;
using HVTravel.Web.Services;

namespace HV_Travel.Web.Tests;

public class SupportChatServiceTests
{
    [Fact]
    public async Task BootstrapConversationAsync_RejectsMissingGuestEmail()
    {
        var service = CreateService();
        var request = new ChatBootstrapRequest
        {
            VisitorSessionId = "visitor-123",
            DisplayName = "Nguyen Van A",
            Email = "",
            PhoneNumber = "0901234567",
            SourcePage = "/tour"
        };

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => service.BootstrapConversationAsync(request, CreateGuestUser()));

        Assert.Equal("Email is required.", exception.Message);
    }

    [Fact]
    public async Task BootstrapConversationAsync_UsesCustomerClaimsWhenRequestIsBlank()
    {
        var conversationRepository = new InMemoryRepository<ChatConversation>();
        var messageRepository = new InMemoryRepository<ChatMessage>();
        var service = new SupportChatService(conversationRepository, messageRepository);

        var request = new ChatBootstrapRequest
        {
            VisitorSessionId = "visitor-456",
            DisplayName = "",
            Email = "",
            PhoneNumber = "",
            SourcePage = "/support"
        };

        var user = CreateCustomerUser(
            customerId: "customer-1",
            fullName: "Tran Thi B",
            email: "tran@example.com",
            phoneNumber: "0912345678");

        var conversation = await service.BootstrapConversationAsync(request, user);

        Assert.Equal("Tran Thi B", conversation.GuestProfile.DisplayName);
        Assert.Equal("tran@example.com", conversation.GuestProfile.Email);
        Assert.Equal("0912345678", conversation.GuestProfile.PhoneNumber);
    }

    private static SupportChatService CreateService()
    {
        return new SupportChatService(new InMemoryRepository<ChatConversation>(), new InMemoryRepository<ChatMessage>());
    }

    private static ClaimsPrincipal CreateGuestUser()
    {
        return new(new ClaimsIdentity());
    }

    private static ClaimsPrincipal CreateCustomerUser(string customerId, string fullName, string email, string phoneNumber)
    {
        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, customerId),
            new Claim(ClaimTypes.Name, email),
            new Claim(ClaimTypes.Role, "Customer"),
            new Claim("FullName", fullName),
            new Claim("PhoneNumber", phoneNumber)
        ], "TestAuth");

        return new ClaimsPrincipal(identity);
    }

    private sealed class InMemoryRepository<T> : IRepository<T> where T : class
    {
        private readonly List<T> _items = [];

        public Task<IEnumerable<T>> GetAllAsync()
        {
            return Task.FromResult<IEnumerable<T>>(_items.ToList());
        }

        public Task<T> GetByIdAsync(string id)
        {
            var item = _items.FirstOrDefault(entity => string.Equals(GetId(entity), id, StringComparison.Ordinal));
            return Task.FromResult(item)!;
        }

        public Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
        {
            var compiled = predicate.Compile();
            return Task.FromResult<IEnumerable<T>>(_items.Where(compiled).ToList());
        }

        public Task AddAsync(T entity)
        {
            EnsureId(entity);
            _items.Add(entity);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(string id, T entity)
        {
            var index = _items.FindIndex(item => string.Equals(GetId(item), id, StringComparison.Ordinal));
            if (index >= 0)
            {
                _items[index] = entity;
            }

            return Task.CompletedTask;
        }

        public Task DeleteAsync(string id)
        {
            var index = _items.FindIndex(item => string.Equals(GetId(item), id, StringComparison.Ordinal));
            if (index >= 0)
            {
                _items.RemoveAt(index);
            }

            return Task.CompletedTask;
        }

        public Task<PaginatedResult<T>> GetPagedAsync(int pageIndex, int pageSize, Expression<Func<T, bool>>? filter = null)
        {
            var query = _items.AsEnumerable();
            if (filter != null)
            {
                query = query.Where(filter.Compile());
            }

            var items = query.Skip(pageIndex * pageSize).Take(pageSize).ToList();
            return Task.FromResult(new PaginatedResult<T>(items, query.Count(), pageIndex, pageSize));
        }

        private static string? GetId(T entity)
        {
            return typeof(T).GetProperty("Id")?.GetValue(entity) as string;
        }

        private static void EnsureId(T entity)
        {
            var property = typeof(T).GetProperty("Id");
            if (property == null || property.PropertyType != typeof(string))
            {
                return;
            }

            var currentValue = property.GetValue(entity) as string;
            if (!string.IsNullOrWhiteSpace(currentValue))
            {
                return;
            }

            property.SetValue(entity, Guid.NewGuid().ToString("N"));
        }
    }
}

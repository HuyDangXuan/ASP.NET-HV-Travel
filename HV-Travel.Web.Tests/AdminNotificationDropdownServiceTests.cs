using System.Collections;
using System.Reflection;
using HVTravel.Domain.Entities;
using HVTravel.Domain.Interfaces;
using HVTravel.Domain.Models;
using HVTravel.Web.Services;
using Xunit.Sdk;

namespace HV_Travel.Web.Tests;

public class AdminNotificationDropdownServiceTests
{
    [Fact]
    public async Task GetDropdownAsync_FiltersToAdminGlobalNotifications_OrdersNewestFirst_AndCountsUnread()
    {
        var service = CreateService(
        [
            CreateNotification("old-all", "ALL", "System", false, DateTime.UtcNow.AddHours(-4)),
            CreateNotification("user-only", "admin-123", "Order", false, DateTime.UtcNow.AddMinutes(-10)),
            CreateNotification("new-empty", string.Empty, "Lead", false, DateTime.UtcNow.AddMinutes(-5)),
            CreateNotification("middle-null", null, "Review", true, DateTime.UtcNow.AddHours(-1))
        ]);

        var result = await InvokeGetDropdownAsync(service);
        var unreadCount = GetProperty<int>(result, "UnreadCount");
        var items = GetItems(result);

        Assert.Equal(2, unreadCount);
        Assert.Equal(3, items.Count);
        Assert.Equal("new-empty", GetProperty<string>(items[0], "Id"));
        Assert.Equal("middle-null", GetProperty<string>(items[1], "Id"));
        Assert.Equal("old-all", GetProperty<string>(items[2], "Id"));
    }

    [Theory]
    [InlineData("Order", "payments", "green")]
    [InlineData("Lead", "support_agent", "sky")]
    [InlineData("Review", "star", "amber")]
    [InlineData("System", "info", "blue")]
    [InlineData("Unknown", "notifications", "slate")]
    public async Task GetDropdownAsync_MapsNotificationTypeToIconAndAccent(string type, string expectedIcon, string expectedAccentToken)
    {
        var service = CreateService([CreateNotification("id", "ALL", type, false, DateTime.UtcNow)]);

        var result = await InvokeGetDropdownAsync(service);
        var item = Assert.Single(GetItems(result));

        Assert.Equal(expectedIcon, GetProperty<string>(item, "IconName"));
        Assert.Contains(expectedAccentToken, GetProperty<string>(item, "IconContainerClass"), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetDropdownAsync_ReturnsEmptyModel_WhenNoAdminNotificationsExist()
    {
        var service = CreateService([CreateNotification("user-only", "staff-1", "System", false, DateTime.UtcNow)]);

        var result = await InvokeGetDropdownAsync(service);
        var items = GetItems(result);

        Assert.Equal(0, GetProperty<int>(result, "UnreadCount"));
        Assert.Empty(items);
    }

    private static object CreateService(IEnumerable<Notification> notifications)
    {
        var serviceType = GetWebType("HVTravel.Web.Services.AdminNotificationDropdownService");
        var repository = new FakeNotificationRepository(notifications);

        return Activator.CreateInstance(serviceType, repository)
            ?? throw new XunitException("Could not create AdminNotificationDropdownService.");
    }

    private static async Task<object> InvokeGetDropdownAsync(object service)
    {
        var method = service.GetType().GetMethod("GetDropdownAsync", BindingFlags.Instance | BindingFlags.Public)
            ?? throw new XunitException("Expected AdminNotificationDropdownService.GetDropdownAsync to exist.");

        var task = method.Invoke(service, Array.Empty<object>()) as Task
            ?? throw new XunitException("Expected GetDropdownAsync to return a Task.");

        await task;

        var resultProperty = task.GetType().GetProperty("Result", BindingFlags.Instance | BindingFlags.Public)
            ?? throw new XunitException("Expected GetDropdownAsync to return a result.");

        return resultProperty.GetValue(task)
            ?? throw new XunitException("Expected GetDropdownAsync result to be non-null.");
    }

    private static List<object> GetItems(object model)
    {
        var value = GetProperty<object>(model, "Items");
        var items = value as IEnumerable
            ?? throw new XunitException("Expected dropdown model Items to be enumerable.");

        return items.Cast<object>().ToList();
    }

    private static T GetProperty<T>(object instance, string propertyName)
    {
        var property = instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public)
            ?? throw new XunitException($"Expected property '{propertyName}' on {instance.GetType().FullName}.");

        var value = property.GetValue(instance);
        if (value is T typed)
        {
            return typed;
        }

        throw new XunitException($"Property '{propertyName}' on {instance.GetType().FullName} did not return {typeof(T).FullName}.");
    }

    private static Type GetWebType(string fullName)
    {
        return typeof(BookingDisplayTextHelper).Assembly.GetType(fullName)
            ?? throw new XunitException($"Expected type '{fullName}' in HV-Travel.Web.");
    }

    private static Notification CreateNotification(string id, string? recipientId, string type, bool isRead, DateTime createdAt)
    {
        return new Notification
        {
            Id = id,
            RecipientId = recipientId!,
            Type = type,
            Title = $"{type} title",
            Message = $"{type} message",
            Link = "/Admin/Anything",
            IsRead = isRead,
            CreatedAt = createdAt
        };
    }

    private sealed class FakeNotificationRepository(IEnumerable<Notification> notifications) : IRepository<Notification>
    {
        private readonly List<Notification> _notifications = notifications.ToList();

        public Task<IEnumerable<Notification>> GetAllAsync() => Task.FromResult<IEnumerable<Notification>>(_notifications);

        public Task<Notification> GetByIdAsync(string id)
        {
            return Task.FromResult(_notifications.FirstOrDefault(notification => notification.Id == id)!);
        }

        public Task<IEnumerable<Notification>> FindAsync(System.Linq.Expressions.Expression<Func<Notification, bool>> predicate)
        {
            var compiled = predicate.Compile();
            return Task.FromResult<IEnumerable<Notification>>(_notifications.Where(compiled).ToList());
        }

        public Task AddAsync(Notification entity) => throw new NotSupportedException();

        public Task UpdateAsync(string id, Notification entity) => throw new NotSupportedException();

        public Task DeleteAsync(string id) => throw new NotSupportedException();

        public Task<PaginatedResult<Notification>> GetPagedAsync(
            int pageIndex,
            int pageSize,
            System.Linq.Expressions.Expression<Func<Notification, bool>>? filter = null)
        {
            throw new NotSupportedException();
        }
    }
}

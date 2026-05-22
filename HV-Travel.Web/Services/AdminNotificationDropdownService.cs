using System.Globalization;
using HVTravel.Domain.Entities;
using HVTravel.Domain.Interfaces;
using HVTravel.Web.Models;

namespace HVTravel.Web.Services;

public class AdminNotificationDropdownService
{
    private static readonly CultureInfo VietnameseCulture = CultureInfo.GetCultureInfo("vi-VN");
    private readonly IRepository<Notification> _notificationRepository;

    public AdminNotificationDropdownService(IRepository<Notification> notificationRepository)
    {
        _notificationRepository = notificationRepository;
    }

    public async Task<AdminNotificationDropdownViewModel> GetDropdownAsync()
    {
        var notifications = (await _notificationRepository.GetAllAsync())
            .Where(IsAdminGlobalNotification)
            .OrderByDescending(notification => notification.CreatedAt)
            .ToList();

        return new AdminNotificationDropdownViewModel
        {
            UnreadCount = notifications.Count(notification => !notification.IsRead),
            Items = notifications
                .Take(6)
                .Select(MapItem)
                .ToList()
        };
    }

    private static bool IsAdminGlobalNotification(Notification notification)
    {
        return string.IsNullOrWhiteSpace(notification.RecipientId)
            || string.Equals(notification.RecipientId.Trim(), "ALL", StringComparison.OrdinalIgnoreCase);
    }

    private static AdminNotificationItemViewModel MapItem(Notification notification)
    {
        var title = string.IsNullOrWhiteSpace(notification.Title) ? "Thông báo hệ thống" : notification.Title.Trim();
        var message = string.IsNullOrWhiteSpace(notification.Message) ? "Nội dung đang được cập nhật." : notification.Message.Trim();
        var accent = ResolveAccent(notification.Type);

        return new AdminNotificationItemViewModel
        {
            Id = notification.Id ?? string.Empty,
            Title = title,
            Message = message,
            Type = notification.Type ?? string.Empty,
            Link = notification.Link ?? string.Empty,
            IsRead = notification.IsRead,
            CreatedAt = notification.CreatedAt,
            RelativeTimeText = FormatRelativeTime(notification.CreatedAt),
            IconName = accent.IconName,
            IconContainerClass = accent.IconContainerClass,
            ItemContainerClass = notification.IsRead
                ? "bg-transparent opacity-80"
                : "bg-primary/5 dark:bg-primary/10",
            ReadStateBarClass = notification.IsRead
                ? "bg-slate-200 dark:bg-slate-700 opacity-0"
                : "bg-primary opacity-100",
            TitleClass = notification.IsRead
                ? "text-slate-700 dark:text-slate-200"
                : "text-slate-900 dark:text-white",
            MessageClass = notification.IsRead
                ? "text-slate-400 dark:text-slate-500"
                : "text-slate-500 dark:text-slate-400"
        };
    }

    private static (string IconName, string IconContainerClass) ResolveAccent(string? type)
    {
        return (type ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "order" => (
                "payments",
                "bg-green-100 text-green-600 dark:bg-green-900/30 dark:text-green-300"),
            "lead" => (
                "support_agent",
                "bg-sky-100 text-sky-600 dark:bg-sky-900/30 dark:text-sky-300"),
            "review" => (
                "star",
                "bg-amber-100 text-amber-600 dark:bg-amber-900/30 dark:text-amber-300"),
            "system" => (
                "info",
                "bg-blue-100 text-blue-600 dark:bg-blue-900/30 dark:text-blue-300"),
            _ => (
                "notifications",
                "bg-slate-100 text-slate-600 dark:bg-slate-800 dark:text-slate-300")
        };
    }

    private static string FormatRelativeTime(DateTime createdAt)
    {
        var utcCreatedAt = createdAt.Kind == DateTimeKind.Utc
            ? createdAt
            : DateTime.SpecifyKind(createdAt, DateTimeKind.Utc);

        var elapsed = DateTime.UtcNow - utcCreatedAt;
        if (elapsed < TimeSpan.FromMinutes(1))
        {
            return "Vừa xong";
        }

        if (elapsed < TimeSpan.FromHours(1))
        {
            return $"{Math.Max(1, (int)elapsed.TotalMinutes)} phút trước";
        }

        if (elapsed < TimeSpan.FromDays(1))
        {
            return $"{Math.Max(1, (int)elapsed.TotalHours)} giờ trước";
        }

        if (elapsed < TimeSpan.FromDays(2))
        {
            return "Hôm qua";
        }

        if (elapsed < TimeSpan.FromDays(7))
        {
            return $"{Math.Max(1, (int)elapsed.TotalDays)} ngày trước";
        }

        return utcCreatedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm", VietnameseCulture);
    }
}

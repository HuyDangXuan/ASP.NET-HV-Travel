namespace HVTravel.Web.Models;

public class AdminNotificationDropdownViewModel
{
    public IReadOnlyList<AdminNotificationItemViewModel> Items { get; set; } = Array.Empty<AdminNotificationItemViewModel>();

    public int UnreadCount { get; set; }

    public bool HasUnread => UnreadCount > 0;
}

public class AdminNotificationItemViewModel
{
    public string Id { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public string Link { get; set; } = string.Empty;

    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; }

    public string RelativeTimeText { get; set; } = string.Empty;

    public string IconName { get; set; } = "notifications";

    public string IconContainerClass { get; set; } = "bg-slate-100 text-slate-600 dark:bg-slate-800 dark:text-slate-300";

    public string ItemContainerClass { get; set; } = string.Empty;

    public string ReadStateBarClass { get; set; } = string.Empty;

    public string TitleClass { get; set; } = string.Empty;

    public string MessageClass { get; set; } = string.Empty;
}

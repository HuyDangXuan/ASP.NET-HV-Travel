using HVTravel.Domain.Entities;

namespace HVTravel.Web.Models;

public class ContentManagementViewModel
{
    public string ActiveTab { get; set; } = "site";

    public IReadOnlyList<ContentTabOption> Tabs { get; set; } = Array.Empty<ContentTabOption>();

    public SiteSettings SiteSettings { get; set; } = new();

    public List<ContentSection> Sections { get; set; } = new();

    public Dictionary<string, List<string>> Inventory { get; set; } = new();
}

public class ContentTabOption
{
    public string Key { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;
}

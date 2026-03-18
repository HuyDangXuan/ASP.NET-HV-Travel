using HVTravel.Domain.Entities;

namespace HVTravel.Web.Models;

public class SiteSettingsFormModel
{
    public SiteSettings SiteSettings { get; set; } = new();
}

public class ContentSectionsFormModel
{
    public string ActiveTab { get; set; } = string.Empty;

    public string SelectedSubtab { get; set; } = string.Empty;

    public List<ContentSection> Sections { get; set; } = new();
}

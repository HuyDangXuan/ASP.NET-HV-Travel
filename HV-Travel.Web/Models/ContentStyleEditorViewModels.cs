using HVTravel.Domain.Entities;

namespace HVTravel.Web.Models;

public class ContentTextStyleEditorViewModel
{
    public string Prefix { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public bool IncludeJustify { get; set; } = true;

    public ContentTextStyle Style { get; set; } = ContentPresentationDefaults.CreateTextStyle();
}

namespace HVTravel.Web.Models;

public class BreadcrumbViewModel
{
    public IReadOnlyList<BreadcrumbItemViewModel> Items { get; set; } = Array.Empty<BreadcrumbItemViewModel>();
}

public class BreadcrumbItemViewModel
{
    public string Label { get; set; } = string.Empty;

    public string? Url { get; set; }

    public bool IsCurrent { get; set; }
}

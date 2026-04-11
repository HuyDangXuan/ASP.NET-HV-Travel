using HVTravel.Domain.Entities;

namespace HVTravel.Web.Models;

public class PromotionsIndexViewModel
{
    public IReadOnlyList<Promotion> FlashSales { get; set; } = Array.Empty<Promotion>();
    public IReadOnlyList<Promotion> VoucherCampaigns { get; set; } = Array.Empty<Promotion>();
    public IReadOnlyList<Promotion> SeasonalDeals { get; set; } = Array.Empty<Promotion>();
    public string CustomerSegment { get; set; } = string.Empty;
}

public class DestinationHubViewModel
{
    public IReadOnlyList<DestinationRegionViewModel> Regions { get; set; } = Array.Empty<DestinationRegionViewModel>();
    public IReadOnlyList<DestinationCollectionViewModel> Collections { get; set; } = Array.Empty<DestinationCollectionViewModel>();
    public IReadOnlyList<Tour> TrendingTours { get; set; } = Array.Empty<Tour>();
}

public class DestinationRegionViewModel
{
    public string Region { get; set; } = string.Empty;
    public IReadOnlyList<Tour> Tours { get; set; } = Array.Empty<Tour>();
}

public class DestinationCollectionViewModel
{
    public string Key { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public IReadOnlyList<Tour> Tours { get; set; } = Array.Empty<Tour>();
}

public class ContentHubIndexViewModel
{
    public TravelArticle? FeaturedArticle { get; set; }
    public IReadOnlyList<TravelArticle> LatestArticles { get; set; } = Array.Empty<TravelArticle>();
    public IReadOnlyList<string> Categories { get; set; } = Array.Empty<string>();
}

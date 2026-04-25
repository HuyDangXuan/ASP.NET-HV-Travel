using System.Linq.Expressions;
using System.Reflection;
using HVTravel.Application.Services;
using HVTravel.Domain.Entities;
using HVTravel.Domain.Interfaces;
using HVTravel.Domain.Models;
using Xunit;

namespace HVTravel.Web.Tests;

public class RouteRecommendationPhase5Tests
{
    [Fact]
    public void RouteRecommendationService_Compact_PrefersMoreEfficientTour()
    {
        var compactTour = BuildTour(
            "tour-compact",
            "Compact Escape",
            days: 2,
            adultPrice: 1500000m,
            rating: 4.4,
            city: "Da Nang",
            region: "Mien Trung",
            stops:
            [
                Stop("c1", 1, 1, "Pickup", "meeting", 16.0471, 108.2062, 10, 0),
                Stop("c2", 1, 2, "Museum", "museum", 16.0472, 108.2068, 60, 6.8),
                Stop("c3", 1, 3, "Old town", "landmark", 16.0480, 108.2073, 70, 7.2)
            ]);

        var sprawlingTour = BuildTour(
            "tour-sprawling",
            "Sprawling Route",
            days: 2,
            adultPrice: 1450000m,
            rating: 4.5,
            city: "Da Nang",
            region: "Mien Trung",
            stops:
            [
                Stop("s1", 1, 1, "Pickup", "meeting", 16.0471, 108.2062, 10, 0),
                Stop("s2", 1, 2, "Far museum", "museum", 16.1200, 108.3200, 60, 6.8),
                Stop("s3", 1, 3, "Far old town", "landmark", 16.1800, 108.3900, 70, 7.2)
            ]);

        var rankedIds = RecommendAndGetTourIds([sprawlingTour, compactTour], routeStyle: "compact");

        Assert.Equal(["tour-compact", "tour-sprawling"], rankedIds);
    }

    [Fact]
    public void RouteRecommendationService_Highlights_PrefersHigherAttractionTour()
    {
        var highHighlightsTour = BuildTour(
            "tour-highlights",
            "Highlight Hunter",
            days: 3,
            adultPrice: 2400000m,
            rating: 4.6,
            city: "Hue",
            region: "Mien Trung",
            stops:
            [
                Stop("h1", 1, 1, "Meeting point", "meeting", 16.4637, 107.5909, 10, 0),
                Stop("h2", 1, 2, "Royal citadel", "landmark", 16.4700, 107.5770, 85, 9.6),
                Stop("h3", 1, 3, "Imperial museum", "museum", 16.4720, 107.5840, 65, 9.2)
            ]);

        var balancedTour = BuildTour(
            "tour-balanced",
            "Balanced Heritage",
            days: 3,
            adultPrice: 2200000m,
            rating: 4.7,
            city: "Hue",
            region: "Mien Trung",
            stops:
            [
                Stop("b1", 1, 1, "Meeting point", "meeting", 16.4637, 107.5909, 10, 0),
                Stop("b2", 1, 2, "Garden house", "landmark", 16.4610, 107.5850, 55, 7.1),
                Stop("b3", 1, 3, "Riverside walk", "park", 16.4590, 107.5820, 40, 6.8)
            ]);

        var rankedIds = RecommendAndGetTourIds([balancedTour, highHighlightsTour], routeStyle: "highlights");

        Assert.Equal(["tour-highlights", "tour-balanced"], rankedIds);
    }

    [Fact]
    public void RouteRecommendationService_RanksToursWithoutRouting_AndBoostsRelatedSimilarity()
    {
        var currentTour = BuildTour(
            "tour-current",
            "Current Tour",
            days: 3,
            adultPrice: 2100000m,
            rating: 4.7,
            city: "Da Lat",
            region: "Tay Nguyen",
            stops:
            [
                Stop("x1", 1, 1, "Pickup", "meeting", 11.9404, 108.4583, 10, 0),
                Stop("x2", 1, 2, "Valley", "park", 11.9380, 108.4510, 70, 8.5),
                Stop("x3", 1, 3, "Lake", "lake", 11.9350, 108.4480, 55, 8.0)
            ]);

        var relatedNoRouting = BuildTour(
            "tour-no-routing",
            "Legacy Scenic Stay",
            days: 3,
            adultPrice: 1800000m,
            rating: 4.8,
            city: "Da Lat",
            region: "Tay Nguyen",
            stops: []);

        var otherRegion = BuildTour(
            "tour-other-region",
            "Far Away Break",
            days: 5,
            adultPrice: 1700000m,
            rating: 4.9,
            city: "Ha Noi",
            region: "Mien Bac",
            stops:
            [
                Stop("o1", 1, 1, "Pickup", "meeting", 21.0285, 105.8542, 10, 0),
                Stop("o2", 1, 2, "Museum", "museum", 21.0300, 105.8580, 55, 7.8),
                Stop("o3", 1, 3, "Lake", "lake", 21.0320, 105.8610, 45, 7.4)
            ]);

        var rankedIds = RecommendAndGetTourIds(
            [otherRegion, relatedNoRouting],
            routeStyle: "balanced",
            currentTourId: currentTour.Id,
            currentCity: currentTour.Destination.City,
            currentRegion: currentTour.Destination.Region);

        Assert.Equal(["tour-no-routing", "tour-other-region"], rankedIds);
    }

    [Fact]
    public async Task TourSearchService_ReranksBeforePagination_WhenRecommendationEnabled()
    {
        var repo = new Phase5SearchRepository(
        [
            BuildTour(
                "tour-wide",
                "Wide Explorer",
                days: 4,
                adultPrice: 1300000m,
                rating: 4.4,
                city: "Da Nang",
                region: "Mien Trung",
                stops:
                [
                    Stop("w1", 1, 1, "Pickup", "meeting", 16.0471, 108.2062, 10, 0),
                    Stop("w2", 1, 2, "Far museum", "museum", 16.1800, 108.3900, 50, 7.2),
                    Stop("w3", 1, 3, "Far market", "market", 16.2200, 108.4300, 45, 7.1)
                ]),
            BuildTour(
                "tour-mid",
                "Mid Explorer",
                days: 3,
                adultPrice: 1200000m,
                rating: 4.5,
                city: "Da Nang",
                region: "Mien Trung",
                stops:
                [
                    Stop("m1", 1, 1, "Pickup", "meeting", 16.0471, 108.2062, 10, 0),
                    Stop("m2", 1, 2, "Museum", "museum", 16.0800, 108.2500, 50, 7.0),
                    Stop("m3", 1, 3, "Market", "market", 16.0900, 108.2600, 45, 6.9)
                ]),
            BuildTour(
                "tour-tight",
                "Tight Explorer",
                days: 2,
                adultPrice: 1100000m,
                rating: 4.3,
                city: "Da Nang",
                region: "Mien Trung",
                stops:
                [
                    Stop("t1", 1, 1, "Pickup", "meeting", 16.0471, 108.2062, 10, 0),
                    Stop("t2", 1, 2, "Museum", "museum", 16.0475, 108.2070, 50, 7.0),
                    Stop("t3", 1, 3, "Market", "market", 16.0478, 108.2076, 45, 6.9)
                ])
        ]);

        var service = new TourSearchService(repo);
        var request = new TourSearchRequest
        {
            Page = 2,
            PageSize = 1,
            Sort = "recommended",
            Travellers = 2
        };

        SetProperty(request, "UseRecommendationRanking", true);
        SetProperty(request, "RouteStyle", "compact");

        var result = await service.SearchAsync(request);

        Assert.Equal(3, result.TotalItems);
        Assert.Equal(3, result.TotalPages);
        Assert.Equal(2, result.CurrentPage);
        Assert.Single(result.Items);
        Assert.Equal("tour-mid", result.Items[0].Id);
    }

    [Fact]
    public async Task TourSearchService_DefaultsRouteStyleToBalanced_WhenMissing()
    {
        var repo = new Phase5SearchRepository(
        [
            BuildTour(
                "tour-a",
                "Tour A",
                days: 2,
                adultPrice: 1200000m,
                rating: 4.4,
                city: "Hue",
                region: "Mien Trung",
                stops:
                [
                    Stop("a1", 1, 1, "Pickup", "meeting", 16.4637, 107.5909, 10, 0),
                    Stop("a2", 1, 2, "Museum", "museum", 16.4700, 107.5770, 60, 8.0)
                ]),
            BuildTour(
                "tour-b",
                "Tour B",
                days: 3,
                adultPrice: 1100000m,
                rating: 4.6,
                city: "Hue",
                region: "Mien Trung",
                stops:
                [
                    Stop("b1", 1, 1, "Pickup", "meeting", 16.4637, 107.5909, 10, 0),
                    Stop("b2", 1, 2, "Market", "market", 16.4610, 107.5850, 55, 7.1)
                ])
        ]);

        var service = new TourSearchService(repo);
        var defaultRequest = new TourSearchRequest { Page = 1, PageSize = 10, Sort = "recommended" };
        var explicitBalancedRequest = new TourSearchRequest { Page = 1, PageSize = 10, Sort = "recommended" };
        SetProperty(defaultRequest, "UseRecommendationRanking", true);
        SetProperty(explicitBalancedRequest, "UseRecommendationRanking", true);
        SetProperty(explicitBalancedRequest, "RouteStyle", "balanced");

        var defaultResult = await service.SearchAsync(defaultRequest);
        var balancedResult = await service.SearchAsync(explicitBalancedRequest);

        Assert.Equal(
            balancedResult.Items.Select(item => item.Id).ToArray(),
            defaultResult.Items.Select(item => item.Id).ToArray());
    }

    [Fact]
    public void PublicToursController_AndViews_ExposeRouteStyleRecommendationFlow()
    {
        var controllerSource = TestPaths.ReadRepoFile("HV-Travel.Web", "Controllers", "PublicToursController.cs");
        var filterSource = TestPaths.ReadRepoFile("HV-Travel.Web", "Views", "PublicTours", "_PublicToursFilter.cshtml");
        var indexSource = TestPaths.ReadRepoFile("HV-Travel.Web", "Views", "PublicTours", "Index.cshtml");
        var detailsSource = TestPaths.ReadRepoFile("HV-Travel.Web", "Views", "PublicTours", "Details.cshtml");
        var tourCardSource = TestPaths.ReadRepoFile("HV-Travel.Web", "Views", "PublicTours", "_TourCard.cshtml");
        var carouselCardSource = TestPaths.ReadRepoFile("HV-Travel.Web", "Views", "Shared", "_CarouselTourCard.cshtml");

        Assert.Contains("routeStyle", controllerSource, StringComparison.Ordinal);
        Assert.Contains("recommended", controllerSource, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ITourSearchService", controllerSource, StringComparison.Ordinal);
        Assert.Contains("IRouteRecommendationService", controllerSource, StringComparison.Ordinal);
        Assert.Contains("GetDetailIdentifier(tour) }, Request.Scheme", controllerSource, StringComparison.Ordinal);

        Assert.Contains("name=\"routeStyle\"", filterSource, StringComparison.Ordinal);
        Assert.Contains("value=\"recommended\"", filterSource, StringComparison.Ordinal);
        Assert.Contains("sortRecommendedLabel", filterSource, StringComparison.Ordinal);

        Assert.Contains("asp-route-routeStyle", indexSource, StringComparison.Ordinal);
        Assert.Contains("CurrentRouteStyle", indexSource, StringComparison.Ordinal);
        Assert.Contains("routeStyleSummaryFormat", indexSource, StringComparison.Ordinal);

        Assert.Contains("CurrentRouteStyle", detailsSource, StringComparison.Ordinal);
        Assert.Contains("routeStyle", detailsSource, StringComparison.Ordinal);

        Assert.Contains("asp-route-routeStyle", tourCardSource, StringComparison.Ordinal);
        Assert.Contains("asp-route-routeStyle", carouselCardSource, StringComparison.Ordinal);
    }

    [Fact]
    public void ApplicationContracts_RegisterRouteRecommendationService_AndPublicContentDefaultsContainRouteStyleCopy()
    {
        var interfaceSource = TestPaths.ReadRepoFile("HV-Travel.Application", "Interfaces", "ICommerceServices.cs");
        var registrationSource = TestPaths.ReadRepoFile("HV-Travel.Application", "DependencyInjection.cs");
        var contentDefaultsSource = TestPaths.ReadRepoFile("HV-Travel.Web", "Services", "PublicContentDefaults.cs");

        Assert.Contains("IRouteRecommendationService", interfaceSource, StringComparison.Ordinal);
        Assert.Contains("RouteRecommendationService", registrationSource, StringComparison.Ordinal);
        Assert.Contains("routeStyleLabel", contentDefaultsSource, StringComparison.Ordinal);
        Assert.Contains("routeStyleCompactLabel", contentDefaultsSource, StringComparison.Ordinal);
        Assert.Contains("routeStyleBalancedLabel", contentDefaultsSource, StringComparison.Ordinal);
        Assert.Contains("routeStyleHighlightsLabel", contentDefaultsSource, StringComparison.Ordinal);
        Assert.Contains("sortRecommendedLabel", contentDefaultsSource, StringComparison.Ordinal);
        Assert.Contains("routeStyleSummaryFormat", contentDefaultsSource, StringComparison.Ordinal);
        Assert.Contains("Phù hợp nhất", contentDefaultsSource, StringComparison.Ordinal);
        Assert.Contains("Gọn, ít di chuyển", contentDefaultsSource, StringComparison.Ordinal);
        Assert.Contains("Khám phá", contentDefaultsSource, StringComparison.Ordinal);
    }

    private static IReadOnlyList<string> RecommendAndGetTourIds(
        IEnumerable<Tour> tours,
        string routeStyle,
        int travellers = 0,
        string? currentTourId = null,
        string? currentCity = null,
        string? currentRegion = null)
    {
        var serviceType = typeof(PricingService).Assembly.GetType("HVTravel.Application.Services.RouteRecommendationService");
        Assert.NotNull(serviceType);

        var service = Activator.CreateInstance(serviceType!);
        Assert.NotNull(service);

        var requestType = serviceType!.Assembly.GetType("HVTravel.Application.Models.RouteRecommendationRequest");
        Assert.NotNull(requestType);

        var request = Activator.CreateInstance(requestType!);
        Assert.NotNull(request);
        SetProperty(request!, "RouteStyle", routeStyle);
        SetProperty(request!, "Travellers", travellers);
        SetProperty(request!, "CurrentTourId", currentTourId);
        SetProperty(request!, "CurrentCity", currentCity);
        SetProperty(request!, "CurrentRegion", currentRegion);

        var recommendMethod = serviceType.GetMethod("Recommend", BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(recommendMethod);

        var rawResult = recommendMethod!.Invoke(service, [tours.ToList(), request]);
        Assert.NotNull(rawResult);

        if (rawResult is IEnumerable<Tour> directItems)
        {
            return directItems.Select(item => item.Id).ToList();
        }

        var items = GetProperty<IEnumerable<Tour>>(rawResult!, "Items");
        return items.Select(item => item.Id).ToList();
    }

    private static T GetProperty<T>(object target, string propertyName)
    {
        var property = target.GetType().GetProperty(propertyName);
        Assert.NotNull(property);
        return (T)property!.GetValue(target)!;
    }

    private static void SetProperty(object target, string propertyName, object? value)
    {
        var property = target.GetType().GetProperty(propertyName);
        Assert.NotNull(property);
        property!.SetValue(target, value);
    }

    private static Tour BuildTour(
        string id,
        string name,
        int days,
        decimal adultPrice,
        double rating,
        string city,
        string region,
        params TourRouteStop[] stops)
    {
        return new Tour
        {
            Id = id,
            Slug = id,
            Code = id.ToUpperInvariant(),
            Name = name,
            Description = $"{name} description",
            ShortDescription = $"{name} short description",
            Destination = new Destination
            {
                City = city,
                Country = "Viet Nam",
                Region = region
            },
            Price = new TourPrice
            {
                Adult = adultPrice,
                Child = Math.Round(adultPrice * 0.75m, 0, MidpointRounding.AwayFromZero),
                Infant = Math.Round(adultPrice * 0.25m, 0, MidpointRounding.AwayFromZero),
                Currency = "VND"
            },
            Duration = new TourDuration
            {
                Days = days,
                Nights = Math.Max(1, days - 1),
                Text = $"{days} ngay"
            },
            Rating = rating,
            ReviewCount = 20,
            MaxParticipants = 20,
            CurrentParticipants = 4,
            ConfirmationType = "Instant",
            Status = "Active",
            Schedule = Enumerable.Range(1, Math.Max(days, 1))
                .Select(day => new ScheduleItem
                {
                    Day = day,
                    Title = $"Day {day}",
                    Description = $"Route day {day}"
                })
                .ToList(),
            Departures =
            [
                new TourDeparture
                {
                    Id = $"{id}-dep-1",
                    StartDate = new DateTime(2026, 6, 1),
                    AdultPrice = adultPrice,
                    ChildPrice = Math.Round(adultPrice * 0.75m, 0, MidpointRounding.AwayFromZero),
                    InfantPrice = Math.Round(adultPrice * 0.25m, 0, MidpointRounding.AwayFromZero),
                    Capacity = 20,
                    BookedCount = 4,
                    ConfirmationType = "Instant",
                    Status = "Scheduled",
                    CutoffHours = 24
                }
            ],
            Routing = stops.Length == 0
                ? null
                : new TourRouting
                {
                    SchemaVersion = 1,
                    Stops = stops.ToList()
                }
        };
    }

    private static TourRouteStop Stop(string id, int day, int order, string name, string type, double? lat, double? lng, int visitMinutes, double attractionScore)
    {
        return new TourRouteStop
        {
            Id = id,
            Day = day,
            Order = order,
            Name = name,
            Type = type,
            VisitMinutes = visitMinutes,
            AttractionScore = attractionScore,
            Note = $"{name} note",
            Coordinates = new GeoPoint
            {
                Lat = lat,
                Lng = lng
            }
        };
    }

    private sealed class Phase5SearchRepository : ITourRepository
    {
        private readonly List<Tour> _tours;

        public Phase5SearchRepository(IEnumerable<Tour> tours)
        {
            _tours = tours.ToList();
        }

        public Task<IEnumerable<Tour>> GetAllAsync() => Task.FromResult<IEnumerable<Tour>>(_tours);

        public Task<Tour> GetByIdAsync(string id) => Task.FromResult(_tours.First(item => item.Id == id));

        public Task<IEnumerable<Tour>> FindAsync(Expression<Func<Tour, bool>> predicate)
        {
            var compiled = predicate.Compile();
            return Task.FromResult<IEnumerable<Tour>>(_tours.Where(compiled).ToList());
        }

        public Task AddAsync(Tour entity) => Task.CompletedTask;

        public Task UpdateAsync(string id, Tour entity) => Task.CompletedTask;

        public Task DeleteAsync(string id) => Task.CompletedTask;

        public Task<PaginatedResult<Tour>> GetPagedAsync(int pageIndex, int pageSize, Expression<Func<Tour, bool>>? filter = null)
        {
            var items = _tours.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToList();
            return Task.FromResult(new PaginatedResult<Tour>(items, _tours.Count, pageIndex, pageSize));
        }

        public Task<TourSearchResult> SearchAsync(TourSearchRequest request)
        {
            var items = _tours.ToList();
            var useRecommendationRanking = (bool?)(request.GetType().GetProperty("UseRecommendationRanking")?.GetValue(request)) == true;

            if (!useRecommendationRanking)
            {
                items = items
                    .OrderByDescending(item => item.Rating)
                    .Skip((Math.Max(request.Page, 1) - 1) * Math.Max(request.PageSize, 1))
                    .Take(Math.Max(request.PageSize, 1))
                    .ToList();
            }

            return Task.FromResult(new TourSearchResult
            {
                Items = items,
                TotalItems = _tours.Count,
                TotalPages = 1,
                CurrentPage = 1
            });
        }

        public Task<Tour?> GetBySlugAsync(string slug)
        {
            return Task.FromResult<Tour?>(_tours.FirstOrDefault(item => string.Equals(item.Slug, slug, StringComparison.Ordinal)));
        }

        public Task<bool> IncrementParticipantsAsync(string tourId, int count) => Task.FromResult(true);

        public Task<bool> ReserveDepartureAsync(string tourId, string departureId, int travellerCount) => Task.FromResult(true);
    }
}

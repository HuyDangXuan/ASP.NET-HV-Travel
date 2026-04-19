using System.Linq.Expressions;
using System.Reflection;
using HVTravel.Application.Services;
using HVTravel.Domain.Entities;
using HVTravel.Domain.Interfaces;
using HVTravel.Domain.Models;
using Xunit;

namespace HVTravel.Web.Tests;

public class TripPlannerPhase9Tests
{
    [Fact]
    public void TripPlannerService_BuildsDedupedPlan_WithSuggestionsMetricsAndWarnings()
    {
        var selectedRoutingTour = BuildTour(
            "tour-selected-route",
            "Da Lat Forest Route",
            days: 2,
            adultPrice: 1800000m,
            rating: 4.7,
            city: "Da Lat",
            region: "Tay Nguyen",
            stops:
            [
                Stop("s1", 1, 1, "Pickup", "meeting", 11.9404, 108.4583, 20, 0),
                Stop("s2", 1, 2, "Forest walk", "forest", 11.9380, 108.4510, 60, 8.6),
                Stop("s3", 2, 1, "Lake", "lake", 11.9200, 108.4300, 45, 8.1)
            ]);

        var selectedLegacyTour = BuildTour(
            "tour-selected-legacy",
            "Legacy Beach Stay",
            days: 2,
            adultPrice: 1600000m,
            rating: 4.3,
            city: "Phu Quoc",
            region: "Mien Nam",
            stops: []);

        var suggestedTour = BuildTour(
            "tour-suggested",
            "Da Lat Compact Garden",
            days: 1,
            adultPrice: 900000m,
            rating: 4.6,
            city: "Da Lat",
            region: "Tay Nguyen",
            stops:
            [
                Stop("g1", 1, 1, "Garden gate", "meeting", 11.9400, 108.4580, 15, 0),
                Stop("g2", 1, 2, "Garden trail", "park", 11.9390, 108.4550, 45, 7.8)
            ]);

        var result = BuildPlannerResult(
            selectedTours: [selectedRoutingTour, selectedRoutingTour, selectedLegacyTour],
            candidateTours: [selectedRoutingTour, suggestedTour],
            routeStyle: "compact",
            maxDays: 7);

        var selectedItems = GetEnumerable(result, "SelectedItems").ToList();
        var suggestedItems = GetEnumerable(result, "SuggestedItems").ToList();
        var days = GetEnumerable(result, "Days").ToList();
        var warnings = GetEnumerable(result, "Warnings").ToList();

        Assert.Equal(2, selectedItems.Count);
        Assert.Single(suggestedItems);
        Assert.Equal("tour-selected-route", GetProperty<string>(selectedItems[0], "TourId"));
        Assert.Equal("tour-selected-legacy", GetProperty<string>(selectedItems[1], "TourId"));
        Assert.Equal("tour-suggested", GetProperty<string>(suggestedItems[0], "TourId"));
        Assert.Equal(5, GetProperty<int>(result, "TotalDays"));
        Assert.Equal(4300000m, GetProperty<decimal>(result, "TotalStartingAdultPrice"));
        Assert.True(GetProperty<int>(result, "TotalVisitMinutes") > 0);
        Assert.True(GetProperty<int>(result, "TotalTravelMinutes") > 0);
        Assert.True(GetProperty<int>(result, "TotalJourneyMinutes") >= GetProperty<int>(result, "TotalVisitMinutes"));
        Assert.Equal(5, days.Count);
        Assert.Contains(warnings, warning => GetProperty<string>(warning, "Code").Contains("missing-routing", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void TripPlannerService_RespectsMaxDays_AndKeepsSelectedBeforeSuggested()
    {
        var selectedTour = BuildTour(
            "tour-selected",
            "Selected Two Day Tour",
            days: 2,
            adultPrice: 2000000m,
            rating: 4.5,
            city: "Hue",
            region: "Mien Trung",
            stops:
            [
                Stop("h1", 1, 1, "Pickup", "meeting", 16.4637, 107.5909, 10, 0),
                Stop("h2", 2, 1, "Citadel", "landmark", 16.4700, 107.5770, 70, 9.1)
            ]);

        var suggestedTooLarge = BuildTour(
            "tour-suggested-large",
            "Large Suggested Tour",
            days: 3,
            adultPrice: 1900000m,
            rating: 4.8,
            city: "Hue",
            region: "Mien Trung",
            stops:
            [
                Stop("l1", 1, 1, "Museum", "museum", 16.4720, 107.5840, 50, 8.7)
            ]);

        var result = BuildPlannerResult(
            selectedTours: [selectedTour],
            candidateTours: [suggestedTooLarge],
            routeStyle: "highlights",
            maxDays: 2);

        var selectedItems = GetEnumerable(result, "SelectedItems").ToList();
        var suggestedItems = GetEnumerable(result, "SuggestedItems").ToList();
        var warnings = GetEnumerable(result, "Warnings").ToList();

        Assert.Single(selectedItems);
        Assert.Empty(suggestedItems);
        Assert.Equal(2, GetProperty<int>(result, "TotalDays"));
        Assert.Contains(warnings, warning => GetProperty<string>(warning, "Code").Contains("max-days", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void TripPlanner_PublicControllerViewAndClientStorage_AreWired()
    {
        var interfaceSource = TestPaths.ReadRepoFile("HV-Travel.Application", "Interfaces", "ICommerceServices.cs");
        var registrationSource = TestPaths.ReadRepoFile("HV-Travel.Application", "DependencyInjection.cs");
        var controllerSource = TestPaths.ReadRepoFile("HV-Travel.Web", "Controllers", "TripPlannerController.cs");
        var viewSource = TestPaths.ReadRepoFile("HV-Travel.Web", "Views", "TripPlanner", "Index.cshtml");
        var cardSource = TestPaths.ReadRepoFile("HV-Travel.Web", "Views", "PublicTours", "_TourCard.cshtml");
        var detailsSource = TestPaths.ReadRepoFile("HV-Travel.Web", "Views", "PublicTours", "Details.cshtml");
        var clientSource = TestPaths.ReadRepoFile("HV-Travel.Web", "wwwroot", "js", "card-interactions.js");

        Assert.Contains("ITripPlannerService", interfaceSource, StringComparison.Ordinal);
        Assert.Contains("TripPlannerService", registrationSource, StringComparison.Ordinal);
        Assert.Contains("Preview", controllerSource, StringComparison.Ordinal);
        Assert.Contains("TripPlannerPreviewRequest", controllerSource, StringComparison.Ordinal);
        Assert.Contains("TripPlannerPreviewResponse", controllerSource, StringComparison.Ordinal);
        Assert.Contains("trip-planner-shell", viewSource, StringComparison.Ordinal);
        Assert.Contains("data-trip-planner-preview", viewSource, StringComparison.Ordinal);
        Assert.Contains("data-tour-planner-toggle", cardSource, StringComparison.Ordinal);
        Assert.Contains("data-tour-planner-toggle", detailsSource, StringComparison.Ordinal);
        Assert.Contains("hvtravel_trip_planner", clientSource, StringComparison.Ordinal);
        Assert.Contains("data-tour-planner-toggle", clientSource, StringComparison.Ordinal);
        Assert.DoesNotContain("coordinates", viewSource, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("latitude", viewSource, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("longitude", viewSource, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("attractionScore", viewSource, StringComparison.OrdinalIgnoreCase);
    }

    private static object BuildPlannerResult(
        IEnumerable<Tour> selectedTours,
        IEnumerable<Tour> candidateTours,
        string routeStyle,
        int? maxDays)
    {
        var serviceType = typeof(PricingService).Assembly.GetType("HVTravel.Application.Services.TripPlannerService");
        Assert.NotNull(serviceType);

        var requestType = typeof(PricingService).Assembly.GetType("HVTravel.Application.Models.TripPlannerRequest");
        Assert.NotNull(requestType);

        var routeInsightService = new RouteInsightService(new RouteTravelEstimator());
        var recommendationService = new RouteRecommendationService(routeInsightService);
        var service = Activator.CreateInstance(serviceType!, routeInsightService, recommendationService);
        Assert.NotNull(service);

        var request = Activator.CreateInstance(requestType!);
        Assert.NotNull(request);
        SetProperty(request!, "SelectedTours", selectedTours.ToList());
        SetProperty(request!, "CandidateTours", candidateTours.ToList());
        SetProperty(request!, "RouteStyle", routeStyle);
        SetProperty(request!, "Travellers", 2);
        SetProperty(request!, "MaxDays", maxDays);

        var buildMethod = serviceType!.GetMethod("Build", BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(buildMethod);

        var result = buildMethod!.Invoke(service, [request]);
        Assert.NotNull(result);
        return result!;
    }

    private static IEnumerable<object> GetEnumerable(object target, string propertyName)
    {
        var property = target.GetType().GetProperty(propertyName);
        Assert.NotNull(property);
        return ((System.Collections.IEnumerable)(property!.GetValue(target) ?? Array.Empty<object>())).Cast<object>();
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
                Nights = Math.Max(0, days - 1),
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

    private sealed class Phase9TourRepository : ITourRepository
    {
        private readonly List<Tour> _tours;
        public int AddCount { get; private set; }
        public int UpdateCount { get; private set; }

        public Phase9TourRepository(IEnumerable<Tour> tours)
        {
            _tours = tours.ToList();
        }

        public Task<IEnumerable<Tour>> GetAllAsync() => Task.FromResult<IEnumerable<Tour>>(_tours);

        public Task<Tour> GetByIdAsync(string id) => Task.FromResult(_tours.FirstOrDefault(item => item.Id == id)!);

        public Task<IEnumerable<Tour>> FindAsync(Expression<Func<Tour, bool>> predicate)
        {
            var compiled = predicate.Compile();
            return Task.FromResult<IEnumerable<Tour>>(_tours.Where(compiled).ToList());
        }

        public Task AddAsync(Tour entity)
        {
            AddCount++;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(string id, Tour entity)
        {
            UpdateCount++;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(string id) => Task.CompletedTask;

        public Task<PaginatedResult<Tour>> GetPagedAsync(int pageIndex, int pageSize, Expression<Func<Tour, bool>>? filter = null)
        {
            var items = _tours.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToList();
            return Task.FromResult(new PaginatedResult<Tour>(items, _tours.Count, pageIndex, pageSize));
        }

        public Task<TourSearchResult> SearchAsync(TourSearchRequest request)
        {
            var items = _tours
                .Where(tour => !request.PublicOnly || tour.Status is "Active" or "ComingSoon" or "SoldOut")
                .Where(tour => string.IsNullOrWhiteSpace(request.Region)
                    || string.Equals(tour.Destination?.Region, request.Region, StringComparison.OrdinalIgnoreCase))
                .ToList();

            return Task.FromResult(new TourSearchResult
            {
                Items = items,
                TotalItems = items.Count,
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

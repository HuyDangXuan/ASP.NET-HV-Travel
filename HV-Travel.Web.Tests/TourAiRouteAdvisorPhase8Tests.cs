using System.Linq.Expressions;
using System.Reflection;
using HVTravel.Application.Interfaces;
using HVTravel.Application.Services;
using HVTravel.Domain.Entities;
using HVTravel.Domain.Interfaces;
using HVTravel.Domain.Models;
using Xunit;

namespace HVTravel.Web.Tests;

public class TourAiRouteAdvisorPhase8Tests
{
    [Fact]
    public async Task ContextBuilder_WithRouting_IncludesEtaTrafficRelatedToursAndNoCoordinates()
    {
        var currentTour = BuildTour(
            "tour-current",
            "Da Lat Route Focus",
            days: 2,
            adultPrice: 1800000m,
            rating: 4.7,
            city: "Da Lat",
            region: "Tay Nguyen",
            stops:
            [
                Stop("current-1", 1, 1, "Diem hen trung tam", "meeting", 11.9404, 108.4583, 20, 0),
                Stop("current-2", 1, 2, "Rung thong", "forest", 11.9380, 108.4510, 60, 8.6),
                Stop("current-3", 1, 3, "Ho Tuyen Lam", "lake", 11.9200, 108.4300, 45, 8.1)
            ]);

        var relatedSameRegion = BuildTour(
            "tour-related-region",
            "Da Lat Compact Nature",
            days: 2,
            adultPrice: 1600000m,
            rating: 4.6,
            city: "Da Lat",
            region: "Tay Nguyen",
            stops:
            [
                Stop("related-1", 1, 1, "Pickup", "meeting", 11.9400, 108.4580, 15, 0),
                Stop("related-2", 1, 2, "Garden", "park", 11.9390, 108.4550, 45, 7.8)
            ]);

        var unrelated = BuildTour(
            "tour-unrelated",
            "Hue Heritage",
            days: 3,
            adultPrice: 2100000m,
            rating: 4.8,
            city: "Hue",
            region: "Mien Trung",
            stops:
            [
                Stop("hue-1", 1, 1, "Citadel", "landmark", 16.4700, 107.5770, 70, 9.1),
                Stop("hue-2", 1, 2, "Museum", "museum", 16.4720, 107.5840, 50, 8.7)
            ]);

        var context = await BuildContextAsync(currentTour, "compact", [currentTour, relatedSameRegion, unrelated]);
        var snapshotText = GetProperty<string>(context, "SnapshotText");
        var suggestedPrompts = GetProperty<IReadOnlyList<string>>(context, "SuggestedPrompts");
        var relatedSummaries = GetProperty<IReadOnlyList<object>>(context, "RelatedTourSummaries");

        Assert.Contains("compact", snapshotText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("di chuyển", snapshotText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("hành trình", snapshotText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("giao thông", snapshotText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Da Lat Compact Nature", snapshotText, StringComparison.Ordinal);
        Assert.DoesNotContain("108.4583", snapshotText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("11.9404", snapshotText, StringComparison.OrdinalIgnoreCase);

        Assert.InRange(suggestedPrompts.Count, 3, 5);
        Assert.Contains(suggestedPrompts, prompt => prompt.Contains("di chuyển", StringComparison.OrdinalIgnoreCase) || prompt.Contains("kiểu hành trình", StringComparison.OrdinalIgnoreCase));
        Assert.Equal("Da Lat Compact Nature", GetProperty<string>(relatedSummaries[0], "Name"));
    }

    [Fact]
    public async Task ContextBuilder_WithoutRouting_AddsFallbackContextAndGenericPrompts()
    {
        var currentTour = BuildTour(
            "tour-no-routing",
            "Legacy Beach Escape",
            days: 3,
            adultPrice: 2200000m,
            rating: 4.5,
            city: "Phu Quoc",
            region: "Mien Nam",
            stops: []);

        var context = await BuildContextAsync(currentTour, "", [currentTour]);
        var snapshotText = GetProperty<string>(context, "SnapshotText");
        var suggestedPrompts = GetProperty<IReadOnlyList<string>>(context, "SuggestedPrompts");

        Assert.Contains("balanced", snapshotText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("chưa có dữ liệu lộ trình có cấu trúc", snapshotText, StringComparison.OrdinalIgnoreCase);
        Assert.InRange(suggestedPrompts.Count, 3, 5);
        Assert.Contains(suggestedPrompts, prompt => prompt.Contains("phù hợp", StringComparison.OrdinalIgnoreCase) || prompt.Contains("khởi hành", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ChatContracts_WidgetAndController_ExposeRouteStyleAndSuggestedPrompts()
    {
        var chatModelsSource = TestPaths.ReadRepoFile("HV-Travel.Web", "Models", "ChatModels.cs");
        var controllerSource = TestPaths.ReadRepoFile("HV-Travel.Web", "Controllers", "TourAiChatController.cs");
        var widgetSource = TestPaths.ReadRepoFile("HV-Travel.Web", "Views", "Shared", "_TourAiChatWidget.cshtml");
        var serviceInterfaceSource = TestPaths.ReadRepoFile("HV-Travel.Web", "Services", "ITourAiChatService.cs");

        Assert.Contains("RouteStyle", chatModelsSource, StringComparison.Ordinal);
        Assert.Contains("SuggestedPrompts", chatModelsSource, StringComparison.Ordinal);
        Assert.Contains("SuggestedPrompts", controllerSource, StringComparison.Ordinal);
        Assert.Contains("TourAiBootstrapResult", serviceInterfaceSource, StringComparison.Ordinal);
        Assert.Contains("data-route-style", widgetSource, StringComparison.Ordinal);
        Assert.Contains("suggestedPrompts", widgetSource, StringComparison.Ordinal);
        Assert.Contains("routeStyle", widgetSource, StringComparison.Ordinal);
        Assert.Contains("tour-ai-suggested-prompts", widgetSource, StringComparison.Ordinal);
        Assert.Contains("requestSubmit", widgetSource, StringComparison.Ordinal);
    }

    [Fact]
    public void TourAiChatService_UsesRouteAdvisorContextBuilderAndRouteAwarePromptRules()
    {
        var source = TestPaths.ReadRepoFile("HV-Travel.Web", "Services", "TourAiChatService.cs");
        var programSource = TestPaths.ReadRepoFile("HV-Travel.Web", "Program.cs");

        Assert.Contains("ITourAiRouteAdvisorContextBuilder", source, StringComparison.Ordinal);
        Assert.Contains("BuildPromptMessages", source, StringComparison.Ordinal);
        Assert.Contains("theo lộ trình", source, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("RouteInsight", source, StringComparison.Ordinal);
        Assert.Contains("không lộ tọa độ", source, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ITourAiRouteAdvisorContextBuilder", programSource, StringComparison.Ordinal);
    }

    private static async Task<object> BuildContextAsync(Tour currentTour, string? routeStyle, IEnumerable<Tour> allTours)
    {
        var builderType = typeof(HVTravel.Web.Services.ITourAiChatService)
            .Assembly
            .GetType("HVTravel.Web.Services.TourAiRouteAdvisorContextBuilder");
        Assert.NotNull(builderType);

        var repository = new Phase8TourRepository(allTours);
        var routeInsightService = new RouteInsightService(new RouteTravelEstimator());
        var recommendationService = new RouteRecommendationService(routeInsightService);
        var builder = Activator.CreateInstance(builderType!, repository, routeInsightService, recommendationService);
        Assert.NotNull(builder);

        var buildMethod = builderType!.GetMethod("BuildAsync", BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(buildMethod);

        var task = (Task)buildMethod!.Invoke(builder, [currentTour, routeStyle, CancellationToken.None])!;
        await task;

        var result = task.GetType().GetProperty("Result")?.GetValue(task);
        Assert.NotNull(result);
        return result!;
    }

    private static T GetProperty<T>(object target, string propertyName)
    {
        var property = target.GetType().GetProperty(propertyName);
        Assert.NotNull(property);
        return (T)property!.GetValue(target)!;
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

    private sealed class Phase8TourRepository : ITourRepository
    {
        private readonly List<Tour> _tours;

        public Phase8TourRepository(IEnumerable<Tour> tours)
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

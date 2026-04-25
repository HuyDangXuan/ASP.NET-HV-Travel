using System.Reflection;
using HVTravel.Domain.Entities;
using HVTravel.Web.Areas.Admin.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace HVTravel.Web.Tests;

public class RouteOptimizationPhase6Tests
{
    [Fact]
    public void RouteOptimizationService_HighlightsFirst_FrontLoadsHighAttractionStop_MoreThanDistanceFirst()
    {
        var tour = BuildTradeoffTour();

        var distanceFirst = OptimizeRoute(tour, "distance_first");
        var highlightsFirst = OptimizeRoute(tour, "highlights_first");

        var distanceAssignments = ReadAssignments(distanceFirst);
        var highlightAssignments = ReadAssignments(highlightsFirst);

        Assert.Equal("start-anchor", distanceAssignments.First().ClientKey);
        Assert.Equal("end-anchor", distanceAssignments.Last().ClientKey);
        Assert.Equal("start-anchor", highlightAssignments.First().ClientKey);
        Assert.Equal("end-anchor", highlightAssignments.Last().ClientKey);

        var distanceHighPosition = distanceAssignments.FindIndex(item => item.ClientKey == "high-stop");
        var highlightHighPosition = highlightAssignments.FindIndex(item => item.ClientKey == "high-stop");

        Assert.True(highlightHighPosition < distanceHighPosition);

        var distanceInsight = GetProperty<object>(distanceFirst, "SuggestedInsight");
        var highlightInsight = GetProperty<object>(highlightsFirst, "SuggestedInsight");
        Assert.True(
            GetProperty<double>(distanceInsight, "TotalDistanceKm")
            <= GetProperty<double>(highlightInsight, "TotalDistanceKm"));
    }

    [Fact]
    public void RouteOptimizationService_InvalidProfile_FallsBackToBalanced()
    {
        var tour = BuildTradeoffTour();

        var balanced = OptimizeRoute(tour, "balanced");
        var invalid = OptimizeRoute(tour, "not-a-real-profile");

        Assert.Equal(
            BuildAssignmentSignature(balanced),
            BuildAssignmentSignature(invalid));
        Assert.Equal("balanced", GetProperty<string>(invalid, "Profile"));
    }

    [Fact]
    public void RouteOptimizationService_LargeDay_ExposesObjectiveScores_AndPreservesAnchors()
    {
        var tour = BuildLargeDayTour();

        var result = OptimizeRoute(tour, "highlights_first");
        var assignments = ReadAssignments(result);

        Assert.Equal("start", assignments.First().ClientKey);
        Assert.Equal("end", assignments.Last().ClientKey);

        Assert.NotNull(result.GetType().GetProperty("CurrentObjectiveScore"));
        Assert.NotNull(result.GetType().GetProperty("SuggestedObjectiveScore"));
        Assert.True(GetProperty<double>(result, "SuggestedObjectiveScore") >= GetProperty<double>(result, "CurrentObjectiveScore"));
    }

    [Fact]
    public async Task OptimizeRoutingPreview_ReturnsProfileMetadata_AndObjectiveScores()
    {
        var controller = BuildController(new RecordingTourRepository());
        var request = BuildPreviewRequest(
            "distance_first",
            (1, "Day 1", "Tradeoff route"),
            ("start-anchor", 1, 1, "Pickup", "meeting", 10.0, 106.0, 10, 0d, "Start"),
            ("near-stop-a", 1, 2, "Near A", "museum", 10.0, 106.1, 35, 1d, "Near"),
            ("near-stop-b", 1, 3, "Near B", "landmark", 10.0, 106.2, 35, 1d, "Near"),
            ("near-stop-c", 1, 4, "Near C", "museum", 10.0, 106.3, 35, 1d, "Near"),
            ("high-stop", 1, 5, "Scenic Peak", "viewpoint", 10.1, 106.25, 40, 10d, "High"),
            ("end-anchor", 1, 6, "Hotel", "hotel", 10.0, 106.5, 10, 0d, "End"));

        var result = await InvokePreviewAsync(controller, request);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
        Assert.Equal("distance_first", GetProperty<string>(ok.Value!, "Profile"));
        Assert.Equal("Ưu tiên quãng đường", GetProperty<string>(ok.Value!, "ProfileLabel"));
        Assert.Contains("đường đi", GetProperty<string>(ok.Value!, "ProfileDescription"), StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(ok.Value!.GetType().GetProperty("CurrentObjectiveScore"));
        Assert.NotNull(ok.Value!.GetType().GetProperty("SuggestedObjectiveScore"));

        var firstDay = GetProperty<IEnumerable<object>>(ok.Value!, "Days").First();
        Assert.NotNull(firstDay.GetType().GetProperty("CurrentObjectiveScore"));
        Assert.NotNull(firstDay.GetType().GetProperty("SuggestedObjectiveScore"));
    }

    [Fact]
    public async Task OptimizeRoutingPreview_DefaultsProfileToBalanced_WhenMissing()
    {
        var controller = BuildController(new RecordingTourRepository());
        var request = BuildPreviewRequest(
            null,
            (1, "Day 1", "Tradeoff route"),
            ("start-anchor", 1, 1, "Pickup", "meeting", 10.0, 106.0, 10, 0d, "Start"),
            ("near-stop-a", 1, 2, "Near A", "museum", 10.0, 106.1, 35, 1d, "Near"),
            ("near-stop-b", 1, 3, "Near B", "landmark", 10.0, 106.2, 35, 1d, "Near"),
            ("high-stop", 1, 4, "Scenic Peak", "viewpoint", 10.1, 106.25, 40, 10d, "High"),
            ("end-anchor", 1, 5, "Hotel", "hotel", 10.0, 106.5, 10, 0d, "End"));

        var result = await InvokePreviewAsync(controller, request);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("balanced", GetProperty<string>(ok.Value!, "Profile"));
    }

    [Fact]
    public void RouteEditor_Source_ContainsOptimizationProfileSelector_AndSendsProfile()
    {
        var source = TestPaths.ReadRepoFile("HV-Travel.Web", "Areas", "Admin", "Views", "Tours", "_TourDeparturesAndRoutingEditor.cshtml");

        Assert.Contains("Lịch khởi hành", source, StringComparison.Ordinal);
        Assert.Contains("Tối ưu lộ trình", source, StringComparison.Ordinal);
        Assert.Contains("Cấu hình tối ưu", source, StringComparison.Ordinal);
        Assert.Contains("Ưu tiên quãng đường", source, StringComparison.Ordinal);
        Assert.Contains("Ưu tiên điểm nổi bật", source, StringComparison.Ordinal);
        Assert.Contains("Áp dụng gợi ý", source, StringComparison.Ordinal);
        Assert.Contains("route-optimization-profile", source, StringComparison.Ordinal);
        Assert.Contains("profile:", source, StringComparison.Ordinal);
        Assert.Contains("Điểm mục tiêu", source, StringComparison.Ordinal);
    }

    [Fact]
    public void ApplicationContracts_ContainRouteOptimizationRequest_AndProfileOverload()
    {
        var interfaceSource = TestPaths.ReadRepoFile("HV-Travel.Application", "Interfaces", "ICommerceServices.cs");
        var modelSource = TestPaths.ReadRepoFile("HV-Travel.Application", "Models", "RouteOptimizationModels.cs");

        Assert.Contains("RouteOptimizationRequest", interfaceSource, StringComparison.Ordinal);
        Assert.Contains("Optimize(HVTravel.Domain.Entities.Tour? tour, RouteOptimizationRequest request)", interfaceSource, StringComparison.Ordinal);
        Assert.Contains("Profile", modelSource, StringComparison.Ordinal);
        Assert.Contains("CurrentObjectiveScore", modelSource, StringComparison.Ordinal);
        Assert.Contains("SuggestedObjectiveScore", modelSource, StringComparison.Ordinal);
    }

    private static object OptimizeRoute(Tour tour, string? profile)
    {
        var serviceType = typeof(HVTravel.Application.Services.PricingService).Assembly.GetType("HVTravel.Application.Services.RouteOptimizationService");
        var requestType = serviceType?.Assembly.GetType("HVTravel.Application.Models.RouteOptimizationRequest");
        Assert.NotNull(serviceType);
        Assert.NotNull(requestType);

        var request = Activator.CreateInstance(requestType!);
        Assert.NotNull(request);
        SetProperty(request!, "Profile", profile);

        var service = Activator.CreateInstance(serviceType!);
        Assert.NotNull(service);

        var optimizeMethod = serviceType!.GetMethod("Optimize", [typeof(Tour), requestType!]);
        Assert.NotNull(optimizeMethod);

        var result = optimizeMethod!.Invoke(service, [tour, request]);
        Assert.NotNull(result);
        return result!;
    }

    private static async Task<IActionResult> InvokePreviewAsync(ToursController controller, object request)
    {
        var method = typeof(ToursController).GetMethod("OptimizeRoutingPreview", BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(method);

        var task = method!.Invoke(controller, [request]);
        Assert.NotNull(task);

        var actionTask = Assert.IsAssignableFrom<Task<IActionResult>>(task);
        return await actionTask;
    }

    private static object BuildPreviewRequest(
        string? profile,
        (int Day, string Title, string Description) schedule,
        params (string ClientKey, int Day, int Order, string Name, string Type, double? Lat, double? Lng, int VisitMinutes, double AttractionScore, string Note)[] stops)
    {
        var webAssembly = typeof(ToursController).Assembly;
        var requestType = webAssembly.GetType("HVTravel.Web.Models.RouteOptimizationPreviewRequest");
        var scheduleItemType = webAssembly.GetType("HVTravel.Web.Models.RouteOptimizationPreviewScheduleItem");
        var stopType = webAssembly.GetType("HVTravel.Web.Models.RouteOptimizationPreviewStop");
        var geoType = webAssembly.GetType("HVTravel.Web.Models.RouteOptimizationPreviewGeoPoint");

        Assert.NotNull(requestType);
        Assert.NotNull(scheduleItemType);
        Assert.NotNull(stopType);
        Assert.NotNull(geoType);

        var request = Activator.CreateInstance(requestType!);
        Assert.NotNull(request);
        SetProperty(request!, "Profile", profile);

        var scheduleItem = Activator.CreateInstance(scheduleItemType!);
        Assert.NotNull(scheduleItem);
        SetProperty(scheduleItem!, "Day", schedule.Day);
        SetProperty(scheduleItem!, "Title", schedule.Title);
        SetProperty(scheduleItem!, "Description", schedule.Description);

        var scheduleList = (System.Collections.IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(scheduleItemType!))!;
        scheduleList.Add(scheduleItem);
        SetProperty(request!, "Schedule", scheduleList);

        var stopList = (System.Collections.IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(stopType!))!;
        foreach (var stop in stops)
        {
            var stopItem = Activator.CreateInstance(stopType!);
            Assert.NotNull(stopItem);

            var geoPoint = Activator.CreateInstance(geoType!);
            Assert.NotNull(geoPoint);
            SetProperty(geoPoint!, "Lat", stop.Lat);
            SetProperty(geoPoint!, "Lng", stop.Lng);

            SetProperty(stopItem!, "ClientKey", stop.ClientKey);
            SetProperty(stopItem!, "Day", stop.Day);
            SetProperty(stopItem!, "Order", stop.Order);
            SetProperty(stopItem!, "Name", stop.Name);
            SetProperty(stopItem!, "Type", stop.Type);
            SetProperty(stopItem!, "Coordinates", geoPoint);
            SetProperty(stopItem!, "VisitMinutes", stop.VisitMinutes);
            SetProperty(stopItem!, "AttractionScore", stop.AttractionScore);
            SetProperty(stopItem!, "Note", stop.Note);
            stopList.Add(stopItem);
        }

        SetProperty(request!, "Stops", stopList);
        return request!;
    }

    private static List<(string ClientKey, int Day, int Order)> ReadAssignments(object result)
    {
        return GetProperty<IEnumerable<object>>(result, "Assignments")
            .Select(item => (
                ClientKey: GetProperty<string>(item, "ClientKey"),
                Day: GetProperty<int>(item, "Day"),
                Order: GetProperty<int>(item, "Order")))
            .OrderBy(item => item.Day)
            .ThenBy(item => item.Order)
            .ToList();
    }

    private static string BuildAssignmentSignature(object result)
    {
        return string.Join("|", ReadAssignments(result).Select(item => $"{item.ClientKey}:{item.Day}:{item.Order}"));
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

    private static ToursController BuildController(RecordingTourRepository repo)
    {
        return new ToursController(repo)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }

    private static Tour BuildTradeoffTour()
    {
        return new Tour
        {
            Id = "tour-phase6",
            Slug = "tour-phase6",
            Code = "P6-001",
            Name = "Phase 6 Tradeoff Tour",
            Description = "Tradeoff tour",
            ShortDescription = "Tradeoff tour",
            Destination = new Destination
            {
                City = "Da Nang",
                Country = "Viet Nam",
                Region = "Mien Trung"
            },
            Price = new TourPrice { Adult = 1000000m, Child = 800000m, Infant = 200000m, Currency = "VND" },
            Duration = new TourDuration { Days = 2, Nights = 1, Text = "2 ngay 1 dem" },
            Status = "Draft",
            ConfirmationType = "Instant",
            Schedule =
            [
                new ScheduleItem { Day = 1, Title = "Day 1", Description = "Tradeoff route" }
            ],
            Routing = new TourRouting
            {
                SchemaVersion = 1,
                Stops =
                [
                    new TourRouteStop { Id = "start-anchor", Day = 1, Order = 1, Name = "Pickup", Type = "meeting", VisitMinutes = 10, Coordinates = new GeoPoint { Lat = 10.0, Lng = 106.0 } },
                    new TourRouteStop { Id = "near-stop-a", Day = 1, Order = 2, Name = "Near A", Type = "museum", VisitMinutes = 30, AttractionScore = 1, Coordinates = new GeoPoint { Lat = 10.0, Lng = 106.1 } },
                    new TourRouteStop { Id = "near-stop-b", Day = 1, Order = 3, Name = "Near B", Type = "landmark", VisitMinutes = 30, AttractionScore = 1, Coordinates = new GeoPoint { Lat = 10.0, Lng = 106.2 } },
                    new TourRouteStop { Id = "near-stop-c", Day = 1, Order = 4, Name = "Near C", Type = "museum", VisitMinutes = 30, AttractionScore = 1, Coordinates = new GeoPoint { Lat = 10.0, Lng = 106.3 } },
                    new TourRouteStop { Id = "high-stop", Day = 1, Order = 5, Name = "Scenic Peak", Type = "viewpoint", VisitMinutes = 45, AttractionScore = 10, Coordinates = new GeoPoint { Lat = 10.1, Lng = 106.25 } },
                    new TourRouteStop { Id = "end-anchor", Day = 1, Order = 6, Name = "Hotel", Type = "hotel", VisitMinutes = 10, Coordinates = new GeoPoint { Lat = 10.0, Lng = 106.5 } }
                ]
            }
        };
    }

    private static Tour BuildLargeDayTour()
    {
        return new Tour
        {
            Id = "tour-large-phase6",
            Slug = "tour-large-phase6",
            Code = "P6-002",
            Name = "Large Day Tour",
            Description = "Large day tour",
            ShortDescription = "Large day tour",
            Destination = new Destination
            {
                City = "Ho Chi Minh",
                Country = "Viet Nam",
                Region = "Mien Nam"
            },
            Price = new TourPrice { Adult = 1200000m, Child = 900000m, Infant = 300000m, Currency = "VND" },
            Duration = new TourDuration { Days = 2, Nights = 1, Text = "2 ngay 1 dem" },
            Status = "Draft",
            ConfirmationType = "Instant",
            Schedule =
            [
                new ScheduleItem { Day = 1, Title = "Day 1", Description = "Large route" }
            ],
            Routing = new TourRouting
            {
                SchemaVersion = 1,
                Stops =
                [
                    new TourRouteStop { Id = "start", Day = 1, Order = 1, Name = "Pickup", Type = "meeting", VisitMinutes = 10, Coordinates = new GeoPoint { Lat = 10.0, Lng = 106.0 } },
                    new TourRouteStop { Id = "s1", Day = 1, Order = 2, Name = "Stop 1", Type = "museum", VisitMinutes = 15, AttractionScore = 2, Coordinates = new GeoPoint { Lat = 10.01, Lng = 106.01 } },
                    new TourRouteStop { Id = "s2", Day = 1, Order = 3, Name = "Stop 2", Type = "landmark", VisitMinutes = 15, AttractionScore = 3, Coordinates = new GeoPoint { Lat = 10.02, Lng = 106.02 } },
                    new TourRouteStop { Id = "s3", Day = 1, Order = 4, Name = "Stop 3", Type = "museum", VisitMinutes = 15, AttractionScore = 4, Coordinates = new GeoPoint { Lat = 10.03, Lng = 106.03 } },
                    new TourRouteStop { Id = "s4", Day = 1, Order = 5, Name = "Stop 4", Type = "museum", VisitMinutes = 15, AttractionScore = 9, Coordinates = new GeoPoint { Lat = 10.04, Lng = 106.08 } },
                    new TourRouteStop { Id = "s5", Day = 1, Order = 6, Name = "Stop 5", Type = "landmark", VisitMinutes = 15, AttractionScore = 5, Coordinates = new GeoPoint { Lat = 10.05, Lng = 106.05 } },
                    new TourRouteStop { Id = "s6", Day = 1, Order = 7, Name = "Stop 6", Type = "museum", VisitMinutes = 15, AttractionScore = 6, Coordinates = new GeoPoint { Lat = 10.06, Lng = 106.06 } },
                    new TourRouteStop { Id = "s7", Day = 1, Order = 8, Name = "Stop 7", Type = "landmark", VisitMinutes = 15, AttractionScore = 7, Coordinates = new GeoPoint { Lat = 10.07, Lng = 106.07 } },
                    new TourRouteStop { Id = "s8", Day = 1, Order = 9, Name = "Stop 8", Type = "museum", VisitMinutes = 15, AttractionScore = 8, Coordinates = new GeoPoint { Lat = 10.08, Lng = 106.09 } },
                    new TourRouteStop { Id = "end", Day = 1, Order = 10, Name = "Hotel", Type = "hotel", VisitMinutes = 10, Coordinates = new GeoPoint { Lat = 10.09, Lng = 106.10 } }
                ]
            }
        };
    }
}

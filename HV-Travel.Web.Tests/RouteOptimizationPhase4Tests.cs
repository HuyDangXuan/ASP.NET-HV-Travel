using System.Reflection;
using HVTravel.Application.Interfaces;
using HVTravel.Application.Services;
using HVTravel.Domain.Entities;
using HVTravel.Web.Areas.Admin.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace HVTravel.Web.Tests;

public class RouteOptimizationPhase4Tests
{
    [Fact]
    public void RouteOptimizationService_ReordersWithinDay_AndPreservesAnchors()
    {
        var tour = BuildTourWithRouting(
            new TourRouteStop
            {
                Id = "start-anchor",
                Day = 1,
                Order = 1,
                Name = "Pickup point",
                Type = "meeting",
                VisitMinutes = 10,
                Coordinates = new GeoPoint { Lat = 10.0, Lng = 106.0 }
            },
            new TourRouteStop
            {
                Id = "middle-far",
                Day = 1,
                Order = 2,
                Name = "Far landmark",
                Type = "landmark",
                VisitMinutes = 35,
                AttractionScore = 6,
                Coordinates = new GeoPoint { Lat = 10.0, Lng = 106.2 }
            },
            new TourRouteStop
            {
                Id = "middle-near",
                Day = 1,
                Order = 3,
                Name = "Near museum",
                Type = "museum",
                VisitMinutes = 40,
                AttractionScore = 8,
                Coordinates = new GeoPoint { Lat = 10.0, Lng = 106.1 }
            },
            new TourRouteStop
            {
                Id = "end-anchor",
                Day = 1,
                Order = 4,
                Name = "Hotel checkin",
                Type = "hotel",
                VisitMinutes = 15,
                Coordinates = new GeoPoint { Lat = 10.0, Lng = 106.3 }
            });

        var result = OptimizeRoute(tour);

        Assert.True(GetProperty<bool>(result, "CanOptimize"));

        var currentInsight = GetProperty<object>(result, "CurrentInsight");
        var suggestedInsight = GetProperty<object>(result, "SuggestedInsight");
        Assert.True(GetProperty<int>(suggestedInsight, "TotalTravelMinutes") < GetProperty<int>(currentInsight, "TotalTravelMinutes"));

        var assignments = GetProperty<IEnumerable<object>>(result, "Assignments")
            .Select(item => new
            {
                ClientKey = GetProperty<string>(item, "ClientKey"),
                Day = GetProperty<int>(item, "Day"),
                Order = GetProperty<int>(item, "Order")
            })
            .OrderBy(item => item.Order)
            .ToList();

        Assert.Equal(new[] { 1, 1, 1, 1 }, assignments.Select(item => item.Day).ToArray());
        Assert.Equal("start-anchor", assignments[0].ClientKey);
        Assert.Equal("middle-near", assignments[1].ClientKey);
        Assert.Equal("middle-far", assignments[2].ClientKey);
        Assert.Equal("end-anchor", assignments[3].ClientKey);
    }

    [Fact]
    public void RouteOptimizationService_SkipsDay_WhenCoordinatesAreMissing()
    {
        var tour = BuildTourWithRouting(
            new TourRouteStop
            {
                Id = "stop-1",
                Day = 1,
                Order = 1,
                Name = "Pickup",
                Type = "meeting",
                VisitMinutes = 10,
                Coordinates = new GeoPoint { Lat = 10.0, Lng = 106.0 }
            },
            new TourRouteStop
            {
                Id = "stop-2",
                Day = 1,
                Order = 2,
                Name = "Museum",
                Type = "museum",
                VisitMinutes = 30,
                Coordinates = new GeoPoint { Lat = null, Lng = null }
            },
            new TourRouteStop
            {
                Id = "stop-3",
                Day = 1,
                Order = 3,
                Name = "Hotel",
                Type = "hotel",
                VisitMinutes = 10,
                Coordinates = new GeoPoint { Lat = 10.0, Lng = 106.3 }
            });

        var result = OptimizeRoute(tour);

        Assert.False(GetProperty<bool>(result, "CanOptimize"));
        Assert.False(string.IsNullOrWhiteSpace(GetProperty<string>(result, "UnchangedReason")));

        var warnings = GetProperty<IEnumerable<object>>(result, "Warnings").ToList();
        Assert.NotEmpty(warnings);
        Assert.Contains(warnings, warning =>
            GetProperty<string>(warning, "Message").Contains("coordinates", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void RouteOptimizationService_LeavesShortDaysUnchanged()
    {
        var tour = BuildTourWithRouting(
            new TourRouteStop
            {
                Id = "stop-1",
                Day = 1,
                Order = 1,
                Name = "Stop 1",
                Type = "viewpoint",
                VisitMinutes = 20,
                Coordinates = new GeoPoint { Lat = 11.0, Lng = 108.0 }
            },
            new TourRouteStop
            {
                Id = "stop-2",
                Day = 1,
                Order = 2,
                Name = "Stop 2",
                Type = "lake",
                VisitMinutes = 20,
                Coordinates = new GeoPoint { Lat = 11.1, Lng = 108.1 }
            });

        var result = OptimizeRoute(tour);

        Assert.False(GetProperty<bool>(result, "CanOptimize"));

        var assignments = GetProperty<IEnumerable<object>>(result, "Assignments")
            .Select(item => GetProperty<string>(item, "ClientKey"))
            .ToArray();

        Assert.Equal(new[] { "stop-1", "stop-2" }, assignments);
    }

    [Fact]
    public void RouteOptimizationService_IsDeterministic_ForLargeDays()
    {
        var stops = new List<TourRouteStop>
        {
            new()
            {
                Id = "start",
                Day = 1,
                Order = 1,
                Name = "Pickup",
                Type = "meeting",
                VisitMinutes = 10,
                Coordinates = new GeoPoint { Lat = 10.0, Lng = 106.0 }
            },
            new()
            {
                Id = "s1",
                Day = 1,
                Order = 2,
                Name = "Stop 1",
                Type = "landmark",
                VisitMinutes = 15,
                AttractionScore = 4,
                Coordinates = new GeoPoint { Lat = 10.00, Lng = 106.01 }
            },
            new()
            {
                Id = "s2",
                Day = 1,
                Order = 3,
                Name = "Stop 2",
                Type = "museum",
                VisitMinutes = 15,
                AttractionScore = 5,
                Coordinates = new GeoPoint { Lat = 10.01, Lng = 106.02 }
            },
            new()
            {
                Id = "s3",
                Day = 1,
                Order = 4,
                Name = "Stop 3",
                Type = "museum",
                VisitMinutes = 15,
                AttractionScore = 7,
                Coordinates = new GeoPoint { Lat = 10.02, Lng = 106.03 }
            },
            new()
            {
                Id = "s4",
                Day = 1,
                Order = 5,
                Name = "Stop 4",
                Type = "landmark",
                VisitMinutes = 15,
                AttractionScore = 6,
                Coordinates = new GeoPoint { Lat = 10.03, Lng = 106.04 }
            },
            new()
            {
                Id = "s5",
                Day = 1,
                Order = 6,
                Name = "Stop 5",
                Type = "museum",
                VisitMinutes = 15,
                AttractionScore = 3,
                Coordinates = new GeoPoint { Lat = 10.04, Lng = 106.05 }
            },
            new()
            {
                Id = "s6",
                Day = 1,
                Order = 7,
                Name = "Stop 6",
                Type = "landmark",
                VisitMinutes = 15,
                AttractionScore = 8,
                Coordinates = new GeoPoint { Lat = 10.05, Lng = 106.06 }
            },
            new()
            {
                Id = "s7",
                Day = 1,
                Order = 8,
                Name = "Stop 7",
                Type = "museum",
                VisitMinutes = 15,
                AttractionScore = 2,
                Coordinates = new GeoPoint { Lat = 10.06, Lng = 106.07 }
            },
            new()
            {
                Id = "s8",
                Day = 1,
                Order = 9,
                Name = "Stop 8",
                Type = "museum",
                VisitMinutes = 15,
                AttractionScore = 9,
                Coordinates = new GeoPoint { Lat = 10.07, Lng = 106.08 }
            },
            new()
            {
                Id = "end",
                Day = 1,
                Order = 10,
                Name = "Hotel",
                Type = "hotel",
                VisitMinutes = 10,
                Coordinates = new GeoPoint { Lat = 10.08, Lng = 106.09 }
            }
        };

        var tour = BuildTourWithRouting(stops.ToArray());

        var first = OptimizeRoute(tour);
        var second = OptimizeRoute(tour);

        var firstSequence = BuildAssignmentSignature(first);
        var secondSequence = BuildAssignmentSignature(second);

        Assert.Equal(firstSequence, secondSequence);
    }

    [Fact]
    public async Task OptimizeRoutingPreview_ReturnsSuggestion_WithoutPersisting()
    {
        var repo = new RecordingTourRepository();
        var controller = BuildController(repo);

        var request = BuildPreviewRequest(
            (1, "Day 1", "Morning route"),
            ("start-anchor", 1, 1, "Pickup point", "meeting", 10.0, 106.0, 10, 0d, "Start"),
            ("middle-far", 1, 2, "Far landmark", "landmark", 10.0, 106.2, 35, 6d, "Far"),
            ("middle-near", 1, 3, "Near museum", "museum", 10.0, 106.1, 40, 8d, "Near"),
            ("end-anchor", 1, 4, "Hotel checkin", "hotel", 10.0, 106.3, 15, 0d, "End"));

        var result = await InvokePreviewAsync(controller, request);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);

        var assignments = GetProperty<IEnumerable<object>>(ok.Value!, "Assignments").ToList();
        Assert.NotEmpty(assignments);
        Assert.Contains(assignments, item => GetProperty<string>(item, "ClientKey") == "middle-near" && GetProperty<int>(item, "Order") == 2);

        Assert.Null(repo.LastAdded);
        Assert.Null(repo.LastUpdated);
    }

    [Fact]
    public async Task OptimizeRoutingPreview_ReturnsBadRequest_WhenRoutingDayIsInvalid()
    {
        var repo = new RecordingTourRepository();
        var controller = BuildController(repo);

        var request = BuildPreviewRequest(
            (1, "Day 1", "Morning route"),
            ("stop-1", 2, 1, "Invalid day stop", "museum", 10.0, 106.0, 30, 5d, "Bad day"),
            ("stop-2", 2, 2, "Another stop", "landmark", 10.1, 106.1, 40, 6d, "Bad day"));

        var result = await InvokePreviewAsync(controller, request);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequest.Value);
        Assert.Null(repo.LastAdded);
        Assert.Null(repo.LastUpdated);
    }

    [Fact]
    public void ToursController_Source_ContainsOptimizePreviewEndpoint_WithAntiforgery()
    {
        var source = TestPaths.ReadRepoFile("HV-Travel.Web", "Areas", "Admin", "Controllers", "ToursController.cs");

        Assert.Contains("OptimizeRoutingPreview", source, StringComparison.Ordinal);
        Assert.Contains("ValidateAntiForgeryToken", source, StringComparison.Ordinal);
        Assert.Contains("HttpPost(\"OptimizeRoutingPreview\")", source, StringComparison.Ordinal);
    }

    [Fact]
    public void RouteEditor_Source_ContainsOptimizerPreviewUi()
    {
        var source = TestPaths.ReadRepoFile("HV-Travel.Web", "Areas", "Admin", "Views", "Tours", "_TourDeparturesAndRoutingEditor.cshtml");

        Assert.Contains("Generate optimization", source, StringComparison.Ordinal);
        Assert.Contains("Apply suggestion", source, StringComparison.Ordinal);
        Assert.Contains("Discard preview", source, StringComparison.Ordinal);
        Assert.Contains("data-route-key", source, StringComparison.Ordinal);
        Assert.Contains("route-optimization-preview", source, StringComparison.Ordinal);
    }

    [Fact]
    public void ApplicationContracts_RegisterRouteOptimizationService()
    {
        var interfaceSource = TestPaths.ReadRepoFile("HV-Travel.Application", "Interfaces", "ICommerceServices.cs");
        var registrationSource = TestPaths.ReadRepoFile("HV-Travel.Application", "DependencyInjection.cs");

        Assert.Contains("IRouteOptimizationService", interfaceSource, StringComparison.Ordinal);
        Assert.Contains("RouteOptimizationService", registrationSource, StringComparison.Ordinal);
    }

    private static string BuildAssignmentSignature(object result)
    {
        return string.Join("|",
            GetProperty<IEnumerable<object>>(result, "Assignments")
                .OrderBy(item => GetProperty<int>(item, "Day"))
                .ThenBy(item => GetProperty<int>(item, "Order"))
                .Select(item => $"{GetProperty<string>(item, "ClientKey")}:{GetProperty<int>(item, "Day")}:{GetProperty<int>(item, "Order")}"));
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

    private static void SetProperty(object target, string propertyName, object? value)
    {
        var property = target.GetType().GetProperty(propertyName);
        Assert.NotNull(property);
        property!.SetValue(target, value);
    }

    private static object OptimizeRoute(Tour tour)
    {
        var serviceType = typeof(PricingService).Assembly.GetType("HVTravel.Application.Services.RouteOptimizationService");
        var interfaceType = typeof(IPricingService).Assembly.GetType("HVTravel.Application.Interfaces.IRouteInsightService");
        Assert.NotNull(serviceType);
        Assert.NotNull(interfaceType);

        var routeInsight = new RouteInsightService();
        var service = serviceType!.GetConstructor(Type.EmptyTypes) != null
            ? Activator.CreateInstance(serviceType)
            : Activator.CreateInstance(serviceType, routeInsight);

        Assert.NotNull(service);

        var optimizeMethod = serviceType.GetMethod("Optimize", BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(optimizeMethod);

        var result = optimizeMethod!.Invoke(service, [tour]);
        Assert.NotNull(result);
        return result!;
    }

    private static T GetProperty<T>(object target, string propertyName)
    {
        var property = target.GetType().GetProperty(propertyName);
        Assert.NotNull(property);
        return (T)property!.GetValue(target)!;
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

    private static Tour BuildTourWithRouting(params TourRouteStop[] stops)
    {
        return new Tour
        {
            Id = "tour-opt-1",
            Slug = "route-optimizer-tour",
            Code = "T-OPT-01",
            Name = "Route Optimizer Tour",
            Description = "Optimizer test tour",
            ShortDescription = "Optimizer test",
            Destination = new Destination
            {
                City = "Ho Chi Minh",
                Country = "Viet Nam",
                Region = "Mien Nam"
            },
            Price = new TourPrice
            {
                Adult = 1000000m,
                Child = 800000m,
                Infant = 200000m,
                Currency = "VND"
            },
            Duration = new TourDuration
            {
                Days = 2,
                Nights = 1,
                Text = "2 ngay 1 dem"
            },
            Status = "Draft",
            ConfirmationType = "Instant",
            Schedule =
            [
                new ScheduleItem { Day = 1, Title = "Day 1", Description = "Route day" },
                new ScheduleItem { Day = 2, Title = "Day 2", Description = "Backup day" }
            ],
            Routing = new TourRouting
            {
                SchemaVersion = 1,
                Stops = stops.ToList()
            }
        };
    }
}

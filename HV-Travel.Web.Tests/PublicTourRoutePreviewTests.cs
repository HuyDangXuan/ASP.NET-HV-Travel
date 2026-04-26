using System.Collections;
using System.Reflection;
using HVTravel.Domain.Entities;

namespace HV_Travel.Web.Tests;

public class PublicTourRoutePreviewTests
{
    private static readonly string RepoRoot = GetRepoRoot();

    [Fact]
    public void RouteOverviewBuilder_GroupsStopsByDay_SortsByOrder_UsesScheduleTitle_AndCalculatesSummary()
    {
        var tour = new Tour
        {
            Schedule = new List<ScheduleItem>
            {
                new() { Day = 1, Title = "Mountain pass start" },
                new() { Day = 2, Title = "River town arrival" }
            },
            Routing = new TourRouting
            {
                Stops = new List<TourRouteStop>
                {
                    new() { Id = "d2-s2", Day = 2, Order = 2, Name = "Night market", Type = "market", VisitMinutes = 45, Note = "Street food stop" },
                    new() { Id = "d1-s2", Day = 1, Order = 2, Name = "Twin Mountains lookout", Type = "viewpoint", VisitMinutes = 35, Note = "Photo stop" },
                    new() { Id = "d1-s1", Day = 1, Order = 1, Name = "Quan Ba gate", Type = "landmark", VisitMinutes = 20, Note = "Quick break" },
                    new() { Id = "d2-s1", Day = 2, Order = 1, Name = "Boat pier", Type = "pier", VisitMinutes = 30, Note = "Boarding" }
                }
            }
        };

        var overview = BuildRouteOverview(tour);

        Assert.True(GetProperty<bool>(overview, "HasRouting"));
        Assert.Equal(2, GetProperty<int>(overview, "DayCount"));
        Assert.Equal(4, GetProperty<int>(overview, "StopCount"));
        Assert.Equal(130, GetProperty<int>(overview, "TotalVisitMinutes"));

        var days = GetItems(overview, "Days");
        Assert.Equal(2, days.Count);

        Assert.Equal(1, GetProperty<int>(days[0], "Day"));
        Assert.Equal("Mountain pass start", GetProperty<string>(days[0], "DayTitle"));
        var dayOneStops = GetItems(days[0], "Stops");
        Assert.Equal(new[] { "Quan Ba gate", "Twin Mountains lookout" }, dayOneStops.Select(item => GetProperty<string>(item, "Name")).ToArray());

        Assert.Equal(2, GetProperty<int>(days[1], "Day"));
        Assert.Equal("River town arrival", GetProperty<string>(days[1], "DayTitle"));
        var dayTwoStops = GetItems(days[1], "Stops");
        Assert.Equal(new[] { "Boat pier", "Night market" }, dayTwoStops.Select(item => GetProperty<string>(item, "Name")).ToArray());
    }

    [Fact]
    public void RouteOverviewBuilder_FallsBackToDayLabel_AndSkipsStopsWithoutName()
    {
        var tour = new Tour
        {
            Schedule = new List<ScheduleItem>
            {
                new() { Day = 1, Title = "Departure day" }
            },
            Routing = new TourRouting
            {
                Stops = new List<TourRouteStop>
                {
                    new() { Id = "invalid", Day = 2, Order = 1, Name = "   ", Type = "ignore", VisitMinutes = 10, Note = "ignore" },
                    new() { Id = "valid", Day = 2, Order = 2, Name = "Lantern street", Type = "cultural", VisitMinutes = 25, Note = "Evening walk" }
                }
            }
        };

        var overview = BuildRouteOverview(tour);

        Assert.True(GetProperty<bool>(overview, "HasRouting"));
        Assert.Equal(1, GetProperty<int>(overview, "DayCount"));
        Assert.Equal(1, GetProperty<int>(overview, "StopCount"));
        Assert.Equal(25, GetProperty<int>(overview, "TotalVisitMinutes"));

        var days = GetItems(overview, "Days");
        var routeDay = Assert.Single(days);
        Assert.Equal(2, GetProperty<int>(routeDay, "Day"));
        Assert.Equal("Day 2", GetProperty<string>(routeDay, "DayTitle"));
        var stops = GetItems(routeDay, "Stops");
        Assert.Equal("Lantern street", Assert.Single(stops).GetType().GetProperty("Name")?.GetValue(stops[0]) as string);
    }

    [Fact]
    public void PublicTextSanitizer_NormalizesRoutingStopFields()
    {
        var sanitizerType = FindType("HVTravel.Web.Services.PublicTextSanitizer");
        Assert.NotNull(sanitizerType);

        var normalizeMethod = sanitizerType!.GetMethod(
            "NormalizeTourForDisplay",
            BindingFlags.Public | BindingFlags.Static,
            binder: null,
            types: new[] { typeof(Tour) },
            modifiers: null);

        Assert.NotNull(normalizeMethod);

        var tour = new Tour
        {
            Routing = new TourRouting
            {
                Stops = new List<TourRouteStop>
                {
                    new()
                    {
                        Id = "stop-1",
                        Day = 1,
                        Order = 1,
                        Name = "  Twin Mountains lookout  ",
                        Type = "  viewpoint  ",
                        Note = "  Photo stop with a short uphill walk.  "
                    }
                }
            }
        };

        var normalized = normalizeMethod!.Invoke(null, new object[] { tour }) as Tour;
        Assert.NotNull(normalized);

        var stop = Assert.Single(normalized!.Routing!.Stops);
        Assert.Equal("Twin Mountains lookout", stop.Name);
        Assert.Equal("viewpoint", stop.Type);
        Assert.Equal("Photo stop with a short uphill walk.", stop.Note);
    }

    [Fact]
    public void PublicTourDetails_View_ContainsRouteOverviewMarkup_WithoutSensitiveRouteFields()
    {
        var markup = ReadRepoFile("HV-Travel.Web", "Views", "PublicTours", "Details.cshtml");

        Assert.Contains("GetSection(\"routing\")", markup, StringComparison.Ordinal);
        Assert.Contains("ViewData[\"RouteOverview\"]", markup, StringComparison.Ordinal);
        Assert.Contains("routeOverview.HasRouting", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("Coordinates", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("AttractionScore", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void PublicToursController_Details_WiresRouteOverviewIntoViewData()
    {
        var controllerSource = ReadRepoFile("HV-Travel.Web", "Controllers", "PublicToursController.cs");

        Assert.Contains("ViewData[\"RouteOverview\"]", controllerSource, StringComparison.Ordinal);
        Assert.Contains("PublicTourRouteOverviewBuilder.Build", controllerSource, StringComparison.Ordinal);
    }

    [Fact]
    public void PublicContentDefaults_RegistersRoutingSection_ForPublicTourDetails()
    {
        var defaultsSource = ReadRepoFile("HV-Travel.Web", "Services", "PublicContentDefaults.cs");

        Assert.Contains("\"routing\"", defaultsSource, StringComparison.Ordinal);
        Assert.Contains("Section(\"publicTourDetails\", \"routing\"", defaultsSource, StringComparison.Ordinal);
    }

    private static object BuildRouteOverview(Tour tour)
    {
        var builderType = FindType("HVTravel.Web.Services.PublicTourRouteOverviewBuilder");
        Assert.NotNull(builderType);

        var buildMethod = builderType!.GetMethod(
            "Build",
            BindingFlags.Public | BindingFlags.Static,
            binder: null,
            types: new[] { typeof(Tour) },
            modifiers: null);

        Assert.NotNull(buildMethod);

        var result = buildMethod!.Invoke(null, new object[] { tour });
        Assert.NotNull(result);
        return result!;
    }

    private static Type? FindType(string fullName)
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .Select(assembly => assembly.GetType(fullName, throwOnError: false, ignoreCase: false))
            .FirstOrDefault(type => type != null);
    }

    private static T GetProperty<T>(object instance, string propertyName)
    {
        var property = instance.GetType().GetProperty(propertyName);
        Assert.NotNull(property);
        return (T)property!.GetValue(instance)!;
    }

    private static List<object> GetItems(object instance, string propertyName)
    {
        var property = instance.GetType().GetProperty(propertyName);
        Assert.NotNull(property);

        var enumerable = property!.GetValue(instance) as IEnumerable;
        Assert.NotNull(enumerable);

        return enumerable!.Cast<object>().ToList();
    }

    private static string ReadRepoFile(params string[] segments)
    {
        return File.ReadAllText(Path.Combine(new[] { RepoRoot }.Concat(segments).ToArray()));
    }

    private static string GetRepoRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "HV-Travel.slnx")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root.");
    }
}

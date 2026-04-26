using System.Reflection;
using HVTravel.Domain.Entities;
using HVTravel.Web.Services;
using Xunit;

namespace HVTravel.Web.Tests;

public class RouteIntelligencePhase3Tests
{
    [Fact]
    public void RouteTravelEstimator_AppliesDayPartTrafficPenalties_Deterministically()
    {
        var estimatorType = typeof(HVTravel.Application.Services.PricingService).Assembly.GetType("HVTravel.Application.Services.RouteTravelEstimator");
        Assert.NotNull(estimatorType);

        var estimator = Activator.CreateInstance(estimatorType!);
        Assert.NotNull(estimator);

        var estimateMethod = estimatorType!.GetMethod("Estimate", BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(estimateMethod);

        var fromStop = new TourRouteStop
        {
            Id = "from-stop",
            Day = 1,
            Order = 1,
            Name = "Cho trung tam",
            Type = "market",
            Coordinates = new GeoPoint { Lat = 10.7769, Lng = 106.7009 }
        };

        var toStop = new TourRouteStop
        {
            Id = "to-stop",
            Day = 1,
            Order = 2,
            Name = "Bao tang thanh pho",
            Type = "museum",
            Coordinates = new GeoPoint { Lat = 10.7898, Lng = 106.6992 }
        };

        var urbanMorningPeak = estimateMethod!.Invoke(estimator, [fromStop, toStop, "urban", 7 * 60 + 30]);
        var urbanLateMorning = estimateMethod.Invoke(estimator, [fromStop, toStop, "urban", 9 * 60 + 30]);
        var scenicMorningPeak = estimateMethod.Invoke(estimator, [fromStop, toStop, "scenic", 7 * 60 + 30]);
        var urbanMorningPeakRepeat = estimateMethod.Invoke(estimator, [fromStop, toStop, "urban", 7 * 60 + 30]);

        Assert.NotNull(urbanMorningPeak);
        Assert.NotNull(urbanLateMorning);
        Assert.NotNull(scenicMorningPeak);
        Assert.NotNull(urbanMorningPeakRepeat);

        Assert.Equal("morning_peak", GetProperty<string>(urbanMorningPeak!, "DayPart"));
        Assert.Equal("late_morning", GetProperty<string>(urbanLateMorning!, "DayPart"));
        Assert.Equal("morning_peak", GetProperty<string>(scenicMorningPeak!, "DayPart"));

        Assert.True(GetProperty<int>(urbanMorningPeak, "TravelMinutes") > GetProperty<int>(urbanLateMorning, "TravelMinutes"));
        Assert.True(GetProperty<int>(urbanMorningPeak, "JunctionDelayMinutes") > GetProperty<int>(scenicMorningPeak, "JunctionDelayMinutes"));

        Assert.Equal(GetProperty<double>(urbanMorningPeak, "DistanceKm"), GetProperty<double>(urbanMorningPeakRepeat, "DistanceKm"), 6);
        Assert.Equal(GetProperty<int>(urbanMorningPeak, "DriveMinutes"), GetProperty<int>(urbanMorningPeakRepeat, "DriveMinutes"));
        Assert.Equal(GetProperty<int>(urbanMorningPeak, "TravelMinutes"), GetProperty<int>(urbanMorningPeakRepeat, "TravelMinutes"));
    }

    [Fact]
    public void RouteInsightService_ComputesUrbanLegDistanceTravelAndJourneyMetrics()
    {
        var tour = BuildTourWithRouting(
            new TourRouteStop
            {
                Id = "stop-1",
                Day = 1,
                Order = 1,
                Name = "Cho trung tam",
                Type = "market",
                VisitMinutes = 40,
                AttractionScore = 8,
                Coordinates = new GeoPoint { Lat = 0, Lng = 0 }
            },
            new TourRouteStop
            {
                Id = "stop-2",
                Day = 1,
                Order = 2,
                Name = "Bao tang tinh",
                Type = "museum",
                VisitMinutes = 30,
                AttractionScore = 6,
                Coordinates = new GeoPoint { Lat = 0, Lng = 0.1 }
            });

        var insight = BuildRouteInsight(tour);

        Assert.True(GetProperty<bool>(insight, "HasRouting"));
        Assert.Equal(1, GetProperty<int>(insight, "DayCount"));
        Assert.Equal(2, GetProperty<int>(insight, "StopCount"));
        Assert.Equal(70, GetProperty<int>(insight, "TotalVisitMinutes"));
        Assert.Equal(34, GetProperty<int>(insight, "TotalTravelMinutes"));
        Assert.Equal(104, GetProperty<int>(insight, "TotalJourneyMinutes"));

        var distance = GetProperty<double>(insight, "TotalDistanceKm");
        Assert.InRange(distance, 11.0, 11.2);

        var averageScore = GetNullableDouble(insight, "AverageAttractionScore");
        Assert.NotNull(averageScore);
        Assert.Equal(7.0, averageScore!.Value, 1);
    }

    [Fact]
    public void RouteInsightService_SkipsLeg_WhenCoordinatesMissing_AndAddsWarning()
    {
        var tour = BuildTourWithRouting(
            new TourRouteStop
            {
                Id = "stop-1",
                Day = 1,
                Order = 1,
                Name = "Diem don",
                Type = "meeting",
                VisitMinutes = 20,
                Coordinates = new GeoPoint { Lat = 10.5, Lng = 106.5 }
            },
            new TourRouteStop
            {
                Id = "stop-2",
                Day = 1,
                Order = 2,
                Name = "Diem tham quan",
                Type = "landmark",
                VisitMinutes = 45,
                Coordinates = new GeoPoint { Lat = null, Lng = null }
            });

        var insight = BuildRouteInsight(tour);

        Assert.Equal(0, GetProperty<int>(insight, "TotalTravelMinutes"));
        Assert.Equal(65, GetProperty<int>(insight, "TotalJourneyMinutes"));

        var warnings = GetProperty<IEnumerable<object>>(insight, "Warnings").ToList();
        Assert.NotEmpty(warnings);
        Assert.Contains(warnings, warning =>
            GetProperty<string>(warning, "Message").Contains("coordinates", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void RouteInsightService_TracksDeterministicClockAndTrafficMetadata_PerDay()
    {
        var tour = BuildTourWithRouting(
            new TourRouteStop
            {
                Id = "stop-1",
                Day = 1,
                Order = 1,
                Name = "Diem don trung tam",
                Type = "meeting",
                VisitMinutes = 20,
                Coordinates = new GeoPoint { Lat = 10.7769, Lng = 106.7009 }
            },
            new TourRouteStop
            {
                Id = "stop-2",
                Day = 1,
                Order = 2,
                Name = "Bao tang tinh",
                Type = "museum",
                VisitMinutes = 40,
                Coordinates = new GeoPoint { Lat = 10.7898, Lng = 106.6992 }
            },
            new TourRouteStop
            {
                Id = "stop-3",
                Day = 1,
                Order = 3,
                Name = "Pho di bo",
                Type = "city",
                VisitMinutes = 25,
                Coordinates = new GeoPoint { Lat = 10.8026, Lng = 106.7050 }
            });

        var insight = BuildRouteInsight(tour);
        var day = GetProperty<IEnumerable<object>>(insight, "Days").Single();
        var legs = GetProperty<IEnumerable<object>>(day, "Legs").ToList();

        Assert.Equal(2, legs.Count);

        var firstLeg = legs[0];
        var secondLeg = legs[1];

        Assert.Equal(8 * 60 + 20, GetProperty<int>(firstLeg, "DepartureMinuteOfDay"));
        Assert.Equal(
            GetProperty<int>(firstLeg, "DepartureMinuteOfDay") + GetProperty<int>(firstLeg, "TravelMinutes"),
            GetProperty<int>(firstLeg, "ArrivalMinuteOfDay"));
        Assert.Equal(
            GetProperty<int>(firstLeg, "ArrivalMinuteOfDay") + 40,
            GetProperty<int>(secondLeg, "DepartureMinuteOfDay"));

        Assert.Equal("morning_peak", GetProperty<string>(firstLeg, "DayPart"));
        Assert.Equal("late_morning", GetProperty<string>(secondLeg, "DayPart"));
        Assert.False(string.IsNullOrWhiteSpace(GetProperty<string>(firstLeg, "CongestionLevel")));
        Assert.False(string.IsNullOrWhiteSpace(GetProperty<string>(secondLeg, "CongestionLevel")));
    }

    [Fact]
    public void PublicTourRouteOverview_Build_ExposesTravelJourneyAndDistanceMetrics()
    {
        var tour = BuildTourWithRouting(
            new TourRouteStop
            {
                Id = "stop-1",
                Day = 1,
                Order = 1,
                Name = "Rung thong",
                Type = "forest",
                VisitMinutes = 60,
                Coordinates = new GeoPoint { Lat = 11.95, Lng = 108.44 }
            },
            new TourRouteStop
            {
                Id = "stop-2",
                Day = 1,
                Order = 2,
                Name = "Ho tuyen lam",
                Type = "lake",
                VisitMinutes = 50,
                Coordinates = new GeoPoint { Lat = 11.92, Lng = 108.40 }
            });

        var overview = PublicTourRouteOverviewBuilder.Build(tour);
        var overviewType = overview.GetType();

        Assert.NotNull(overviewType.GetProperty("TotalTravelMinutes"));
        Assert.NotNull(overviewType.GetProperty("TotalJourneyMinutes"));
        Assert.NotNull(overviewType.GetProperty("TotalDistanceKm"));

        Assert.True((bool)(overviewType.GetProperty("HasRouting")?.GetValue(overview) ?? false));
        Assert.True((int)(overviewType.GetProperty("TotalTravelMinutes")?.GetValue(overview) ?? 0) > 0);
        Assert.True((int)(overviewType.GetProperty("TotalJourneyMinutes")?.GetValue(overview) ?? 0) > (int)(overviewType.GetProperty("TotalVisitMinutes")?.GetValue(overview) ?? 0));

        var firstDay = ((IEnumerable<object>)(overviewType.GetProperty("Days")?.GetValue(overview) ?? Array.Empty<object>())).First();
        Assert.NotNull(firstDay.GetType().GetProperty("TravelMinutes"));
        Assert.NotNull(firstDay.GetType().GetProperty("JourneyMinutes"));
    }

    [Fact]
    public void PublicTourRouteOverview_Build_ExposesTrafficAwareTransferMetadata_WithoutCoordinates()
    {
        var tour = BuildTourWithRouting(
            new TourRouteStop
            {
                Id = "stop-1",
                Day = 1,
                Order = 1,
                Name = "Ben xe trung tam",
                Type = "meeting",
                VisitMinutes = 25,
                Coordinates = new GeoPoint { Lat = 10.7769, Lng = 106.7009 }
            },
            new TourRouteStop
            {
                Id = "stop-2",
                Day = 1,
                Order = 2,
                Name = "Cho lon",
                Type = "market",
                VisitMinutes = 30,
                Coordinates = new GeoPoint { Lat = 10.7898, Lng = 106.6992 }
            });

        var overview = PublicTourRouteOverviewBuilder.Build(tour);
        var day = overview.Days.Single();
        var secondStop = day.Stops.Last();

        Assert.NotNull(secondStop.GetType().GetProperty("TransferFromPrevious"));

        var transfer = secondStop.GetType().GetProperty("TransferFromPrevious")?.GetValue(secondStop);
        Assert.NotNull(transfer);

        Assert.True(GetProperty<int>(transfer!, "TravelMinutes") > 0);
        Assert.False(string.IsNullOrWhiteSpace(GetProperty<string>(transfer, "DayPart")));
        Assert.False(string.IsNullOrWhiteSpace(GetProperty<string>(transfer, "CongestionLevel")));
        Assert.Null(transfer.GetType().GetProperty("Lat"));
        Assert.Null(transfer.GetType().GetProperty("Lng"));
    }

    [Fact]
    public void TourAiChatService_Snapshot_IncludesRouteSummary_ButNotRawCoordinates()
    {
        var method = typeof(TourAiChatService).GetMethod("BuildTourSnapshot", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        var tour = BuildTourWithRouting(
            new TourRouteStop
            {
                Id = "stop-1",
                Day = 1,
                Order = 1,
                Name = "Twin mountains",
                Type = "viewpoint",
                VisitMinutes = 35,
                Coordinates = new GeoPoint { Lat = 23.0541, Lng = 104.9834 }
            },
            new TourRouteStop
            {
                Id = "stop-2",
                Day = 1,
                Order = 2,
                Name = "Pho co dong van",
                Type = "city",
                VisitMinutes = 80,
                Coordinates = new GeoPoint { Lat = 23.278, Lng = 105.361 }
            });

        var snapshot = method!.Invoke(null, [tour])?.ToString();

        Assert.False(string.IsNullOrWhiteSpace(snapshot));
        Assert.True(snapshot!.Contains("lộ trình", StringComparison.OrdinalIgnoreCase));
        Assert.True(snapshot.Contains("di chuyển", StringComparison.OrdinalIgnoreCase));
        Assert.True(
            snapshot.Contains("cao Ä‘iá»ƒm", StringComparison.OrdinalIgnoreCase)
            || snapshot.Contains("khung giá»", StringComparison.OrdinalIgnoreCase)
            || snapshot.Contains("Peak traffic", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain("104.9834", snapshot, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("105.361", snapshot, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PublicContentDefaults_RoutingSection_RegistersTravelMetricFields()
    {
        var source = TestPaths.ReadRepoFile("HV-Travel.Web", "Services", "PublicContentDefaults.cs");

        Assert.True(source.Contains("travelMinutesFormat", StringComparison.Ordinal));
        Assert.True(source.Contains("journeyMinutesFormat", StringComparison.Ordinal));
        Assert.True(source.Contains("distanceFormat", StringComparison.Ordinal));
        Assert.True(source.Contains("dayTravelMinutesFormat", StringComparison.Ordinal));
        Assert.True(source.Contains("dayJourneyMinutesFormat", StringComparison.Ordinal));
        Assert.True(source.Contains("transferTimeFormat", StringComparison.Ordinal));
        Assert.True(source.Contains("dayPartLabel", StringComparison.Ordinal));
        Assert.True(source.Contains("congestionLabel", StringComparison.Ordinal));
        Assert.True(source.Contains("junctionDelayLabel", StringComparison.Ordinal));
    }

    [Fact]
    public void AdminTourDetails_Source_ContainsRouteIntelligenceSummary()
    {
        var source = TestPaths.ReadRepoFile("HV-Travel.Web", "Areas", "Admin", "Views", "Tours", "Details.cshtml");

        Assert.True(source.Contains("Phân tích lộ trình", StringComparison.OrdinalIgnoreCase));
        Assert.True(source.Contains("Cảnh báo", StringComparison.OrdinalIgnoreCase));
        Assert.True(source.Contains("Trễ giao lộ", StringComparison.OrdinalIgnoreCase));
        Assert.True(source.Contains("Mật độ", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void PublicTourDetails_Source_ContainsTrafficAwareTransferMarkup()
    {
        var source = TestPaths.ReadRepoFile("HV-Travel.Web", "Views", "PublicTours", "Details.cshtml");

        Assert.True(source.Contains("transferTimeFormat", StringComparison.Ordinal));
        Assert.True(source.Contains("dayPartLabel", StringComparison.Ordinal));
        Assert.True(source.Contains("congestionLabel", StringComparison.Ordinal));
    }

    private static object BuildRouteInsight(Tour tour)
    {
        var serviceType = typeof(HVTravel.Application.Services.PricingService).Assembly.GetType("HVTravel.Application.Services.RouteInsightService");
        Assert.NotNull(serviceType);

        var service = Activator.CreateInstance(serviceType!);
        Assert.NotNull(service);

        var buildMethod = serviceType!.GetMethod("Build", BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(buildMethod);

        var insight = buildMethod!.Invoke(service, [tour]);
        Assert.NotNull(insight);
        return insight!;
    }

    private static T GetProperty<T>(object target, string propertyName)
    {
        var property = target.GetType().GetProperty(propertyName);
        Assert.NotNull(property);
        return (T)property!.GetValue(target)!;
    }

    private static double? GetNullableDouble(object target, string propertyName)
    {
        var property = target.GetType().GetProperty(propertyName);
        Assert.NotNull(property);
        return property!.GetValue(target) as double? ?? (double?)property.GetValue(target);
    }

    private static Tour BuildTourWithRouting(params TourRouteStop[] stops)
    {
        return new Tour
        {
            Id = "tour-1",
            Slug = "route-intelligence-tour",
            Code = "T-INT-01",
            Name = "Route Intelligence Tour",
            Description = "Mo ta tour",
            ShortDescription = "Mo ta ngan",
            Destination = new Destination
            {
                City = "Da Lat",
                Country = "Viet Nam",
                Region = "Tay Nguyen"
            },
            Price = new TourPrice
            {
                Adult = 1200000m,
                Child = 850000m,
                Infant = 250000m,
                Currency = "VND"
            },
            Duration = new TourDuration
            {
                Days = 2,
                Nights = 1,
                Text = "2 ngay 1 dem"
            },
            Status = "Active",
            ConfirmationType = "Instant",
            Schedule =
            [
                new ScheduleItem { Day = 1, Title = "Ngay 1", Description = "Khoi hanh" },
                new ScheduleItem { Day = 2, Title = "Ngay 2", Description = "Tro ve" }
            ],
            Routing = new TourRouting
            {
                SchemaVersion = 1,
                Stops = stops.ToList()
            }
        };
    }
}

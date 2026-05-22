using System.Linq.Expressions;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using HVTravel.Domain.Entities;
using HVTravel.Domain.Interfaces;
using HVTravel.Domain.Models;
using HVTravel.Web.Areas.Admin.Controllers;

namespace HV_Travel.Web.Tests;

public class AdminToursSchemaSafetyTests
{
    private static readonly string RepoRoot = GetRepoRoot();

    [Fact]
    public async Task Edit_PreservesSchemaFieldsThatAreNotEditableInTheCurrentForm()
    {
        var existingTour = BuildSchemaRichTour();
        var postedTour = new Tour
        {
            Id = existingTour.Id,
            Version = existingTour.Version,
            Code = "TOUR-EDITED",
            Name = "Edited Tour Name",
            Description = "<p>Updated description</p>",
            ShortDescription = "<p>Updated short description</p>",
            Destination = new Destination
            {
                City = "Da Lat",
                Country = "Vietnam",
                Region = "Central Highlands"
            },
            Images = new List<string> { "https://cdn.test/updated.jpg" },
            Price = new TourPrice
            {
                Adult = 4200000m,
                Child = 3100000m,
                Infant = 520000m,
                Currency = "VND",
                Discount = 0
            },
            Duration = new TourDuration
            {
                Days = 4,
                Nights = 3,
                Text = "4 Days 3 Nights"
            },
            StartDates = new List<DateTime> { new(2026, 4, 22, 0, 0, 0, DateTimeKind.Utc) },
            Schedule = new List<ScheduleItem>
            {
                new()
                {
                    Day = 1,
                    Title = "Updated day 1",
                    Description = "<p>Updated schedule description</p>",
                    Activities = new List<string> { "Updated stop" }
                }
            },
            GeneratedInclusions = new List<string> { "Updated inclusion" },
            GeneratedExclusions = new List<string> { "Updated exclusion" },
            MaxParticipants = 28,
            CurrentParticipants = 9,
            Rating = 4.9,
            ReviewCount = 77,
            Status = "Active"
        };

        var repository = new RecordingTourRepository(existingTour);
        var controller = new ToursController(repository)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        var result = await controller.Edit(existingTour.Id, postedTour, null);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);

        var savedTour = Assert.Single(repository.UpdatedEntities);
        Assert.Same(existingTour, savedTour);

        Assert.Equal("Edited Tour Name", savedTour.Name);
        Assert.Equal("TOUR-EDITED", savedTour.Code);
        Assert.Equal("Da Lat", savedTour.Destination.City);
        Assert.Equal(28, savedTour.MaxParticipants);
        Assert.Equal(77, savedTour.ReviewCount);

        Assert.Equal("ha-giang-loop-explorer-00001", savedTour.Slug);
        Assert.Equal("SEO Title", savedTour.Seo.Title);
        Assert.Equal("Free cancellation up to 48 hours before departure.", savedTour.CancellationPolicy.Summary);
        Assert.Equal("Instant", savedTour.ConfirmationType);
        Assert.Equal(new[] { "Twin Mountains lookout", "Ma Pi Leng pass stop" }, savedTour.Highlights);
        Assert.Equal("HV Travel office, 18 Nguyen Trai, Ha Giang City.", savedTour.MeetingPoint);
        Assert.Equal(new[] { "deal", "route-ready" }, savedTour.BadgeSet);
        Assert.Equal("SUP-001", savedTour.SupplierRef.SupplierCode);
        Assert.Single(savedTour.Departures);
        Assert.Equal("dep-001", savedTour.Departures[0].Id);
        Assert.NotNull(savedTour.Routing);
        Assert.Equal(1, savedTour.Routing!.SchemaVersion);
        Assert.Equal("Twin Mountains lookout", savedTour.Routing.Stops[0].Name);
        Assert.Equal(23.0324, savedTour.Routing.Stops[0].Coordinates.Lat);
    }

    [Fact]
    public async Task Create_FillsSlugAndInstantConfirmationWhenCurrentFormLeavesThemBlank()
    {
        var repository = new RecordingTourRepository();
        var controller = new ToursController(repository);
        var draftTour = new Tour
        {
            Id = "66112233445566778899aabb",
            Code = "TOUR-CREATE-01",
            Name = "Da Nang & Hoi An Discovery",
            Description = "<p>Created from admin form</p>",
            ShortDescription = "<p>Created short description</p>",
            ConfirmationType = string.Empty,
            Slug = string.Empty,
            Destination = new Destination
            {
                City = "Da Nang",
                Country = "Vietnam",
                Region = "Central"
            },
            Price = new TourPrice
            {
                Adult = 4800000m,
                Child = 3400000m,
                Infant = 600000m,
                Currency = "VND",
                Discount = 0
            },
            Duration = new TourDuration
            {
                Days = 4,
                Nights = 3,
                Text = "4 Days 3 Nights"
            }
        };

        var result = await controller.Create(draftTour, null);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);

        var savedTour = Assert.Single(repository.AddedEntities);
        Assert.Equal("da-nang-hoi-an-discovery", savedTour.Slug);
        Assert.Equal("Instant", savedTour.ConfirmationType);
        Assert.Equal("Draft", savedTour.Status);
    }

    [Fact]
    public async Task Import_AcceptsMongoExtendedJsonAndKeepsRoutingAndDepartures()
    {
        var repository = new RecordingTourRepository();
        var controller = new ToursController(repository);
        var file = BuildFormFile(
            """
            [
              {
                "_id": { "$oid": "66112233445566778899aacc" },
                "code": "TOUR-ROUTE-01",
                "name": "Ha Giang Loop Explorer",
                "slug": "ha-giang-loop-explorer-00001",
                "description": "<p>Mountain passes and compact route.</p>",
                "shortDescription": "Mountain passes and compact route.",
                "seo": {
                  "title": "SEO Title",
                  "description": "SEO Description",
                  "canonicalPath": "/PublicTours/ha-giang-loop-explorer-00001",
                  "openGraphImageUrl": "https://cdn.test/ha-giang.jpg"
                },
                "destination": {
                  "city": "Ha Giang",
                  "country": "Vietnam",
                  "region": "North"
                },
                "images": [
                  "https://cdn.test/ha-giang.jpg"
                ],
                "price": {
                  "adult": { "$numberDecimal": "3500000" },
                  "child": { "$numberDecimal": "2520000" },
                  "infant": { "$numberDecimal": "420000" },
                  "currency": "VND",
                  "discount": 0.0
                },
                "duration": {
                  "days": 3,
                  "nights": 2,
                  "text": "3 Days 2 Nights"
                },
                "startDates": [
                  { "$date": "2026-04-16T12:00:00.000Z" }
                ],
                "departures": [
                  {
                    "id": "dep-001",
                    "startDate": { "$date": "2026-04-16T12:00:00.000Z" },
                    "adultPrice": { "$numberDecimal": "3500000" },
                    "childPrice": { "$numberDecimal": "2520000" },
                    "infantPrice": { "$numberDecimal": "420000" },
                    "discountPercentage": { "$numberDecimal": "0" },
                    "capacity": 14,
                    "bookedCount": 3,
                    "confirmationType": "Instant",
                    "status": "Scheduled",
                    "cutoffHours": 24
                  }
                ],
                "schedule": [
                  {
                    "day": 1,
                    "title": "Ha Giang - Quan Ba - Yen Minh",
                    "description": "Route overview",
                    "activities": [ "Twin Mountains lookout" ]
                  }
                ],
                "generatedInclusions": [ "Sleeper bus" ],
                "generatedExclusions": [ "Personal drinks" ],
                "cancellationPolicy": {
                  "summary": "Free cancellation up to 48 hours before departure.",
                  "isFreeCancellation": true,
                  "freeCancellationBeforeHours": 48
                },
                "confirmationType": "Instant",
                "highlights": [ "Twin Mountains lookout" ],
                "meetingPoint": "HV Travel office, 18 Nguyen Trai, Ha Giang City.",
                "badgeSet": [ "route-ready" ],
                "routing": {
                  "schemaVersion": 1,
                  "stops": [
                    {
                      "id": "hg-day1-stop1",
                      "day": 1,
                      "order": 1,
                      "name": "Twin Mountains lookout",
                      "type": "viewpoint",
                      "coordinates": { "lat": 23.0324, "lng": 104.8856 },
                      "visitMinutes": 35,
                      "attractionScore": 8.4,
                      "note": "Photo stop with a short uphill walk."
                    }
                  ]
                },
                "maxParticipants": 14,
                "currentParticipants": 3,
                "rating": 4.8,
                "reviewCount": 128,
                "createdAt": { "$date": "2026-04-01T00:00:00.000Z" },
                "updatedAt": { "$date": "2026-04-02T00:00:00.000Z" },
                "version": 7,
                "status": "Active"
              }
            ]
            """,
            "Tours.json");

        var result = await controller.Import(file);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);

        var savedTour = Assert.Single(repository.AddedEntities);
        Assert.NotEqual("66112233445566778899aacc", savedTour.Id);
        Assert.Equal("ha-giang-loop-explorer-00001", savedTour.Slug);
        Assert.Equal("SEO Title", savedTour.Seo.Title);
        Assert.Single(savedTour.Departures);
        Assert.Equal("dep-001", savedTour.Departures[0].Id);
        Assert.NotNull(savedTour.Routing);
        Assert.Equal("Twin Mountains lookout", savedTour.Routing!.Stops[0].Name);
    }

    [Fact]
    public async Task Import_FallsBackToPlainJsonPayload()
    {
        var repository = new RecordingTourRepository();
        var controller = new ToursController(repository);
        var file = BuildFormFile(
            """
            [
              {
                "Id": "plain-json-id",
                "Code": "TOUR-PLAIN-01",
                "Name": "Plain Json Tour",
                "Slug": "plain-json-tour",
                "Description": "<p>Plain JSON import</p>",
                "ShortDescription": "Plain JSON import",
                "ConfirmationType": "Manual",
                "Destination": {
                  "City": "Hue",
                  "Country": "Vietnam",
                  "Region": "Central"
                },
                "Price": {
                  "Adult": 2100000,
                  "Child": 1600000,
                  "Infant": 250000,
                  "Currency": "VND",
                  "Discount": 0
                },
                "Duration": {
                  "Days": 2,
                  "Nights": 1,
                  "Text": "2 Days 1 Night"
                },
                "StartDates": [
                  "2026-05-01T00:00:00Z"
                ],
                "Status": "Draft"
              }
            ]
            """,
            "Tours-plain.json");

        var result = await controller.Import(file);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);

        var savedTour = Assert.Single(repository.AddedEntities);
        Assert.Equal("plain-json-tour", savedTour.Slug);
        Assert.Equal("Manual", savedTour.ConfirmationType);
        Assert.Equal("Plain Json Tour", savedTour.Name);
    }

    [Fact]
    public void ToursDetails_View_ContainsReadOnlySchemaBlocksForPhase11()
    {
        var markup = ReadAdminToursView("Details.cshtml");

        Assert.Contains("Model.Slug", markup, StringComparison.Ordinal);
        Assert.Contains("Model.Seo", markup, StringComparison.Ordinal);
        Assert.Contains("Model.ConfirmationType", markup, StringComparison.Ordinal);
        Assert.Contains("Model.CancellationPolicy", markup, StringComparison.Ordinal);
        Assert.Contains("Model.Highlights", markup, StringComparison.Ordinal);
        Assert.Contains("Model.MeetingPoint", markup, StringComparison.Ordinal);
        Assert.Contains("Model.BadgeSet", markup, StringComparison.Ordinal);
        Assert.Contains("Model.Departures", markup, StringComparison.Ordinal);
        Assert.Contains("Đang dùng fallback từ StartDates", markup, StringComparison.Ordinal);
        Assert.Contains("Model.Routing", markup, StringComparison.Ordinal);
        Assert.Contains("Chưa có dữ liệu routing", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void ToursIndex_View_ContainsRoutingAndDepartureIndicators()
    {
        var markup = ReadAdminToursView("Index.cshtml");

        Assert.Contains("tour.Routing", markup, StringComparison.Ordinal);
        Assert.Contains("tour.Departures", markup, StringComparison.Ordinal);
    }

    private static IFormFile BuildFormFile(string content, string fileName)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, bytes.Length, "file", fileName);
    }

    private static string ReadAdminToursView(string fileName)
    {
        var viewPath = Path.Combine(RepoRoot, "HV-Travel.Web", "Areas", "Admin", "Views", "Tours", fileName);
        return File.ReadAllText(viewPath);
    }

    private static Tour BuildSchemaRichTour()
    {
        return new Tour
        {
            Id = "66112233445566778899aabb",
            Code = "TOUR-EXISTING-01",
            Name = "Ha Giang Loop Explorer",
            Slug = "ha-giang-loop-explorer-00001",
            Description = "<p>Existing description</p>",
            ShortDescription = "<p>Existing short description</p>",
            Seo = new SeoMetadata
            {
                Title = "SEO Title",
                Description = "SEO Description",
                CanonicalPath = "/PublicTours/ha-giang-loop-explorer-00001",
                OpenGraphImageUrl = "https://cdn.test/seo.jpg"
            },
            Destination = new Destination
            {
                City = "Ha Giang",
                Country = "Vietnam",
                Region = "North"
            },
            Images = new List<string> { "https://cdn.test/original.jpg" },
            Price = new TourPrice
            {
                Adult = 3500000m,
                Child = 2520000m,
                Infant = 420000m,
                Currency = "VND",
                Discount = 5
            },
            Duration = new TourDuration
            {
                Days = 3,
                Nights = 2,
                Text = "3 Days 2 Nights"
            },
            StartDates = new List<DateTime> { new(2026, 4, 16, 0, 0, 0, DateTimeKind.Utc) },
            Schedule = new List<ScheduleItem>
            {
                new()
                {
                    Day = 1,
                    Title = "Original day 1",
                    Description = "<p>Original schedule description</p>",
                    Activities = new List<string> { "Twin Mountains lookout" }
                }
            },
            Highlights = new List<string> { "Twin Mountains lookout", "Ma Pi Leng pass stop" },
            GeneratedInclusions = new List<string> { "Sleeper bus" },
            GeneratedExclusions = new List<string> { "Personal drinks" },
            CancellationPolicy = new TourCancellationPolicy
            {
                Summary = "Free cancellation up to 48 hours before departure.",
                IsFreeCancellation = true,
                FreeCancellationBeforeHours = 48
            },
            ConfirmationType = "Instant",
            MeetingPoint = "HV Travel office, 18 Nguyen Trai, Ha Giang City.",
            SupplierRef = new SupplierReference
            {
                SupplierId = "sup-1",
                SupplierCode = "SUP-001",
                SupplierName = "HV Supplier",
                SourceSystem = "MockData"
            },
            BadgeSet = new List<string> { "deal", "route-ready" },
            Departures = new List<TourDeparture>
            {
                new()
                {
                    Id = "dep-001",
                    StartDate = new DateTime(2026, 4, 16, 12, 0, 0, DateTimeKind.Utc),
                    AdultPrice = 3500000m,
                    ChildPrice = 2520000m,
                    InfantPrice = 420000m,
                    DiscountPercentage = 0,
                    Capacity = 14,
                    BookedCount = 3,
                    ConfirmationType = "Instant",
                    Status = "Scheduled",
                    CutoffHours = 24
                }
            },
            Routing = new TourRouting
            {
                SchemaVersion = 1,
                Stops = new List<TourRouteStop>
                {
                    new()
                    {
                        Id = "hg-day1-stop1",
                        Day = 1,
                        Order = 1,
                        Name = "Twin Mountains lookout",
                        Type = "viewpoint",
                        Coordinates = new GeoPoint
                        {
                            Lat = 23.0324,
                            Lng = 104.8856
                        },
                        VisitMinutes = 35,
                        AttractionScore = 8.4,
                        Note = "Photo stop with a short uphill walk."
                    }
                }
            },
            MaxParticipants = 14,
            CurrentParticipants = 3,
            Rating = 4.8,
            ReviewCount = 128,
            CreatedAt = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2026, 4, 2, 0, 0, 0, DateTimeKind.Utc),
            Version = 7,
            Status = "Draft"
        };
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

        throw new DirectoryNotFoundException("Could not find repository root from test output directory.");
    }

    private sealed class RecordingTourRepository : IRepository<Tour>
    {
        private readonly Dictionary<string, Tour> _byId = new(StringComparer.Ordinal);

        public RecordingTourRepository(params Tour[] existingTours)
        {
            foreach (var tour in existingTours)
            {
                _byId[tour.Id] = tour;
            }
        }

        public List<Tour> AddedEntities { get; } = new();

        public List<Tour> UpdatedEntities { get; } = new();

        public Task<IEnumerable<Tour>> GetAllAsync()
        {
            return Task.FromResult<IEnumerable<Tour>>(_byId.Values.ToList());
        }

        public Task<Tour> GetByIdAsync(string id)
        {
            _byId.TryGetValue(id, out var tour);
            return Task.FromResult(tour)!;
        }

        public Task<IEnumerable<Tour>> FindAsync(Expression<Func<Tour, bool>> predicate)
        {
            var compiled = predicate.Compile();
            return Task.FromResult<IEnumerable<Tour>>(_byId.Values.Where(compiled).ToList());
        }

        public Task AddAsync(Tour entity)
        {
            AddedEntities.Add(entity);
            _byId[entity.Id] = entity;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(string id, Tour entity)
        {
            UpdatedEntities.Add(entity);
            _byId[id] = entity;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(string id)
        {
            _byId.Remove(id);
            return Task.CompletedTask;
        }

        public Task<PaginatedResult<Tour>> GetPagedAsync(int pageIndex, int pageSize, Expression<Func<Tour, bool>>? filter = null)
        {
            IEnumerable<Tour> tours = _byId.Values;
            if (filter != null)
            {
                tours = tours.Where(filter.Compile());
            }

            var items = tours.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToList();
            return Task.FromResult(new PaginatedResult<Tour>(items, tours.Count(), pageIndex, pageSize));
        }
    }
}

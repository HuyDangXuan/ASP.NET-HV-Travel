using System.Text;
using HVTravel.Domain.Entities;
using HVTravel.Web.Areas.Admin.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace HVTravel.Web.Tests;

public class AdminToursParityTests
{
    [Fact]
    public async Task Create_AssignsSlugAndDefaultConfirmationType_WhenMissing()
    {
        var repo = new RecordingTourRepository();
        var controller = new ToursController(repo);
        var tour = BuildPostedTour();

        var result = await controller.Create(tour, "Draft");

        Assert.IsType<RedirectToActionResult>(result);
        Assert.NotNull(repo.LastAdded);
        Assert.Equal("kham-pha-thai-binh", repo.LastAdded!.Slug);
        Assert.Equal("Instant", repo.LastAdded.ConfirmationType);
    }

    [Fact]
    public async Task Edit_PreservesSchemaFieldsThatLegacyFormDoesNotOwn()
    {
        var repo = new RecordingTourRepository();
        var existing = BuildPostedTour();
        existing.Id = "tour-1";
        existing.Slug = "slug-cu";
        existing.Seo = new SeoMetadata { Title = "SEO title" };
        existing.Highlights = ["Điểm nổi bật"];
        existing.MeetingPoint = "Nhà hát lớn";
        existing.BadgeSet = ["deal"];
        existing.Departures =
        [
            new TourDeparture
            {
                Id = "dep-1",
                StartDate = new DateTime(2026, 5, 1),
                AdultPrice = 1200000m,
                Capacity = 10,
                BookedCount = 2
            }
        ];

        repo.Seed(existing);

        var posted = BuildPostedTour();
        posted.Id = existing.Id;
        posted.Name = "Tên mới";
        posted.Description = "Mô tả mới";
        posted.ShortDescription = "Mô tả ngắn mới";

        var result = await controller(repo).Edit(existing.Id, posted, "Draft");

        Assert.IsType<RedirectToActionResult>(result);
        Assert.NotNull(repo.LastUpdated);
        Assert.Equal("slug-cu", repo.LastUpdated!.Slug);
        Assert.Equal("SEO title", repo.LastUpdated.Seo.Title);
        Assert.Equal("Nhà hát lớn", repo.LastUpdated.MeetingPoint);
        Assert.Single(repo.LastUpdated.Departures);
    }

    [Fact]
    public async Task Edit_ReturnsView_WhenDepartureBookedCountExceedsCapacity()
    {
        var repo = new RecordingTourRepository();
        var existing = BuildPostedTour();
        existing.Id = "tour-2";
        repo.Seed(existing);
        var adminController = controller(repo);

        var posted = BuildPostedTour();
        posted.Id = existing.Id;
        posted.Departures =
        [
            new TourDeparture
            {
                StartDate = new DateTime(2026, 5, 10),
                AdultPrice = 1000000m,
                Capacity = 2,
                BookedCount = 3
            }
        ];

        var result = await adminController.Edit(existing.Id, posted, "Draft");

        var view = Assert.IsType<ViewResult>(result);
        Assert.False(adminController.ModelState.IsValid);
        Assert.Null(repo.LastUpdated);
        Assert.Same(posted, view.Model);
    }

    [Fact]
    public async Task Import_AcceptsExtendedJson_WithDeparturesAndRouting()
    {
        var repo = new RecordingTourRepository();
        var adminController = controller(repo);
        var content = """
[
  {
    "_id": { "$oid": "507f1f77bcf86cd799439011" },
    "code": "TB-001",
    "name": "Khám phá Thái Bình",
    "description": "Mô tả",
    "shortDescription": "Mô tả ngắn",
    "destination": { "city": "Thái Bình", "country": "Việt Nam", "region": "Miền Bắc" },
    "images": ["https://example.test/tour.jpg"],
    "price": { "adult": { "$numberDecimal": "1000000" }, "child": { "$numberDecimal": "800000" }, "infant": { "$numberDecimal": "200000" }, "currency": "VND", "discount": 0 },
    "duration": { "days": 2, "nights": 1, "text": "2N1Đ" },
    "startDates": [{ "$date": "2026-05-01T00:00:00Z" }],
    "schedule": [{ "day": 1, "title": "Ngày 1", "description": "Đi chơi", "activities": [] }],
    "generatedInclusions": [],
    "generatedExclusions": [],
    "maxParticipants": 10,
    "currentParticipants": 1,
    "rating": 4.8,
    "reviewCount": 12,
    "createdAt": { "$date": "2026-04-01T00:00:00Z" },
    "updatedAt": { "$date": "2026-04-02T00:00:00Z" },
    "version": 0,
    "status": "Active",
    "slug": "kham-pha-thai-binh",
    "confirmationType": "Instant",
    "departures": [
      {
        "id": "dep-1",
        "startDate": { "$date": "2026-05-01T00:00:00Z" },
        "adultPrice": { "$numberDecimal": "1000000" },
        "childPrice": { "$numberDecimal": "800000" },
        "infantPrice": { "$numberDecimal": "200000" },
        "discountPercentage": { "$numberDecimal": "0" },
        "capacity": 10,
        "bookedCount": 2,
        "confirmationType": "Instant",
        "status": "Scheduled",
        "cutoffHours": 24
      }
    ],
    "routing": {
      "schemaVersion": 1,
      "stops": [
        {
          "id": "stop-1",
          "day": 1,
          "order": 1,
          "name": "Đền Trần",
          "type": "landmark",
          "coordinates": { "lat": 20.45, "lng": 106.34 },
          "visitMinutes": 45,
          "attractionScore": 8.5,
          "note": "Tham quan"
        }
      ]
    }
  }
]
""";

        await adminController.Import(CreateFormFile("Tours.json", content));

        Assert.NotNull(repo.LastAdded);
        Assert.Equal("kham-pha-thai-binh", repo.LastAdded!.Slug);
        Assert.Single(repo.LastAdded.Departures);
        var routingProperty = typeof(Tour).GetProperty("Routing");
        Assert.NotNull(routingProperty);
        Assert.NotNull(routingProperty!.GetValue(repo.LastAdded));
    }

    [Fact]
    public void AdminViews_ContainDeparturesAndRoutingEditors()
    {
        var createView = TestPaths.ReadRepoFile("HV-Travel.Web", "Areas", "Admin", "Views", "Tours", "Create.cshtml");
        var editView = TestPaths.ReadRepoFile("HV-Travel.Web", "Areas", "Admin", "Views", "Tours", "Edit.cshtml");
        var detailsView = TestPaths.ReadRepoFile("HV-Travel.Web", "Areas", "Admin", "Views", "Tours", "Details.cshtml");
        var indexView = TestPaths.ReadRepoFile("HV-Travel.Web", "Areas", "Admin", "Views", "Tours", "Index.cshtml");

        Assert.Contains("Departures", createView, StringComparison.Ordinal);
        Assert.Contains("Routing", createView, StringComparison.Ordinal);
        Assert.Contains("Departures", editView, StringComparison.Ordinal);
        Assert.Contains("Routing", editView, StringComparison.Ordinal);
        Assert.Contains("Routing", detailsView, StringComparison.Ordinal);
        Assert.Contains("Departures", detailsView, StringComparison.Ordinal);
        Assert.Contains("Routing", indexView, StringComparison.Ordinal);
    }

    private static ToursController controller(RecordingTourRepository repo)
    {
        return new ToursController(repo)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }

    private static IFormFile CreateFormFile(string fileName, string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, bytes.Length, "file", fileName);
    }

    private static Tour BuildPostedTour()
    {
        return new Tour
        {
            Id = "temp-id",
            Code = "TB-001",
            Name = "Khám phá Thái Bình",
            Description = "Mô tả",
            ShortDescription = "Mô tả ngắn",
            Destination = new Destination { City = "Thái Bình", Country = "Việt Nam", Region = "Miền Bắc" },
            Images = ["https://example.test/tour.jpg"],
            Price = new TourPrice { Adult = 1000000m, Child = 800000m, Infant = 200000m },
            Duration = new TourDuration { Days = 2, Nights = 1, Text = "2N1Đ" },
            StartDates = [new DateTime(2026, 5, 1)],
            Schedule = [new ScheduleItem { Day = 1, Title = "Ngày 1", Description = "Đi chơi" }],
            GeneratedInclusions = ["Xe đưa đón"],
            GeneratedExclusions = ["Chi tiêu cá nhân"],
            MaxParticipants = 10,
            CurrentParticipants = 1,
            Rating = 4.5,
            ReviewCount = 10,
            Status = "Active"
        };
    }
}

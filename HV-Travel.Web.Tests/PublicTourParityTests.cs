using HVTravel.Domain.Entities;
using HVTravel.Web.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace HVTravel.Web.Tests;

public class PublicTourParityTests
{
    [Fact]
    public async Task Details_UsesSlugLookupDirectly_WhenIdentifierIsNotAValidObjectId()
    {
        var repo = new RecordingTourRepository();
        repo.Seed(BuildTour("tour-1", "kham-pha-thai-binh"));
        var controller = CreateController(repo);

        var result = await controller.Details("kham-pha-thai-binh");

        Assert.IsType<ViewResult>(result);
        Assert.Equal(0, repo.GetByIdCallCount);
        Assert.Equal(1, repo.GetBySlugCallCount);
    }

    [Fact]
    public async Task Details_FallsBackToSlugLookup_WhenObjectIdShapedIdentifierDoesNotMatchAnId()
    {
        const string objectIdShapedSlug = "507f1f77bcf86cd799439011";
        var repo = new RecordingTourRepository();
        repo.Seed(BuildTour("tour-2", objectIdShapedSlug));
        var controller = CreateController(repo);

        var result = await controller.Details(objectIdShapedSlug);

        Assert.IsType<ViewResult>(result);
        Assert.Equal(1, repo.GetByIdCallCount);
        Assert.Equal(1, repo.GetBySlugCallCount);
    }

    [Fact]
    public void Tour_ExposesOptionalRoutingProperty()
    {
        var property = typeof(Tour).GetProperty("Routing");

        Assert.NotNull(property);
    }

    [Fact]
    public void PublicTourDetails_Source_ContainsRoutingSection()
    {
        var source = TestPaths.ReadRepoFile("HV-Travel.Web", "Views", "PublicTours", "Details.cshtml");

        Assert.Contains("routing", source, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PublicTourLinks_UseSharedSlugFirstHelper()
    {
        var helperSource = TestPaths.ReadRepoFile("HV-Travel.Web", "Services", "PublicTourIdentifierHelper.cs");
        var tourCard = TestPaths.ReadRepoFile("HV-Travel.Web", "Views", "PublicTours", "_TourCard.cshtml");
        var carouselCard = TestPaths.ReadRepoFile("HV-Travel.Web", "Views", "Shared", "_CarouselTourCard.cshtml");
        var bookingCreate = TestPaths.ReadRepoFile("HV-Travel.Web", "Views", "Booking", "Create.cshtml");
        var destinationsIndex = TestPaths.ReadRepoFile("HV-Travel.Web", "Views", "Destinations", "Index.cshtml");

        Assert.Contains("GetDetailIdentifier", helperSource, StringComparison.Ordinal);
        Assert.Contains("PublicTourIdentifierHelper.GetDetailIdentifier", tourCard, StringComparison.Ordinal);
        Assert.Contains("PublicTourIdentifierHelper.GetDetailIdentifier", carouselCard, StringComparison.Ordinal);
        Assert.Contains("PublicTourIdentifierHelper.GetDetailIdentifier", bookingCreate, StringComparison.Ordinal);
        Assert.Contains("PublicTourIdentifierHelper.GetDetailIdentifier", destinationsIndex, StringComparison.Ordinal);
    }

    [Fact]
    public void PublicContentDefaults_RegistersRoutingSection()
    {
        var source = TestPaths.ReadRepoFile("HV-Travel.Web", "Services", "PublicContentDefaults.cs");

        Assert.Contains("\"routing\"", source, StringComparison.Ordinal);
    }

    private static PublicToursController CreateController(RecordingTourRepository repo)
    {
        var controller = new PublicToursController(repo)
        {
            Url = new FakeUrlHelper(),
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        controller.ControllerContext.HttpContext.Request.Scheme = "https";
        return controller;
    }

    private static Tour BuildTour(string id, string slug)
    {
        return new Tour
        {
            Id = id,
            Slug = slug,
            Code = "T-001",
            Name = "Khám phá Thái Bình",
            Description = "Mô tả",
            ShortDescription = "Mô tả ngắn",
            Destination = new Destination { City = "Thái Bình", Country = "Việt Nam", Region = "Miền Bắc" },
            Price = new TourPrice { Adult = 1000000m, Child = 800000m, Infant = 200000m },
            Duration = new TourDuration { Days = 2, Nights = 1, Text = "2N1Đ" },
            Status = "Active",
            Images = ["https://example.test/tour.jpg"]
        };
    }
}

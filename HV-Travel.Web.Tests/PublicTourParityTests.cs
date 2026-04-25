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
    public void PublicToursIndex_RendersTourGridAsTwoColumnsOnWideScreens()
    {
        var source = TestPaths.ReadRepoFile("HV-Travel.Web", "Views", "PublicTours", "Index.cshtml");

        Assert.Contains("sm:grid-cols-2", source, StringComparison.Ordinal);
        Assert.DoesNotContain("xl:grid-cols-3", source, StringComparison.Ordinal);
    }

    [Fact]
    public void PublicToursFilter_RendersInputsInSingleColumn()
    {
        var source = TestPaths.ReadRepoFile("HV-Travel.Web", "Views", "PublicTours", "_PublicToursFilter.cshtml");

        Assert.DoesNotContain("sm:grid-cols-2", source, StringComparison.Ordinal);
    }

    [Fact]
    public void PublicViews_LoadPublicSelectStylesAndMarkEverySelect()
    {
        var layoutSource = TestPaths.ReadRepoFile("HV-Travel.Web", "Views", "Shared", "_LayoutPublic.cshtml");
        var cssPath = Path.Combine(TestPaths.RepoRoot(), "HV-Travel.Web", "wwwroot", "css", "public-select.css");
        var scriptPath = Path.Combine(TestPaths.RepoRoot(), "HV-Travel.Web", "wwwroot", "js", "public-select.js");

        Assert.Contains("~/css/public-select.css", layoutSource, StringComparison.Ordinal);
        Assert.Contains("~/js/public-select.js", layoutSource, StringComparison.Ordinal);
        Assert.True(File.Exists(cssPath), "Public select stylesheet should exist.");
        Assert.True(File.Exists(scriptPath), "Public select script should exist.");

        var cssSource = File.ReadAllText(cssPath);
        Assert.Contains("select.public-select", cssSource, StringComparison.Ordinal);
        Assert.Contains("appearance: none", cssSource, StringComparison.Ordinal);
        Assert.Contains("background-image", cssSource, StringComparison.Ordinal);
        Assert.Contains("linear-gradient", cssSource, StringComparison.Ordinal);
        Assert.Contains("inset 0 0 0 1px", cssSource, StringComparison.Ordinal);
        Assert.Contains(".dark select.public-select", cssSource, StringComparison.Ordinal);
        Assert.Contains("select.public-select:disabled", cssSource, StringComparison.Ordinal);
        Assert.Contains("public-select-trigger", cssSource, StringComparison.Ordinal);
        Assert.Contains("public-select-panel", cssSource, StringComparison.Ordinal);
        Assert.Contains("public-select-option", cssSource, StringComparison.Ordinal);

        var scriptSource = File.ReadAllText(scriptPath);
        Assert.Contains("select.public-select", scriptSource, StringComparison.Ordinal);
        Assert.Contains("public-select-shell", scriptSource, StringComparison.Ordinal);
        Assert.Contains("public-select-trigger", scriptSource, StringComparison.Ordinal);
        Assert.Contains("public-select-panel", scriptSource, StringComparison.Ordinal);
        Assert.Contains("public-select-option", scriptSource, StringComparison.Ordinal);
        Assert.Contains("select.value", scriptSource, StringComparison.Ordinal);
        Assert.Contains("dispatchEvent(new Event('change'", scriptSource, StringComparison.Ordinal);

        AssertPublicSelectCoverage("HV-Travel.Web", "Views", "Home", "Contact.cshtml");
        AssertPublicSelectCoverage("HV-Travel.Web", "Views", "Services", "Index.cshtml");
        AssertPublicSelectCoverage("HV-Travel.Web", "Views", "TripPlanner", "Index.cshtml");
        AssertPublicSelectCoverage("HV-Travel.Web", "Views", "PublicTours", "Details.cshtml");
        AssertPublicSelectCoverage("HV-Travel.Web", "Views", "PublicTours", "_PublicToursFilter.cshtml");

        var filterSource = TestPaths.ReadRepoFile("HV-Travel.Web", "Views", "PublicTours", "_PublicToursFilter.cshtml");
        Assert.Contains("SelectControlClass", filterSource, StringComparison.Ordinal);
        Assert.Contains("public-select", filterSource, StringComparison.Ordinal);
    }

    [Fact]
    public void PublicFormControls_UseReadableClassesBesideCustomSelects()
    {
        var filterSource = TestPaths.ReadRepoFile("HV-Travel.Web", "Views", "PublicTours", "_PublicToursFilter.cshtml");

        Assert.Contains("InputControlClass", filterSource, StringComparison.Ordinal);
        Assert.Contains("SelectControlClass", filterSource, StringComparison.Ordinal);
        Assert.Contains("font-bold", filterSource, StringComparison.Ordinal);
        Assert.Contains("placeholder:text-slate-500", filterSource, StringComparison.Ordinal);
        Assert.Contains("focus:ring-4", filterSource, StringComparison.Ordinal);

        foreach (var inputId in new[] { "min-price", "max-price", "max-days", "travellers" })
        {
            var inputLine = FindLineContaining(filterSource, $"id=\"{inputId}\"");
            Assert.Contains("class=\"@InputControlClass\"", inputLine, StringComparison.Ordinal);
            Assert.DoesNotContain("@SelectControlClass", inputLine, StringComparison.Ordinal);
        }

        foreach (var selectId in new[] { "region-select", "destination-select", "departure-month-select", "route-style-select", "sort-select", "confirmation-type-select", "cancellation-type-select" })
        {
            var selectLine = FindLineContaining(filterSource, $"id=\"{selectId}\"");
            Assert.Contains("class=\"@SelectControlClass\"", selectLine, StringComparison.Ordinal);
        }

        var contactSource = TestPaths.ReadRepoFile("HV-Travel.Web", "Views", "Home", "Contact.cshtml");
        foreach (var token in new[] { "asp-for=\"FullName\"", "asp-for=\"PhoneNumber\"", "asp-for=\"Email\"", "asp-for=\"Message\"" })
        {
            AssertControlLineHasReadableWeight(contactSource, token);
        }

        var servicesSource = TestPaths.ReadRepoFile("HV-Travel.Web", "Views", "Services", "Index.cshtml");
        foreach (var token in new[]
        {
            "asp-for=\"Request.FullName\"",
            "asp-for=\"Request.Phone\"",
            "asp-for=\"Request.Email\"",
            "asp-for=\"Request.Destination\"",
            "asp-for=\"Request.DepartureDate\"",
            "asp-for=\"Request.ReturnDate\"",
            "asp-for=\"Request.TravellersCount\"",
            "asp-for=\"Request.BudgetText\"",
            "asp-for=\"Request.RequestNote\""
        })
        {
            AssertControlLineHasReadableWeight(servicesSource, token);
        }

        var tripPlannerSource = TestPaths.ReadRepoFile("HV-Travel.Web", "Views", "TripPlanner", "Index.cshtml");
        AssertControlLineHasReadableWeight(tripPlannerSource, "data-planner-travellers");
        AssertControlLineHasReadableWeight(tripPlannerSource, "data-planner-max-days");
    }

    [Fact]
    public void PublicTourCard_StacksPlannerButtonBelowWishlistButton()
    {
        var source = TestPaths.ReadRepoFile("HV-Travel.Web", "Views", "PublicTours", "_TourCard.cshtml");
        var actionsStart = source.IndexOf("data-tour-card-actions", StringComparison.Ordinal);
        var actionLine = FindLineContaining(source, "data-tour-card-actions");

        Assert.True(actionsStart >= 0, "Tour card should expose a scoped action container for overlay buttons.");

        var actionsEnd = source.IndexOf("</div>", actionsStart, StringComparison.Ordinal);
        Assert.True(actionsEnd > actionsStart, "Tour card action container should be closed after the action buttons.");

        var actionsMarkup = source.Substring(actionsStart, actionsEnd - actionsStart);
        Assert.Contains("flex-col", actionLine, StringComparison.Ordinal);
        Assert.Contains("order-1", actionsMarkup, StringComparison.Ordinal);
        Assert.Contains("data-tour-wishlist-toggle", actionsMarkup, StringComparison.Ordinal);
        Assert.Contains("order-2", actionsMarkup, StringComparison.Ordinal);
        Assert.Contains("data-tour-planner-toggle", actionsMarkup, StringComparison.Ordinal);
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

    private static void AssertPublicSelectCoverage(params string[] pathSegments)
    {
        var source = TestPaths.ReadRepoFile(pathSegments);
        var selectLines = source
            .Split(["\r\n", "\n"], StringSplitOptions.None)
            .Where(line => line.Contains("<select", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        Assert.NotEmpty(selectLines);
        foreach (var line in selectLines)
        {
            Assert.True(
                line.Contains("public-select", StringComparison.Ordinal) || line.Contains("@SelectControlClass", StringComparison.Ordinal),
                $"Public select line should use public-select styling: {line.Trim()}");
        }
    }

    private static void AssertControlLineHasReadableWeight(string source, string token)
    {
        var line = FindLineContaining(source, token);
        Assert.True(
            line.Contains("font-semibold", StringComparison.Ordinal) || line.Contains("font-bold", StringComparison.Ordinal),
            $"Public form control should use a readable font weight: {line.Trim()}");
    }

    private static string FindLineContaining(string source, string token)
    {
        var line = source
            .Split(["\r\n", "\n"], StringSplitOptions.None)
            .FirstOrDefault(line => line.Contains(token, StringComparison.Ordinal));

        Assert.False(string.IsNullOrWhiteSpace(line), $"Expected to find line containing {token}.");
        return line!;
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

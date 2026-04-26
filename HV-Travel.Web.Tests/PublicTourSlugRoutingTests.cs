using System.Linq.Expressions;
using HVTravel.Domain.Entities;
using HVTravel.Domain.Interfaces;
using HVTravel.Domain.Models;
using HVTravel.Web.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;

namespace HV_Travel.Web.Tests;

public class PublicTourSlugRoutingTests
{
    private static readonly string RepoRoot = GetRepoRoot();

    [Fact]
    public async Task Details_UsesSlugLookupDirectly_WhenIdentifierIsNotAValidObjectId()
    {
        var tour = BuildTour(id: "66112233445566778899aabb", slug: "kham-pha-thai-binh");
        var repository = new RecordingTourRepository
        {
            TourBySlug = tour
        };

        var controller = CreateController(repository);

        var result = await controller.Details("kham-pha-thai-binh");

        var view = Assert.IsType<ViewResult>(result);
        Assert.Same(tour, view.Model);
        Assert.Empty(repository.GetByIdCalls);
        Assert.Equal(new[] { "kham-pha-thai-binh" }, repository.GetBySlugCalls);
    }

    [Fact]
    public async Task Details_UsesIdLookupFirst_WhenIdentifierIsAValidObjectId()
    {
        var objectId = "66112233445566778899aabb";
        var tour = BuildTour(id: objectId, slug: "ha-giang-loop");
        var repository = new RecordingTourRepository
        {
            TourById = tour
        };

        var controller = CreateController(repository);

        var result = await controller.Details(objectId);

        var view = Assert.IsType<ViewResult>(result);
        Assert.Same(tour, view.Model);
        Assert.Equal(new[] { objectId }, repository.GetByIdCalls);
        Assert.Empty(repository.GetBySlugCalls);
    }

    [Fact]
    public async Task Details_FallsBackToSlugLookup_WhenObjectIdShapedIdentifierHasNoIdMatch()
    {
        var objectIdShapedSlug = "abcdefabcdefabcdefabcdef";
        var tour = BuildTour(id: "66112233445566778899aabb", slug: objectIdShapedSlug);
        var repository = new RecordingTourRepository
        {
            TourBySlug = tour
        };

        var controller = CreateController(repository);

        var result = await controller.Details(objectIdShapedSlug);

        var view = Assert.IsType<ViewResult>(result);
        Assert.Same(tour, view.Model);
        Assert.Equal(new[] { objectIdShapedSlug }, repository.GetByIdCalls);
        Assert.Equal(new[] { objectIdShapedSlug }, repository.GetBySlugCalls);
    }

    [Fact]
    public async Task Details_BuildsCanonicalUrl_FromSlugFirstIdentifier()
    {
        var tour = BuildTour(id: "66112233445566778899aabb", slug: "ha-giang-loop");
        var repository = new RecordingTourRepository
        {
            TourBySlug = tour
        };

        var controller = CreateController(repository);

        var result = await controller.Details("ha-giang-loop");

        _ = Assert.IsType<ViewResult>(result);
        Assert.Equal("https://example.test/PublicTours/Details/ha-giang-loop", controller.ViewData["CanonicalUrl"]);
    }

    [Fact]
    public void PublicTourIdentifierHelper_PrefersSlug_AndFallsBackToId()
    {
        var helperType = FindType("HVTravel.Web.Services.PublicTourIdentifierHelper");
        Assert.NotNull(helperType);

        var method = helperType!.GetMethod("GetDetailIdentifier");
        Assert.NotNull(method);

        var slugTour = BuildTour(id: "66112233445566778899aabb", slug: "da-nang-discovery");
        var idOnlyTour = BuildTour(id: "66112233445566778899aacc", slug: "");

        Assert.Equal("da-nang-discovery", method!.Invoke(null, new object[] { slugTour }) as string);
        Assert.Equal("66112233445566778899aacc", method.Invoke(null, new object[] { idOnlyTour }) as string);
    }

    [Fact]
    public void PublicTourLinks_UseSharedSlugFirstIdentifier()
    {
        var bookingCreate = ReadRepoFile("HV-Travel.Web", "Views", "Booking", "Create.cshtml");
        var tourCard = ReadRepoFile("HV-Travel.Web", "Views", "PublicTours", "_TourCard.cshtml");
        var carouselCard = ReadRepoFile("HV-Travel.Web", "Views", "Shared", "_CarouselTourCard.cshtml");
        var destinationsIndex = ReadRepoFile("HV-Travel.Web", "Views", "Destinations", "Index.cshtml");

        Assert.Contains("PublicTourIdentifierHelper.GetDetailIdentifier", bookingCreate, StringComparison.Ordinal);
        Assert.Contains("PublicTourIdentifierHelper.GetDetailIdentifier", tourCard, StringComparison.Ordinal);
        Assert.Contains("PublicTourIdentifierHelper.GetDetailIdentifier", carouselCard, StringComparison.Ordinal);
        Assert.Contains("PublicTourIdentifierHelper.GetDetailIdentifier", destinationsIndex, StringComparison.Ordinal);
        Assert.DoesNotContain("asp-route-id=\"@tour.Id\"", destinationsIndex, StringComparison.Ordinal);
    }

    private static PublicToursController CreateController(RecordingTourRepository repository)
    {
        return new PublicToursController(repository)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            },
            Url = new FakeUrlHelper()
        };
    }

    private static Tour BuildTour(string id, string slug)
    {
        return new Tour
        {
            Id = id,
            Slug = slug,
            Name = "Route Ready Tour",
            Status = "Active",
            Description = "Test description",
            ShortDescription = "Short description",
            Destination = new Destination
            {
                City = "Ha Giang",
                Country = "Vietnam",
                Region = "North"
            },
            Price = new TourPrice
            {
                Adult = 3200000m,
                Child = 2500000m,
                Infant = 500000m,
                Currency = "VND"
            },
            Duration = new TourDuration
            {
                Days = 3,
                Nights = 2,
                Text = "3 Days 2 Nights"
            },
            Images = new List<string> { "https://cdn.test/tour.jpg" },
            Seo = new SeoMetadata()
        };
    }

    private static Type? FindType(string fullName)
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .Select(assembly => assembly.GetType(fullName, throwOnError: false, ignoreCase: false))
            .FirstOrDefault(type => type != null);
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

    private sealed class FakeUrlHelper : IUrlHelper
    {
        public ActionContext ActionContext { get; } = new()
        {
            HttpContext = new DefaultHttpContext()
        };

        public string? Action(UrlActionContext actionContext)
        {
            var id = actionContext.Values?.GetType().GetProperty("id")?.GetValue(actionContext.Values)?.ToString();
            return $"https://example.test/{actionContext.Controller}/{actionContext.Action}/{id}";
        }

        public string? Content(string? contentPath) => contentPath;

        public bool IsLocalUrl(string? url) => true;

        public string? Link(string? routeName, object? values) => null;

        public string? RouteUrl(UrlRouteContext routeContext) => null;
    }

    private sealed class RecordingTourRepository : ITourRepository
    {
        public Tour? TourById { get; set; }
        public Tour? TourBySlug { get; set; }
        public List<string> GetByIdCalls { get; } = new();
        public List<string> GetBySlugCalls { get; } = new();

        public Task<Tour> GetByIdAsync(string id)
        {
            GetByIdCalls.Add(id);
            return Task.FromResult(TourById)!;
        }

        public Task<Tour?> GetBySlugAsync(string slug)
        {
            GetBySlugCalls.Add(slug);
            return Task.FromResult(TourBySlug);
        }

        public Task<IEnumerable<Tour>> GetAllAsync() => Task.FromResult<IEnumerable<Tour>>([]);

        public Task<IEnumerable<Tour>> FindAsync(Expression<Func<Tour, bool>> predicate) => Task.FromResult<IEnumerable<Tour>>([]);

        public Task AddAsync(Tour entity) => Task.CompletedTask;

        public Task UpdateAsync(string id, Tour entity) => Task.CompletedTask;

        public Task DeleteAsync(string id) => Task.CompletedTask;

        public Task<PaginatedResult<Tour>> GetPagedAsync(int pageIndex, int pageSize, Expression<Func<Tour, bool>>? filter = null)
            => Task.FromResult(new PaginatedResult<Tour>([], 0, pageIndex, pageSize));

        public Task<TourSearchResult> SearchAsync(TourSearchRequest request)
            => Task.FromResult(new TourSearchResult());

        public Task<bool> IncrementParticipantsAsync(string tourId, int count)
            => Task.FromResult(false);

        public Task<bool> ReserveDepartureAsync(string tourId, string departureId, int travellerCount)
            => Task.FromResult(false);
    }
}

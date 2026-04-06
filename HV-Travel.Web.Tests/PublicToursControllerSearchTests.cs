using HVTravel.Domain.Entities;
using HVTravel.Domain.Interfaces;
using HVTravel.Domain.Models;
using HVTravel.Web.Controllers;
using HV_Travel.Web.Tests.TestSupport;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace HV_Travel.Web.Tests;

public class PublicToursControllerSearchTests
{
    [Fact]
    public async Task Index_SearchMatchesPlainTextWhenDescriptionContainsHtmlEntities()
    {
        var repository = new InMemoryTourRepository(
        [
            new Tour
            {
                Id = "tour-1",
                Name = "Khám phá Hà Nội",
                Description = "<p>Được tham quan c&aacute;c danh lam thắng cảnh</p>",
                ShortDescription = "<p>Trải nghiệm thủ đô</p>",
                Status = "Active",
                Destination = new Destination { City = "Hà Nội", Country = "Việt Nam", Region = "Miền Bắc" },
                Price = new TourPrice { Adult = 1000000 },
                Duration = new TourDuration { Days = 2, Nights = 1, Text = "2 ngày 1 đêm" }
            }
        ]);

        var controller = new PublicToursController(repository);

        var result = await controller.Index(search: "các", sort: null);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<List<Tour>>(view.Model);
        var tour = Assert.Single(model);
        Assert.Equal("tour-1", tour.Id);
    }

    [Fact]
    public async Task Index_FiltersByRegionMonthAvailabilityAndPromotion()
    {
        var nextMonth = DateTime.UtcNow.AddMonths(1);
        var repository = new InMemoryTourRepository(
        [
            new Tour
            {
                Id = "tour-deal-north",
                Name = "Deal Đông Bắc",
                Description = "Tour deal mùa hè",
                ShortDescription = "Deal hot",
                Status = "Active",
                Destination = new Destination { City = "Hà Giang", Country = "Việt Nam", Region = "North" },
                Price = new TourPrice { Adult = 3200000, Discount = 15 },
                Duration = new TourDuration { Days = 3, Nights = 2, Text = "3 ngày 2 đêm" },
                StartDates = [new DateTime(nextMonth.Year, nextMonth.Month, 10)],
                MaxParticipants = 20,
                CurrentParticipants = 8,
                Rating = 4.8
            },
            new Tour
            {
                Id = "tour-standard-south",
                Name = "Miền Tây thư giãn",
                Description = "Không có ưu đãi",
                ShortDescription = "Sông nước",
                Status = "Active",
                Destination = new Destination { City = "Cần Thơ", Country = "Việt Nam", Region = "South" },
                Price = new TourPrice { Adult = 2500000, Discount = 0 },
                Duration = new TourDuration { Days = 2, Nights = 1, Text = "2 ngày 1 đêm" },
                StartDates = [new DateTime(nextMonth.Year, nextMonth.Month, 12)],
                MaxParticipants = 20,
                CurrentParticipants = 6,
                Rating = 4.6
            },
            new Tour
            {
                Id = "tour-sold-out",
                Name = "Deal hết chỗ",
                Description = "Đã kín",
                ShortDescription = "Hết chỗ",
                Status = "SoldOut",
                Destination = new Destination { City = "Lào Cai", Country = "Việt Nam", Region = "North" },
                Price = new TourPrice { Adult = 2800000, Discount = 10 },
                Duration = new TourDuration { Days = 3, Nights = 2, Text = "3 ngày 2 đêm" },
                StartDates = [new DateTime(nextMonth.Year, nextMonth.Month, 18)],
                MaxParticipants = 20,
                CurrentParticipants = 20,
                Rating = 4.9
            }
        ]);

        var controller = new PublicToursController(repository);

        var result = await controller.Index(
            search: null,
            sort: "price_asc",
            region: "North",
            destination: null,
            minPrice: 2000000,
            maxPrice: 3500000,
            departureMonth: nextMonth.Month,
            maxDays: 4,
            collection: "deal",
            availableOnly: true,
            promotionOnly: true,
            page: 1);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<List<Tour>>(view.Model);
        var tour = Assert.Single(model);
        Assert.Equal("tour-deal-north", tour.Id);
    }

    [Fact]
    public async Task Index_FiltersByTravellersConfirmationAndFreeCancellationUsingDepartureInventory()
    {
        var nextMonth = DateTime.UtcNow.AddMonths(1);
        var repository = new InMemoryTourRepository(
        [
            new Tour
            {
                Id = "tour-commerce-ready",
                Name = "Tokyo Premium",
                Description = "Có xác nhận nhanh",
                ShortDescription = "Ưu tiên gia đình",
                Status = "Active",
                Destination = new Destination { City = "Tokyo", Country = "Japan", Region = "North" },
                Price = new TourPrice { Adult = 12000000m, Discount = 0 },
                Duration = new TourDuration { Days = 5, Nights = 4, Text = "5 ngày 4 đêm" },
                ConfirmationType = "Instant",
                CancellationPolicy = new TourCancellationPolicy { Summary = "Miễn phí trước 72 giờ", IsFreeCancellation = true, FreeCancellationBeforeHours = 72 },
                Departures =
                [
                    new TourDeparture
                    {
                        Id = "dep-match",
                        StartDate = new DateTime(nextMonth.Year, nextMonth.Month, 20),
                        AdultPrice = 11800000m,
                        ChildPrice = 8200000m,
                        Capacity = 16,
                        BookedCount = 8,
                        ConfirmationType = "Instant",
                        DiscountPercentage = 5m
                    }
                ],
                Rating = 4.9
            },
            new Tour
            {
                Id = "tour-slow-confirm",
                Name = "Osaka Saver",
                Description = "Xác nhận chậm",
                ShortDescription = "OTA pending",
                Status = "Active",
                Destination = new Destination { City = "Osaka", Country = "Japan", Region = "North" },
                Price = new TourPrice { Adult = 9900000m, Discount = 0 },
                Duration = new TourDuration { Days = 5, Nights = 4, Text = "5 ngày 4 đêm" },
                ConfirmationType = "Request",
                CancellationPolicy = new TourCancellationPolicy { Summary = "Không miễn phí", IsFreeCancellation = false },
                Departures =
                [
                    new TourDeparture
                    {
                        Id = "dep-miss",
                        StartDate = new DateTime(nextMonth.Year, nextMonth.Month, 22),
                        AdultPrice = 9900000m,
                        Capacity = 16,
                        BookedCount = 2,
                        ConfirmationType = "Request"
                    }
                ],
                Rating = 4.7
            }
        ]);

        var controller = new PublicToursController(repository);

        var result = await controller.Index(
            search: null,
            sort: "best_value",
            region: "North",
            destination: null,
            minPrice: null,
            maxPrice: null,
            departureMonth: nextMonth.Month,
            maxDays: 6,
            collection: null,
            availableOnly: true,
            promotionOnly: false,
            travellers: 4,
            confirmationType: "Instant",
            cancellationType: "FreeCancellation",
            page: 1);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<List<Tour>>(view.Model);
        var tour = Assert.Single(model);
        Assert.Equal("tour-commerce-ready", tour.Id);
        Assert.Equal(4, Assert.IsType<int>(controller.ViewData["CurrentTravellers"]));
        Assert.Equal("Instant", controller.ViewData["CurrentConfirmationType"]);
        Assert.Equal("FreeCancellation", controller.ViewData["CurrentCancellationType"]);
    }

    [Fact]
    public async Task Details_FallsBackToSlugWithoutQueryingObjectIdParserForNonObjectIdInput()
    {
        var tour = new Tour
        {
            Id = "507f1f77bcf86cd799439011",
            Slug = "ha-long-premium",
            Name = "Hạ Long Premium",
            Description = "Du thuyền vịnh",
            ShortDescription = "Hành trình cao cấp",
            Status = "Active",
            Destination = new Destination { City = "Hạ Long", Country = "Việt Nam", Region = "Miền Bắc" },
            Price = new TourPrice { Adult = 4500000m },
            Duration = new TourDuration { Days = 2, Nights = 1, Text = "2 ngày 1 đêm" },
            Images = []
        };

        var controller = new PublicToursController(new SlugFirstTourRepository(tour))
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            },
            Url = new StubUrlHelper()
        };
        controller.ControllerContext.HttpContext.Request.Scheme = "https";

        var result = await controller.Details("ha-long-premium");

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<HVTravel.Web.Models.PublicTourDetailsPageViewModel>(view.Model);
        Assert.Equal("ha-long-premium", model.Tour.Slug);
    }

    private sealed class SlugFirstTourRepository : ITourRepository
    {
        private readonly Tour _tour;

        public SlugFirstTourRepository(Tour tour)
        {
            _tour = tour;
        }

        public Task<IEnumerable<Tour>> GetAllAsync() => Task.FromResult<IEnumerable<Tour>>([_tour]);
        public Task<Tour> GetByIdAsync(string id) => throw new FormatException($"Unexpected id lookup for {id}");
        public Task<Tour?> GetBySlugAsync(string slug) => Task.FromResult<Tour?>(string.Equals(slug, _tour.Slug, StringComparison.OrdinalIgnoreCase) ? _tour : null);
        public Task<IEnumerable<Tour>> FindAsync(System.Linq.Expressions.Expression<Func<Tour, bool>> predicate) => Task.FromResult<IEnumerable<Tour>>([_tour]);
        public Task AddAsync(Tour entity) => Task.CompletedTask;
        public Task UpdateAsync(string id, Tour entity) => Task.CompletedTask;
        public Task DeleteAsync(string id) => Task.CompletedTask;
        public Task<PaginatedResult<Tour>> GetPagedAsync(int pageIndex, int pageSize, System.Linq.Expressions.Expression<Func<Tour, bool>>? filter = null) => Task.FromResult(new PaginatedResult<Tour>([_tour], 1, pageIndex, pageSize));
        public Task<TourSearchResult> SearchAsync(TourSearchRequest request) => Task.FromResult(new TourSearchResult { Items = [_tour], CurrentPage = 1, TotalItems = 1, TotalPages = 1 });
        public Task<bool> IncrementParticipantsAsync(string tourId, int count) => Task.FromResult(false);
        public Task<bool> ReserveDepartureAsync(string tourId, string departureId, int travellerCount) => Task.FromResult(false);
    }

    private sealed class StubUrlHelper : IUrlHelper
    {
        public ActionContext ActionContext { get; } = new();
        public string? Action(UrlActionContext actionContext) => $"/{actionContext.Controller}/{actionContext.Action}";
        public string? Content(string? contentPath) => contentPath;
        public bool IsLocalUrl(string? url) => true;
        public string? Link(string? routeName, object? values) => "/PublicTours/Details";
        public string? RouteUrl(UrlRouteContext routeContext) => "/PublicTours/Details";
    }
}

using HVTravel.Domain.Entities;
using HVTravel.Web.Controllers;
using HV_Travel.Web.Tests.TestSupport;
using Microsoft.AspNetCore.Mvc;

namespace HV_Travel.Web.Tests;

public class PublicToursControllerSearchTests
{
    [Fact]
    public async Task Index_SearchMatchesPlainTextWhenDescriptionContainsHtmlEntities()
    {
        var repository = new InMemoryRepository<Tour>(
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
        var repository = new InMemoryRepository<Tour>(
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
}

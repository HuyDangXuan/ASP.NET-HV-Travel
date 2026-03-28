using HVTravel.Domain.Entities;
using HVTravel.Domain.Interfaces;
using HVTravel.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace HVTravel.Web.Controllers;

public class DestinationsController : Controller
{
    private readonly IRepository<Tour> _tourRepository;

    public DestinationsController(IRepository<Tour> tourRepository)
    {
        _tourRepository = tourRepository;
    }

    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Hub điểm đến";
        ViewData["ActivePage"] = "Destinations";

        var tours = (await _tourRepository.GetAllAsync())
            .Where(t => t.Status is "Active" or "ComingSoon" or "SoldOut")
            .ToList();

        var regions = tours
            .GroupBy(t => t.Destination?.Region ?? "Khác")
            .Select(group => new DestinationRegionViewModel
            {
                Region = group.Key,
                Destinations = group
                    .GroupBy(t => new { City = t.Destination?.City ?? "Chưa xác định", Country = t.Destination?.Country ?? "Việt Nam" })
                    .Select(cityGroup => new DestinationCardViewModel
                    {
                        City = cityGroup.Key.City,
                        Country = cityGroup.Key.Country,
                        TourCount = cityGroup.Count(),
                        StartingPrice = cityGroup.Min(t => t.Price?.Adult ?? 0),
                        BestRating = cityGroup.Max(t => t.Rating),
                        RepresentativeImage = cityGroup.SelectMany(t => t.Images).FirstOrDefault() ?? string.Empty
                    })
                    .OrderByDescending(card => card.TourCount)
                    .ThenBy(card => card.City)
                    .ToList()
            })
            .OrderBy(region => region.Region)
            .ToList();

        return View(new DestinationHubViewModel
        {
            Regions = regions,
            Collections = new List<DestinationCollectionViewModel>
            {
                CreateCollection("domestic", "Trong nước nổi bật", "Những hành trình ngắn ngày, dễ bán, dễ upsell và có tỷ lệ chốt cao.", tours.Where(t => string.Equals(t.Destination?.Country, "Vietnam", StringComparison.OrdinalIgnoreCase) || string.Equals(t.Destination?.Country, "Việt Nam", StringComparison.OrdinalIgnoreCase)).Take(4)),
                CreateCollection("premium", "Premium selection", "Ưu tiên khách chi tiêu cao, lịch trình mượt và dịch vụ chỉn chu.", tours.Where(t => (t.Price?.Adult ?? 0) >= 5000000).OrderByDescending(t => t.Rating).Take(4)),
                CreateCollection("deal", "Săn deal", "Tour có giảm giá, ghế còn ít và dễ tạo urgency ngoài mặt tiền.", tours.Where(t => (t.Price?.Discount ?? 0) > 0).Take(4)),
                CreateCollection("family", "Cho gia đình", "Các lịch trình cân bằng giữa trải nghiệm và nhịp di chuyển nhẹ.", tours.Where(t => t.MaxParticipants >= 10).Take(4))
            },
            TrendingTours = tours.OrderByDescending(t => t.Rating).ThenByDescending(t => t.ReviewCount).Take(6).ToList()
        });
    }

    private static DestinationCollectionViewModel CreateCollection(string key, string title, string description, IEnumerable<Tour> tours)
    {
        return new DestinationCollectionViewModel
        {
            Key = key,
            Title = title,
            Description = description,
            Tours = tours.ToList()
        };
    }
}

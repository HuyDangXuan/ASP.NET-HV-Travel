using System.Security.Claims;
using HVTravel.Domain.Entities;
using HVTravel.Domain.Interfaces;
using HVTravel.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace HVTravel.Web.Controllers;

public class PromotionsController : Controller
{
    private readonly IRepository<Promotion> _promotionRepository;
    private readonly IRepository<Customer> _customerRepository;

    public PromotionsController(IRepository<Promotion> promotionRepository, IRepository<Customer> customerRepository)
    {
        _promotionRepository = promotionRepository;
        _customerRepository = customerRepository;
    }

    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Ưu đãi & voucher";
        ViewData["ActivePage"] = "Promotions";

        var promotions = (await _promotionRepository.GetAllAsync())
            .Where(p => p.IsActive && p.ValidFrom <= DateTime.UtcNow && p.ValidTo >= DateTime.UtcNow)
            .OrderByDescending(p => p.IsFlashSale)
            .ThenByDescending(p => p.Priority)
            .ThenByDescending(p => p.DiscountPercentage)
            .ToList();

        var segment = string.Empty;
        var customerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrWhiteSpace(customerId))
        {
            segment = (await _customerRepository.GetByIdAsync(customerId))?.Segment ?? string.Empty;
        }

        return View(new PromotionsIndexViewModel
        {
            FlashSales = promotions.Where(p => p.IsFlashSale || string.Equals(p.CampaignType, "FlashSale", StringComparison.OrdinalIgnoreCase)).Take(4).ToList(),
            VoucherCampaigns = promotions.Where(p => string.Equals(p.CampaignType, "Voucher", StringComparison.OrdinalIgnoreCase)).Take(6).ToList(),
            SeasonalDeals = promotions.Where(p => !string.Equals(p.CampaignType, "Voucher", StringComparison.OrdinalIgnoreCase)).Take(6).ToList(),
            CustomerSegment = segment
        });
    }
}

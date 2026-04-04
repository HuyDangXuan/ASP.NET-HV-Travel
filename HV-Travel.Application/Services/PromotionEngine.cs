using HVTravel.Application.Interfaces;
using HVTravel.Application.Models;
using HVTravel.Domain.Entities;
using HVTravel.Domain.Interfaces;

namespace HVTravel.Application.Services;

public class PromotionEngine : IPromotionEngine
{
    private readonly IRepository<Promotion> _promotionRepository;

    public PromotionEngine(IRepository<Promotion> promotionRepository)
    {
        _promotionRepository = promotionRepository;
    }

    public async Task<PromotionMatchResult?> ResolveCouponAsync(string? couponCode, string? customerSegment, decimal subtotal, string? destination)
    {
        if (string.IsNullOrWhiteSpace(couponCode))
        {
            return null;
        }

        var normalizedCode = couponCode.Trim();
        var now = DateTime.UtcNow;
        var promotions = await _promotionRepository.FindAsync(promotion =>
            promotion.IsActive &&
            promotion.Code == normalizedCode &&
            promotion.ValidFrom <= now &&
            promotion.ValidTo >= now);

        var promotion = promotions
            .OrderByDescending(item => item.Priority)
            .ThenByDescending(item => item.DiscountPercentage)
            .FirstOrDefault();

        if (promotion == null)
        {
            return null;
        }

        if (promotion.MinimumSpend > 0 && subtotal < promotion.MinimumSpend)
        {
            return null;
        }

        if (promotion.UsageLimit > 0 && promotion.UsageCount >= promotion.UsageLimit)
        {
            return null;
        }

        if (promotion.EligibleSegments.Count > 0 &&
            !promotion.EligibleSegments.Any(segment => string.Equals(segment, customerSegment, StringComparison.OrdinalIgnoreCase)))
        {
            return null;
        }

        if (promotion.ApplicableDestinations.Count > 0 &&
            !promotion.ApplicableDestinations.Any(item => string.Equals(item, destination, StringComparison.OrdinalIgnoreCase)))
        {
            return null;
        }

        var discountAmount = promotion.DiscountValue > 0m
            ? Math.Min(subtotal, promotion.DiscountValue)
            : Math.Round(subtotal * (decimal)(promotion.DiscountPercentage / 100d), 0, MidpointRounding.AwayFromZero);

        if (discountAmount <= 0m)
        {
            return null;
        }

        return new PromotionMatchResult
        {
            Promotion = promotion,
            Redemption = new CouponRedemption
            {
                Code = promotion.Code,
                Title = string.IsNullOrWhiteSpace(promotion.Title) ? promotion.Code : promotion.Title,
                DiscountAmount = discountAmount
            }
        };
    }
}

using HVTravel.Domain.Entities;
using HVTravel.Domain.Interfaces;
using HVTravel.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HVTravel.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(AuthenticationSchemes = AuthSchemes.AdminScheme, Roles = "Admin,Manager,Staff")]
public class ReviewsController : Controller
{
    private readonly IRepository<Review> _reviewRepository;

    public ReviewsController(IRepository<Review> reviewRepository)
    {
        _reviewRepository = reviewRepository;
    }

    public async Task<IActionResult> Index(string moderationStatus = "")
    {
        ViewData["AdminSection"] = "reviews";
        ViewData["Title"] = "Review moderation";
        var reviews = (await _reviewRepository.GetAllAsync()).AsEnumerable();
        if (!string.IsNullOrWhiteSpace(moderationStatus))
        {
            reviews = reviews.Where(review => string.Equals(review.ModerationStatus, moderationStatus, StringComparison.OrdinalIgnoreCase));
        }

        return View(reviews.OrderByDescending(review => review.CreatedAt).ToList());
    }

    [HttpPost]
    public async Task<IActionResult> Moderate(string id, string decision)
    {
        var review = await _reviewRepository.GetByIdAsync(id);
        if (review == null)
        {
            return RedirectToAction(nameof(Index));
        }

        var approved = string.Equals(decision, "approve", StringComparison.OrdinalIgnoreCase);
        review.IsApproved = approved;
        review.ModerationStatus = approved ? "Approved" : "Rejected";
        review.ModeratedAt = DateTime.UtcNow;
        review.ModeratorName = User.Identity?.Name ?? "Admin";
        await _reviewRepository.UpdateAsync(review.Id, review);
        return RedirectToAction(nameof(Index));
    }
}

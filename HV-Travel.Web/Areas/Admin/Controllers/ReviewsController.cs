using HVTravel.Domain.Entities;
using HVTravel.Domain.Interfaces;
using HVTravel.Domain.Models;
using HVTravel.Web.Models;
using HVTravel.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HVTravel.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(AuthenticationSchemes = AuthSchemes.AdminScheme, Roles = "Admin,Manager,Staff")]
public class ReviewsController : Controller
{
    private readonly IRepository<Review> _reviewRepository;
    private const int DefaultPageSize = 10;
    private static readonly int[] AllowedPageSizes = [10, 15, 20, 50, 100];

    public ReviewsController(IRepository<Review> reviewRepository)
    {
        _reviewRepository = reviewRepository;
    }

    public async Task<IActionResult> Index(
        string status = "all",
        string verified = "all",
        string searchString = "",
        string startDate = "",
        string endDate = "",
        string sortOrder = "",
        int page = 1,
        int pageSize = DefaultPageSize)
    {
        ViewData["AdminSection"] = "reviews";
        ViewData["Title"] = "Quản lý đánh giá";

        if (!AllowedPageSizes.Contains(pageSize))
        {
            pageSize = DefaultPageSize;
        }

        ViewBag.CurrentSort = sortOrder;
        ViewBag.DateSortParm = string.IsNullOrEmpty(sortOrder) ? "date_asc" : string.Empty;
        ViewBag.RatingSortParm = sortOrder == "rating" ? "rating_desc" : "rating";
        ViewBag.StatusSortParm = sortOrder == "status" ? "status_desc" : "status";

        var reviews = (await _reviewRepository.GetAllAsync()).AsEnumerable();
        var normalizedStatus = string.IsNullOrWhiteSpace(status) ? "all" : status.Trim().ToLowerInvariant();
        var normalizedVerified = string.IsNullOrWhiteSpace(verified) ? "all" : verified.Trim().ToLowerInvariant();
        var normalizedSearch = string.IsNullOrWhiteSpace(searchString) ? null : searchString.Trim().ToLowerInvariant();

        DateTime? start = null;
        DateTime? end = null;

        if (!string.IsNullOrWhiteSpace(startDate) && DateTime.TryParse(startDate, out var parsedStart))
        {
            start = parsedStart.Date;
        }

        if (!string.IsNullOrWhiteSpace(endDate) && DateTime.TryParse(endDate, out var parsedEnd))
        {
            end = parsedEnd.Date.AddDays(1).AddTicks(-1);
        }

        reviews = reviews.Where(review =>
            MatchesModerationStatus(review, normalizedStatus) &&
            MatchesVerificationState(review, normalizedVerified) &&
            MatchesSearch(review, normalizedSearch) &&
            (start == null || review.CreatedAt >= start.Value) &&
            (end == null || review.CreatedAt <= end.Value));

        reviews = sortOrder switch
        {
            "date_asc" => reviews.OrderBy(review => review.CreatedAt),
            "rating" => reviews.OrderBy(review => review.Rating).ThenByDescending(review => review.CreatedAt),
            "rating_desc" => reviews.OrderByDescending(review => review.Rating).ThenByDescending(review => review.CreatedAt),
            "status" => reviews.OrderBy(review => GetModerationStatusRank(review.ModerationStatus)).ThenByDescending(review => review.CreatedAt),
            "status_desc" => reviews.OrderByDescending(review => GetModerationStatusRank(review.ModerationStatus)).ThenByDescending(review => review.CreatedAt),
            _ => reviews.OrderByDescending(review => review.CreatedAt)
        };

        var totalCount = reviews.Count();
        var items = reviews
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(ToListItem)
            .ToList();

        ViewData["CurrentStatus"] = normalizedStatus;
        ViewData["CurrentVerified"] = normalizedVerified;
        ViewData["CurrentSearch"] = searchString;
        ViewData["CurrentStartDate"] = startDate;
        ViewData["CurrentEndDate"] = endDate;
        ViewData["CurrentPageSize"] = pageSize;

        return View(new PaginatedResult<AdminReviewListItemViewModel>(items, totalCount, page, pageSize));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Moderate(string id, string decision, string? returnUrl = null)
    {
        var review = await _reviewRepository.GetByIdAsync(id);
        if (review == null || !TryResolveDecision(decision, out var isApproved))
        {
            return RedirectToIndex(returnUrl);
        }

        ApplyModeration(review, isApproved, User.Identity?.Name ?? "Admin");
        await _reviewRepository.UpdateAsync(review.Id, review);
        return RedirectToIndex(returnUrl);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BulkModerate([FromForm] List<string> ids, [FromForm] string decision, [FromForm] string? returnUrl = null)
    {
        if (ids == null || ids.Count == 0 || !TryResolveDecision(decision, out var isApproved))
        {
            return RedirectToIndex(returnUrl);
        }

        var moderatorName = User.Identity?.Name ?? "Admin";
        var validIds = ids
            .Where(id => !string.IsNullOrWhiteSpace(id) && id.Length == 24)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        foreach (var id in validIds)
        {
            var review = await _reviewRepository.GetByIdAsync(id);
            if (review == null)
            {
                continue;
            }

            ApplyModeration(review, isApproved, moderatorName);
            await _reviewRepository.UpdateAsync(review.Id, review);
        }

        return RedirectToIndex(returnUrl);
    }

    private IActionResult RedirectToIndex(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction(nameof(Index));
    }

    private static bool TryResolveDecision(string? decision, out bool isApproved)
    {
        if (string.Equals(decision, "approve", StringComparison.OrdinalIgnoreCase))
        {
            isApproved = true;
            return true;
        }

        if (string.Equals(decision, "reject", StringComparison.OrdinalIgnoreCase))
        {
            isApproved = false;
            return true;
        }

        isApproved = false;
        return false;
    }

    private static void ApplyModeration(Review review, bool isApproved, string moderatorName)
    {
        review.IsApproved = isApproved;
        review.ModerationStatus = isApproved ? "Approved" : "Rejected";
        review.ModeratedAt = DateTime.UtcNow;
        review.ModeratorName = moderatorName;
    }

    private static bool MatchesModerationStatus(Review review, string normalizedStatus)
    {
        return normalizedStatus switch
        {
            "pending" => string.Equals(review.ModerationStatus, "Pending", StringComparison.OrdinalIgnoreCase),
            "approved" => string.Equals(review.ModerationStatus, "Approved", StringComparison.OrdinalIgnoreCase),
            "rejected" => string.Equals(review.ModerationStatus, "Rejected", StringComparison.OrdinalIgnoreCase),
            _ => true
        };
    }

    private static bool MatchesVerificationState(Review review, string normalizedVerified)
    {
        return normalizedVerified switch
        {
            "verified" => review.IsVerifiedBooking,
            "unverified" => !review.IsVerifiedBooking,
            _ => true
        };
    }

    private static bool MatchesSearch(Review review, string? normalizedSearch)
    {
        if (string.IsNullOrWhiteSpace(normalizedSearch))
        {
            return true;
        }

        return (review.Id?.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) ?? false) ||
               (review.BookingId?.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) ?? false) ||
               (review.DisplayName?.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) ?? false) ||
               (review.Comment?.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) ?? false) ||
               (review.ModeratorName?.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) ?? false);
    }

    private static int GetModerationStatusRank(string? moderationStatus)
    {
        return moderationStatus?.ToLowerInvariant() switch
        {
            "pending" => 0,
            "approved" => 1,
            "rejected" => 2,
            _ => 99
        };
    }

    private static AdminReviewListItemViewModel ToListItem(Review review)
    {
        return new AdminReviewListItemViewModel
        {
            Id = review.Id,
            ReviewCode = BuildReviewCode(review.Id),
            DisplayName = string.IsNullOrWhiteSpace(review.DisplayName) ? "Khách ẩn danh" : review.DisplayName.Trim(),
            ReviewerInitials = BuildInitials(review.DisplayName),
            Rating = review.Rating,
            Comment = review.Comment ?? string.Empty,
            CommentPreview = BuildCommentPreview(review.Comment),
            ModerationStatus = review.ModerationStatus ?? "Pending",
            ModerationStatusText = TranslateModerationStatus(review.ModerationStatus),
            CreatedAt = review.CreatedAt,
            IsVerifiedBooking = review.IsVerifiedBooking,
            VerificationText = review.IsVerifiedBooking ? "Đã xác thực" : "Chưa xác thực",
            ModeratorName = review.ModeratorName ?? string.Empty,
            ModeratedAt = review.ModeratedAt,
            BookingId = review.BookingId ?? string.Empty
        };
    }

    private static string BuildReviewCode(string? id)
    {
        if (!string.IsNullOrWhiteSpace(id) && id.Length >= 6)
        {
            return $"RV-{id[^6..].ToUpperInvariant()}";
        }

        return "RV-REVIEW";
    }

    private static string BuildInitials(string? displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            return "RV";
        }

        var parts = displayName
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .Take(2)
            .ToArray();

        if (parts.Length == 0)
        {
            return "RV";
        }

        return string.Concat(parts.Select(part => char.ToUpperInvariant(part[0])));
    }

    private static string BuildCommentPreview(string? comment)
    {
        if (string.IsNullOrWhiteSpace(comment))
        {
            return "Chưa có nội dung đánh giá.";
        }

        var compact = string.Join(" ", comment
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

        return compact.Length <= 120 ? compact : $"{compact[..117]}...";
    }

    private static string TranslateModerationStatus(string? moderationStatus)
    {
        return moderationStatus?.ToLowerInvariant() switch
        {
            "approved" => "Đã duyệt",
            "rejected" => "Đã từ chối",
            _ => "Chờ duyệt"
        };
    }
}

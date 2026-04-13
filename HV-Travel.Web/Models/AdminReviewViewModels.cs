namespace HVTravel.Web.Models;

public class AdminReviewListItemViewModel
{
    public string Id { get; set; } = string.Empty;
    public string ReviewCode { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string ReviewerInitials { get; set; } = "RV";
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public string CommentPreview { get; set; } = string.Empty;
    public string ModerationStatus { get; set; } = "Pending";
    public string ModerationStatusText { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsVerifiedBooking { get; set; }
    public string VerificationText { get; set; } = string.Empty;
    public string ModeratorName { get; set; } = string.Empty;
    public DateTime? ModeratedAt { get; set; }
    public string BookingId { get; set; } = string.Empty;
}

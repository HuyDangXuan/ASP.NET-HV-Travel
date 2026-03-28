namespace HVTravel.Web.Models;

public class BookingLookupViewModel
{
    public string QueryBookingCode { get; set; } = string.Empty;
    public string QueryEmail { get; set; } = string.Empty;
    public string QueryPhone { get; set; } = string.Empty;
    public string BookingCode { get; set; } = string.Empty;
    public string BookingStatus { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public string TourName { get; set; } = string.Empty;
    public string TourCode { get; set; } = string.Empty;
    public string Duration { get; set; } = string.Empty;
    public string ContactName { get; set; } = string.Empty;
    public int ParticipantsCount { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime BookingDate { get; set; }
    public IReadOnlyList<string> History { get; set; } = Array.Empty<string>();
    public bool HasResult => !string.IsNullOrWhiteSpace(BookingCode);
}

namespace HVTravel.Web.Models;

public class QuotePreviewResponse
{
    public decimal Subtotal { get; set; }
    public decimal DiscountTotal { get; set; }
    public decimal GrandTotal { get; set; }
    public decimal AmountDueNow { get; set; }
    public decimal BalanceDue { get; set; }
    public string AppliedCouponCode { get; set; } = string.Empty;
    public int RemainingCapacity { get; set; }
    public IReadOnlyList<string> Badges { get; set; } = Array.Empty<string>();
    public bool IsAvailable { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
}
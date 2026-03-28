namespace HVTravel.Web.Models;

public class PaymentGatewayWebhookModel
{
    public string BookingCode { get; set; } = string.Empty;
    public string TransactionId { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string Method { get; set; } = "OnlineGateway";
    public string Status { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string Signature { get; set; } = string.Empty;
}

using MongoDB.Bson.Serialization.Attributes;

namespace HVTravel.Domain.Entities;

public partial class Booking
{
    [BsonElement("departureId")]
    public string DepartureId { get; set; } = string.Empty;

    [BsonElement("pricingBreakdown")]
    public PricingBreakdown PricingBreakdown { get; set; } = new();

    [BsonElement("couponCode")]
    public string CouponCode { get; set; } = string.Empty;

    [BsonElement("voucherRedemptions")]
    public List<CouponRedemption> VoucherRedemptions { get; set; } = new();

    [BsonElement("paymentPlan")]
    public PaymentPlan PaymentPlan { get; set; } = new();

    [BsonElement("paymentSessions")]
    public List<PaymentSession> PaymentSessions { get; set; } = new();

    [BsonElement("issuedDocuments")]
    public List<IssuedDocument> IssuedDocuments { get; set; } = new();

    [BsonElement("fulfillmentStatus")]
    public string FulfillmentStatus { get; set; } = "Pending";

    [BsonElement("fulfillmentItems")]
    public List<FulfillmentItem> FulfillmentItems { get; set; } = new();

    [BsonElement("checkoutSessionId")]
    public string CheckoutSessionId { get; set; } = string.Empty;
}

[BsonIgnoreExtraElements]
public class PricingBreakdown
{
    [BsonElement("subtotal")]
    public decimal Subtotal { get; set; }

    [BsonElement("discountTotal")]
    public decimal DiscountTotal { get; set; }

    [BsonElement("grandTotal")]
    public decimal GrandTotal { get; set; }
}

[BsonIgnoreExtraElements]
public class CouponRedemption
{
    [BsonElement("code")]
    public string Code { get; set; } = string.Empty;

    [BsonElement("title")]
    public string Title { get; set; } = string.Empty;

    [BsonElement("discountAmount")]
    public decimal DiscountAmount { get; set; }
}

[BsonIgnoreExtraElements]
public class PaymentPlan
{
    [BsonElement("planType")]
    public string PlanType { get; set; } = "Full";

    [BsonElement("amountDueNow")]
    public decimal AmountDueNow { get; set; }

    [BsonElement("balanceDue")]
    public decimal BalanceDue { get; set; }

    [BsonElement("depositPercentage")]
    public decimal DepositPercentage { get; set; }
}

[BsonIgnoreExtraElements]
public class PaymentSession
{
    [BsonElement("id")]
    public string Id { get; set; } = string.Empty;

    [BsonElement("provider")]
    public string Provider { get; set; } = string.Empty;

    [BsonElement("status")]
    public string Status { get; set; } = string.Empty;

    [BsonElement("idempotencyKey")]
    public string IdempotencyKey { get; set; } = string.Empty;

    [BsonElement("reference")]
    public string Reference { get; set; } = string.Empty;

    [BsonElement("amount")]
    public decimal Amount { get; set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

[BsonIgnoreExtraElements]
public class IssuedDocument
{
    [BsonElement("id")]
    public string Id { get; set; } = string.Empty;

    [BsonElement("documentType")]
    public string DocumentType { get; set; } = string.Empty;

    [BsonElement("code")]
    public string Code { get; set; } = string.Empty;

    [BsonElement("url")]
    public string Url { get; set; } = string.Empty;

    [BsonElement("issuedAt")]
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
}

[BsonIgnoreExtraElements]
public class FulfillmentItem
{
    [BsonElement("itemType")]
    public string ItemType { get; set; } = string.Empty;

    [BsonElement("status")]
    public string Status { get; set; } = string.Empty;

    [BsonElement("supplierId")]
    public string SupplierId { get; set; } = string.Empty;

    [BsonElement("reference")]
    public string Reference { get; set; } = string.Empty;
}


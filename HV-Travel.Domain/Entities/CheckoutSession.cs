using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HVTravel.Domain.Entities;

[BsonIgnoreExtraElements]
public class CheckoutSession
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonRepresentation(BsonType.ObjectId)]
    [BsonElement("bookingId")]
    public string BookingId { get; set; } = string.Empty;

    [BsonRepresentation(BsonType.ObjectId)]
    [BsonElement("tourId")]
    public string TourId { get; set; } = string.Empty;

    [BsonElement("departureId")]
    public string DepartureId { get; set; } = string.Empty;

    [BsonElement("status")]
    public string Status { get; set; } = "Open";

    [BsonElement("contactEmail")]
    public string ContactEmail { get; set; } = string.Empty;

    [BsonElement("couponCode")]
    public string CouponCode { get; set; } = string.Empty;

    [BsonElement("paymentPlanType")]
    public string PaymentPlanType { get; set; } = "Full";

    [BsonElement("expiresAt")]
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddMinutes(30);

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

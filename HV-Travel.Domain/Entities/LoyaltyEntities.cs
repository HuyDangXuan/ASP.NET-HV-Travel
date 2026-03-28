using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HVTravel.Domain.Entities;

[BsonIgnoreExtraElements]
public class LoyaltyLedgerEntry
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonRepresentation(BsonType.ObjectId)]
    [BsonElement("customerId")]
    public string CustomerId { get; set; } = string.Empty;

    [BsonRepresentation(BsonType.ObjectId)]
    [BsonElement("bookingId")]
    public string BookingId { get; set; } = string.Empty;

    [BsonElement("type")]
    public string Type { get; set; } = "Earn";

    [BsonElement("title")]
    public string Title { get; set; } = string.Empty;

    [BsonElement("points")]
    public int Points { get; set; }

    [BsonElement("balanceAfter")]
    public int BalanceAfter { get; set; }

    [BsonElement("note")]
    public string Note { get; set; } = string.Empty;

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("expiresAt")]
    public DateTime? ExpiresAt { get; set; }
}

[BsonIgnoreExtraElements]
public class VoucherWalletItem
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonRepresentation(BsonType.ObjectId)]
    [BsonElement("customerId")]
    public string CustomerId { get; set; } = string.Empty;

    [BsonRepresentation(BsonType.ObjectId)]
    [BsonElement("promotionId")]
    public string PromotionId { get; set; } = string.Empty;

    [BsonElement("code")]
    public string Code { get; set; } = string.Empty;

    [BsonElement("title")]
    public string Title { get; set; } = string.Empty;

    [BsonElement("discountPercentage")]
    public double DiscountPercentage { get; set; }

    [BsonElement("discountValue")]
    public decimal DiscountValue { get; set; }

    [BsonElement("minimumSpend")]
    public decimal MinimumSpend { get; set; }

    [BsonElement("status")]
    public string Status { get; set; } = "Active";

    [BsonElement("source")]
    public string Source { get; set; } = string.Empty;

    [BsonElement("issuedAt")]
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("expiresAt")]
    public DateTime? ExpiresAt { get; set; }
}

[BsonIgnoreExtraElements]
public class SavedTravellerProfile
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonRepresentation(BsonType.ObjectId)]
    [BsonElement("customerId")]
    public string CustomerId { get; set; } = string.Empty;

    [BsonElement("fullName")]
    public string FullName { get; set; } = string.Empty;

    [BsonElement("dateOfBirth")]
    public DateTime? DateOfBirth { get; set; }

    [BsonElement("gender")]
    public string Gender { get; set; } = string.Empty;

    [BsonElement("passportNumber")]
    public string PassportNumber { get; set; } = string.Empty;

    [BsonElement("nationality")]
    public string Nationality { get; set; } = string.Empty;

    [BsonElement("phone")]
    public string Phone { get; set; } = string.Empty;

    [BsonElement("email")]
    public string Email { get; set; } = string.Empty;

    [BsonElement("note")]
    public string Note { get; set; } = string.Empty;

    [BsonElement("isDefault")]
    public bool IsDefault { get; set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

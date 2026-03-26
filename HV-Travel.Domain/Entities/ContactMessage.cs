using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HVTravel.Domain.Entities;

[BsonIgnoreExtraElements]
public class ContactMessage
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("fullName")]
    public string FullName { get; set; } = string.Empty;

    [BsonElement("phoneNumber")]
    public string PhoneNumber { get; set; } = string.Empty;

    [BsonElement("email")]
    public string Email { get; set; } = string.Empty;

    [BsonElement("subject")]
    public string Subject { get; set; } = string.Empty;

    [BsonElement("message")]
    public string Message { get; set; } = string.Empty;

    [BsonElement("notificationEmail")]
    public string NotificationEmail { get; set; } = string.Empty;

    [BsonElement("emailSent")]
    public bool EmailSent { get; set; }

    [BsonElement("emailSentAt")]
    public DateTime? EmailSentAt { get; set; }

    [BsonElement("emailError")]
    public string EmailError { get; set; } = string.Empty;

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

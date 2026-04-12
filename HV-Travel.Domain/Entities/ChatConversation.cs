using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HVTravel.Domain.Entities;

[BsonIgnoreExtraElements]
public class ChatConversation
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("conversationCode")]
    public string ConversationCode { get; set; } = string.Empty;

    [BsonElement("channel")]
    public string Channel { get; set; } = "web";

    [BsonElement("status")]
    public string Status { get; set; } = "open";

    [BsonElement("participantType")]
    public string ParticipantType { get; set; } = "guest";

    [BsonElement("customerId")]
    public string? CustomerId { get; set; }

    [BsonElement("visitorSessionId")]
    public string? VisitorSessionId { get; set; }

    [BsonElement("guestProfile")]
    public GuestChatProfile GuestProfile { get; set; } = new();

    [BsonElement("assignedStaffUserId")]
    public string? AssignedStaffUserId { get; set; }

    [BsonElement("sourcePage")]
    public string SourcePage { get; set; } = "/";

    [BsonElement("contextType")]
    public string? ContextType { get; set; }

    [BsonElement("contextId")]
    public string? ContextId { get; set; }

    [BsonElement("contextLabel")]
    public string? ContextLabel { get; set; }

    [BsonElement("lastMessagePreview")]
    public string LastMessagePreview { get; set; } = string.Empty;

    [BsonElement("lastMessageAt")]
    public DateTime LastMessageAt { get; set; } = DateTime.UtcNow;

    [BsonElement("unreadForAdminCount")]
    public int UnreadForAdminCount { get; set; }

    [BsonElement("unreadForCustomerCount")]
    public int UnreadForCustomerCount { get; set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

[BsonIgnoreExtraElements]
public class GuestChatProfile
{
    [BsonElement("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [BsonElement("email")]
    public string Email { get; set; } = string.Empty;

    [BsonElement("phoneNumber")]
    public string PhoneNumber { get; set; } = string.Empty;
}

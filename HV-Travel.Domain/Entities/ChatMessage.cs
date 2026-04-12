using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HVTravel.Domain.Entities;

[BsonIgnoreExtraElements]
public class ChatMessage
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("conversationId")]
    public string ConversationId { get; set; } = string.Empty;

    [BsonElement("senderType")]
    public string SenderType { get; set; } = "guest";

    [BsonElement("senderUserId")]
    public string? SenderUserId { get; set; }

    [BsonElement("senderDisplayName")]
    public string SenderDisplayName { get; set; } = string.Empty;

    [BsonElement("messageType")]
    public string MessageType { get; set; } = "text";

    [BsonElement("clientMessageId")]
    public string ClientMessageId { get; set; } = Guid.NewGuid().ToString("N");

    [BsonElement("content")]
    public string Content { get; set; } = string.Empty;

    [BsonElement("isRead")]
    public bool IsRead { get; set; }

    [BsonElement("readAt")]
    public DateTime? ReadAt { get; set; }

    [BsonElement("sentAt")]
    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

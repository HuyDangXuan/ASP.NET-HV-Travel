using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace HVTravel.Domain.Entities
{
    [BsonIgnoreExtraElements]
    public class Notification
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("recipientId")]
        public string RecipientId { get; set; } // Can be userId or "ALL"

        [BsonElement("type")]
        public string Type { get; set; } // Order, System, Promotion

        [BsonElement("title")]
        public string Title { get; set; }

        [BsonElement("message")]
        public string Message { get; set; }

        [BsonElement("link")]
        public string Link { get; set; }

        [BsonElement("isRead")]
        public bool IsRead { get; set; } = false;

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

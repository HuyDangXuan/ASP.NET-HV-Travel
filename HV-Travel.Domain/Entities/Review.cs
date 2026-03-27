using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace HVTravel.Domain.Entities
{
    [BsonIgnoreExtraElements]
    public class Review
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [Required]
        [BsonRepresentation(BsonType.ObjectId)]
        [BsonElement("tourId")]
        public string TourId { get; set; }

        [Required]
        [BsonRepresentation(BsonType.ObjectId)]
        [BsonElement("customerId")]
        public string CustomerId { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        [BsonElement("bookingId")]
        public string BookingId { get; set; } = string.Empty;

        [Required]
        [Range(1, 5)]
        [BsonElement("rating")]
        public int Rating { get; set; }

        [BsonElement("comment")]
        public string Comment { get; set; }

        [BsonElement("displayName")]
        public string DisplayName { get; set; } = string.Empty;

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("isApproved")]
        public bool IsApproved { get; set; } = false;

        [BsonElement("isVerifiedBooking")]
        public bool IsVerifiedBooking { get; set; }

        [BsonElement("moderationStatus")]
        public string ModerationStatus { get; set; } = "Pending";

        [BsonElement("moderatedAt")]
        public DateTime? ModeratedAt { get; set; }

        [BsonElement("moderatorName")]
        public string ModeratorName { get; set; } = string.Empty;
    }
}

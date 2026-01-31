using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace VietVoyage.Domain.Entities
{
    public class Booking
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [Required]
        [BsonRepresentation(BsonType.ObjectId)]
        [BsonElement("tour_id")]
        public string TourId { get; set; }

        [BsonIgnore]
        public Tour Tour { get; set; }

        [Required]
        [BsonRepresentation(BsonType.ObjectId)]
        [BsonElement("customer_id")]
        public string CustomerId { get; set; }

        [BsonIgnore]
        public Customer Customer { get; set; }

        [Required]
        [BsonElement("booking_date")]
        public DateTime BookingDate { get; set; } = DateTime.UtcNow;

        [Required]
        [BsonElement("total_amount")]
        public decimal TotalAmount { get; set; }

        [Required]
        [BsonElement("status")]
        public string Status { get; set; } // Pending, Confirmed, Cancelled, Completed, Refunded

        [BsonElement("participants_count")]
        public int ParticipantsCount { get; set; }
    }
}

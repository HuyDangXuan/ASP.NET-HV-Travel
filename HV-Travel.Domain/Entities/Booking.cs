using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace HVTravel.Domain.Entities
{

    public class Booking
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("booking_code")]
        public string BookingCode { get; set; }

        [Required]
        [BsonRepresentation(BsonType.ObjectId)]
        [BsonElement("tour_id")]
        public string TourId { get; set; }

        [BsonElement("tour_snapshot")]
        public TourSnapshot TourSnapshot { get; set; }

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
        public string Status { get; set; } // Pending, Paid, Confirmed, Completed, Cancelled, Refunded

        [BsonElement("payment_status")]
        public string PaymentStatus { get; set; } = "Unpaid"; // Unpaid, Partial, Full, Refunded

        [BsonElement("participants_count")]
        public int ParticipantsCount { get; set; }

        [BsonElement("passengers")]
        public List<Passenger> Passengers { get; set; } = new List<Passenger>();

        [BsonElement("contact_info")]
        public ContactInfo ContactInfo { get; set; }

        [BsonElement("notes")]
        public string Notes { get; set; }

        [BsonElement("history_log")]
        public List<BookingHistoryLog> HistoryLog { get; set; } = new List<BookingHistoryLog>();

        [BsonElement("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    public class TourSnapshot
    {
        [BsonElement("code")]
        public string Code { get; set; }
        
        [BsonElement("name")]
        public string Name { get; set; }
        
        [BsonElement("start_date")]
        public DateTime StartDate { get; set; }
        
        [BsonElement("duration")]
        public string Duration { get; set; }
    }

    public class Passenger
    {
        [BsonElement("full_name")]
        public string FullName { get; set; }
        
        [BsonElement("birth_date")]
        public DateTime? BirthDate { get; set; }
        
        [BsonElement("type")]
        public string Type { get; set; } // Adult, Child, Infant
        
        [BsonElement("gender")]
        public string Gender { get; set; }
        
        [BsonElement("passport_number")]
        public string PassportNumber { get; set; }
    }

    public class ContactInfo
    {
        [BsonElement("name")]
        public string Name { get; set; }
        
        [BsonElement("email")]
        public string Email { get; set; }
        
        [BsonElement("phone")]
        public string Phone { get; set; }
    }

    public class BookingHistoryLog
    {
        [BsonElement("action")]
        public string Action { get; set; }
        
        [BsonElement("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        [BsonElement("user")]
        public string User { get; set; }
        
        [BsonElement("note")]
        public string Note { get; set; }
    }
}


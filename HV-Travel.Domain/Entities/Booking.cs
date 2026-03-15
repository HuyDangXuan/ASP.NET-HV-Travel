using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace HVTravel.Domain.Entities
{

    [BsonIgnoreExtraElements]
    public class Booking
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("bookingCode")]
        public string BookingCode { get; set; }

        [Required]
        [BsonRepresentation(BsonType.ObjectId)]
        [BsonElement("tourId")]
        public string TourId { get; set; }

        [BsonElement("tourSnapshot")]
        public TourSnapshot TourSnapshot { get; set; }

        [BsonIgnore]
        public Tour Tour { get; set; }

        [Required]
        [BsonRepresentation(BsonType.ObjectId)]
        [BsonElement("customerId")]
        public string CustomerId { get; set; }

        [BsonIgnore]
        public Customer Customer { get; set; }

        [Required]
        [BsonElement("bookingDate")]
        public DateTime BookingDate { get; set; } = DateTime.UtcNow;

        [Required]
        [BsonElement("totalAmount")]
        public decimal TotalAmount { get; set; }

        [Required]
        [BsonElement("status")]
        public string Status { get; set; } // Pending, Paid, Confirmed, Completed, Cancelled, Refunded

        [BsonElement("paymentStatus")]
        public string PaymentStatus { get; set; } = "Unpaid"; // Unpaid, Partial, Full, Refunded

        [BsonElement("participantsCount")]
        public int ParticipantsCount { get; set; }

        [BsonElement("passengers")]
        public List<Passenger> Passengers { get; set; } = new List<Passenger>();

        [BsonElement("contactInfo")]
        public ContactInfo ContactInfo { get; set; }

        [BsonElement("notes")]
        public string Notes { get; set; }

        [BsonElement("historyLog")]
        public List<BookingHistoryLog> HistoryLog { get; set; } = new List<BookingHistoryLog>();

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("updatedAt")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("isDeleted")]
        public bool IsDeleted { get; set; } = false;

        [BsonElement("deletedBy")]
        public string? DeletedBy { get; set; }

        [BsonElement("deletedAt")]
        public DateTime? DeletedAt { get; set; }
    }

    [BsonIgnoreExtraElements]
    public class TourSnapshot
    {
        [BsonElement("code")]
        public string Code { get; set; }
        
        [BsonElement("name")]
        public string Name { get; set; }
        
        [BsonElement("startDate")]
        public DateTime? StartDate { get; set; }
        
        [BsonElement("duration")]
        public string Duration { get; set; }
    }

    [BsonIgnoreExtraElements]
    public class Passenger
    {
        [BsonElement("fullName")]
        public string FullName { get; set; }
        
        [BsonElement("birthDate")]
        public DateTime? BirthDate { get; set; }
        
        [BsonElement("type")]
        public string Type { get; set; } // Adult, Child, Infant
        
        [BsonElement("gender")]
        public string Gender { get; set; }
        
        [BsonElement("passportNumber")]
        public string PassportNumber { get; set; }
    }

    [BsonIgnoreExtraElements]
    public class ContactInfo
    {
        [BsonElement("name")]
        public string Name { get; set; }
        
        [BsonElement("email")]
        public string Email { get; set; }
        
        [BsonElement("phone")]
        public string Phone { get; set; }
    }

    [BsonIgnoreExtraElements]
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


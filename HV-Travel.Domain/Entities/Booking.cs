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
        public string Status { get; set; } = "Pending"; // Pending, PendingPayment, Paid, Confirmed, Completed, Cancelled, Refunded

        [BsonElement("paymentStatus")]
        public string PaymentStatus { get; set; } = "Unpaid"; // Unpaid, Pending, Partial, Paid, Full, Failed, Refunded

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

        [BsonElement("events")]
        public List<BookingEvent> Events { get; set; } = new List<BookingEvent>();

        [BsonElement("paymentTransactions")]
        public List<PaymentTransaction> PaymentTransactions { get; set; } = new List<PaymentTransaction>();

        [BsonElement("cancellationRequest")]
        public CancellationRequest? CancellationRequest { get; set; }

        [BsonElement("publicLookupEnabled")]
        public bool PublicLookupEnabled { get; set; } = true;

        [BsonElement("receiptNumber")]
        public string? ReceiptNumber { get; set; }

        [BsonElement("transferProofFileName")]
        public string? TransferProofFileName { get; set; }

        [BsonElement("transferProofContentType")]
        public string? TransferProofContentType { get; set; }

        [BsonElement("transferProofBase64")]
        public string? TransferProofBase64 { get; set; }

        [BsonElement("confirmedAt")]
        public DateTime? ConfirmedAt { get; set; }

        [BsonElement("completedAt")]
        public DateTime? CompletedAt { get; set; }

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
        public string Type { get; set; }

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

    [BsonIgnoreExtraElements]
    public class BookingEvent
    {
        [BsonElement("type")]
        public string Type { get; set; } = string.Empty;

        [BsonElement("title")]
        public string Title { get; set; } = string.Empty;

        [BsonElement("description")]
        public string Description { get; set; } = string.Empty;

        [BsonElement("occurredAt")]
        public DateTime OccurredAt { get; set; } = DateTime.UtcNow;

        [BsonElement("actor")]
        public string Actor { get; set; } = string.Empty;

        [BsonElement("visibleToCustomer")]
        public bool VisibleToCustomer { get; set; } = true;
    }

    [BsonIgnoreExtraElements]
    public class PaymentTransaction
    {
        [BsonElement("provider")]
        public string Provider { get; set; } = string.Empty;

        [BsonElement("method")]
        public string Method { get; set; } = string.Empty;

        [BsonElement("transactionId")]
        public string TransactionId { get; set; } = string.Empty;

        [BsonElement("reference")]
        public string Reference { get; set; } = string.Empty;

        [BsonElement("amount")]
        public decimal Amount { get; set; }

        [BsonElement("status")]
        public string Status { get; set; } = string.Empty;

        [BsonElement("receivedFromWebhook")]
        public bool ReceivedFromWebhook { get; set; }

        [BsonElement("payloadHash")]
        public string PayloadHash { get; set; } = string.Empty;

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("processedAt")]
        public DateTime? ProcessedAt { get; set; }
    }

    [BsonIgnoreExtraElements]
    public class CancellationRequest
    {
        [BsonElement("status")]
        public string Status { get; set; } = "None";

        [BsonElement("reason")]
        public string Reason { get; set; } = string.Empty;

        [BsonElement("requestedAt")]
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("requestedBy")]
        public string RequestedBy { get; set; } = string.Empty;

        [BsonElement("processedAt")]
        public DateTime? ProcessedAt { get; set; }

        [BsonElement("resolutionNote")]
        public string ResolutionNote { get; set; } = string.Empty;
    }
}

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace VietVoyage.Domain.Entities
{
    public class Payment
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [Required]
        [BsonRepresentation(BsonType.ObjectId)]
        [BsonElement("booking_id")]
        public string BookingId { get; set; }

        [Required]
        [BsonElement("amount")]
        public decimal Amount { get; set; }

        [Required]
        [BsonElement("payment_method")]
        public string PaymentMethod { get; set; } // CreditCard, BankTransfer, Cash

        [BsonElement("transaction_id")]
        public string TransactionId { get; set; }

        [Required]
        [BsonElement("payment_date")]
        public DateTime PaymentDate { get; set; } = DateTime.UtcNow;

        [Required]
        [BsonElement("status")]
        public string Status { get; set; } // Success, Failed, Pending
    }
}

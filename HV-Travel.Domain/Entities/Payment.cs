using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace HVTravel.Domain.Entities
{
    [BsonIgnoreExtraElements]
    public class Payment
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [Required]
        [BsonRepresentation(BsonType.ObjectId)]
        [BsonElement("bookingId")]
        public string BookingId { get; set; }

        [Required]
        [BsonElement("amount")]
        public decimal Amount { get; set; }

        [Required]
        [BsonElement("paymentMethod")]
        public string PaymentMethod { get; set; } // CreditCard, BankTransfer, Cash

        [BsonElement("transactionId")]
        public string TransactionId { get; set; }

        [Required]
        [BsonElement("paymentDate")]
        public DateTime PaymentDate { get; set; } = DateTime.UtcNow;

        [Required]
        [BsonElement("status")]
        public string Status { get; set; } // Success, Failed, Pending
    }
}

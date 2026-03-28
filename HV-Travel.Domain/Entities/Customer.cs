using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace HVTravel.Domain.Entities
{
    [BsonIgnoreExtraElements]
    public class Customer
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("customerCode")]
        public string CustomerCode { get; set; }

        [Required]
        [BsonElement("fullName")]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [BsonElement("email")]
        public string Email { get; set; } = string.Empty;

        [BsonElement("password")]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [Phone]
        [BsonElement("phoneNumber")]
        public string PhoneNumber { get; set; } = string.Empty;

        [BsonElement("avatarUrl")]
        public string AvatarUrl { get; set; }

        [BsonElement("address")]
        public Address Address { get; set; }

        [BsonElement("notes")]
        public string Notes { get; set; }

        [BsonElement("segment")]
        public string Segment { get; set; } = "Standard"; // VIP, New, Standard, ChurnRisk, Inactive

        [BsonElement("status")]
        public string Status { get; set; } = "Active"; // Active, Banned

        [BsonElement("emailVerified")]
        public bool EmailVerified { get; set; }

        [BsonElement("tokenVersion")]
        public int TokenVersion { get; set; }

        [BsonElement("stats")]
        public CustomerStats Stats { get; set; } = new CustomerStats();

        [BsonElement("tags")]
        public List<string> Tags { get; set; } = new List<string>();

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("updatedAt")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    [BsonIgnoreExtraElements]
    public class Address
    {
        [BsonElement("street")]
        public string Street { get; set; }

        [BsonElement("city")]
        public string City { get; set; }

        [BsonElement("country")]
        public string Country { get; set; }
    }

    [BsonIgnoreExtraElements]
    public class CustomerStats
    {
        [BsonElement("lifetimeSpend")]
        public decimal LifetimeSpend { get; set; }

        [BsonElement("tripCount")]
        public int TripCount { get; set; }

        [BsonElement("loyaltyPoints")]
        public int LoyaltyPoints { get; set; }

        [BsonElement("pendingPoints")]
        public int PendingPoints { get; set; }

        [BsonElement("tier")]
        public string Tier { get; set; } = "Explorer";

        [BsonElement("referralCode")]
        public string ReferralCode { get; set; } = string.Empty;

        [BsonElement("voucherBalance")]
        public int VoucherBalance { get; set; }

        [BsonElement("lastActivity")]
        public DateTime LastActivity { get; set; } = DateTime.UtcNow;

        [BsonElement("lastCompletedTripAt")]
        public DateTime? LastCompletedTripAt { get; set; }

        [BsonIgnore]
        public decimal TotalSpending
        {
            get => LifetimeSpend;
            set => LifetimeSpend = value;
        }

        [BsonIgnore]
        public int TotalOrders
        {
            get => TripCount;
            set => TripCount = value;
        }
    }
}

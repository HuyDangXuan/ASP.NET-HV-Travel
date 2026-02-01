using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace HVTravel.Domain.Entities
{

    public class Customer
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("customer_code")]
        public string CustomerCode { get; set; }

        [Required]
        [BsonElement("full_name")]
        public string FullName { get; set; }

        [Required]
        [EmailAddress]
        [BsonElement("email")]
        public string Email { get; set; }

        [Required]
        [Phone]
        [BsonElement("phone_number")]
        public string PhoneNumber { get; set; }
        
        [BsonElement("avatar_url")]
        public string AvatarUrl { get; set; }

        [BsonElement("address")]
        public Address Address { get; set; }

        [BsonElement("notes")]
        public string Notes { get; set; }

        [BsonElement("segment")]
        public string Segment { get; set; } = "Standard"; // VIP, New, Standard, ChurnRisk, Inactive
        
        [BsonElement("status")]
        public string Status { get; set; } = "Active"; // Active, Banned

        [BsonElement("stats")]
        public CustomerStats Stats { get; set; } = new CustomerStats();
        
        [BsonElement("tags")]
        public List<string> Tags { get; set; } = new List<string>();

        [BsonElement("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        [BsonElement("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    public class Address
    {
        [BsonElement("street")]
        public string Street { get; set; }
        
        [BsonElement("city")]
        public string City { get; set; }
        
        [BsonElement("country")]
        public string Country { get; set; }
    }

    public class CustomerStats
    {
        [BsonElement("total_spending")]
        public decimal TotalSpending { get; set; }
        
        [BsonElement("total_orders")]
        public int TotalOrders { get; set; }
        
        [BsonElement("loyalty_points")]
        public int LoyaltyPoints { get; set; }
        
        [BsonElement("last_activity")]
        public DateTime LastActivity { get; set; } = DateTime.UtcNow;
    }
}


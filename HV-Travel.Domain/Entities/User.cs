using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace HVTravel.Domain.Entities
{

    public class User
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [Required]
        [BsonElement("email")]
        public string Email { get; set; }

        [Required]
        [BsonElement("password_hash")]
        public string PasswordHash { get; set; }

        [Required]
        [BsonElement("role")]
        public string Role { get; set; } = "Client"; // Admin, Manager, Staff, Guide, Client

        [BsonElement("full_name")]
        public string FullName { get; set; }
        
        [BsonElement("avatar_url")]
        public string AvatarUrl { get; set; }
        
        [BsonElement("status")]
        public string Status { get; set; } = "Active"; // Active, Inactive

        [BsonElement("last_login")]
        public DateTime? LastLogin { get; set; }
        
        [BsonElement("permissions")]
        public List<string> Permissions { get; set; } = new List<string>();

        [BsonElement("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}


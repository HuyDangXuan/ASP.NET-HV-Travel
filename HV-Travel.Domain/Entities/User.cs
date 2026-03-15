using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace HVTravel.Domain.Entities
{

    [BsonIgnoreExtraElements]
    public class User
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [Required]
        [BsonElement("email")]
        public string Email { get; set; }

        [Required]
        [BsonElement("passwordHash")]
        public string PasswordHash { get; set; }

        [Required]
        [BsonElement("role")]
        public string Role { get; set; } = "Client"; // Admin, Manager, Staff, Guide, Client

        [BsonElement("fullName")]
        public string FullName { get; set; }
        
        [BsonElement("avatarUrl")]
        public string AvatarUrl { get; set; }
        
        [BsonElement("status")]
        public string Status { get; set; } = "Active"; // Active, Inactive

        [BsonElement("lastLogin")]
        public DateTime? LastLogin { get; set; }
        
        [BsonElement("permissions")]
        public List<string> Permissions { get; set; } = new List<string>();

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}


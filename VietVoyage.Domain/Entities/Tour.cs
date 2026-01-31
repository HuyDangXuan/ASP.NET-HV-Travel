using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace VietVoyage.Domain.Entities
{
    public class Tour
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [Required]
        [BsonElement("name")]
        public string Name { get; set; }

        [Required]
        [BsonElement("description")]
        public string Description { get; set; }

        [Required]
        [BsonElement("price")]
        public decimal Price { get; set; }

        [Required]
        [BsonElement("duration")]
        public string Duration { get; set; }

        [Required]
        [BsonElement("image_url")]
        public string ImageUrl { get; set; }

        [Required]
        [BsonElement("city")]
        public string City { get; set; }

        [Required]
        [BsonElement("category")]
        public string Category { get; set; } // e.g., Adventure, Culture, Food, Nature

        [BsonElement("start_date")]
        public DateTime StartDate { get; set; }

        [BsonElement("max_participants")]
        public int MaxParticipants { get; set; }

        [BsonElement("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("status")]
        public string Status { get; set; } = "Active"; // Active, Inactive, SoldOut
    }
}

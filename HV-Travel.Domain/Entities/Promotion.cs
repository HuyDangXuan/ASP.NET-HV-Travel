using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace HVTravel.Domain.Entities
{
    public class Promotion
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [Required]
        [BsonElement("code")]
        public string Code { get; set; }

        [Required]
        [BsonElement("discount_percentage")]
        public double DiscountPercentage { get; set; }

        [BsonElement("description")]
        public string Description { get; set; }

        [BsonElement("valid_from")]
        public DateTime ValidFrom { get; set; }

        [BsonElement("valid_to")]
        public DateTime ValidTo { get; set; }

        [BsonElement("is_active")]
        public bool IsActive { get; set; } = true;
    }
}

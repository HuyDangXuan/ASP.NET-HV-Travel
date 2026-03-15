using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace HVTravel.Domain.Entities
{
    [BsonIgnoreExtraElements]
    public class Promotion
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [Required]
        [BsonElement("code")]
        public string Code { get; set; }

        [Required]
        [BsonElement("discountPercentage")]
        public double DiscountPercentage { get; set; }

        [BsonElement("description")]
        public string Description { get; set; }

        [BsonElement("validFrom")]
        public DateTime ValidFrom { get; set; }

        [BsonElement("validTo")]
        public DateTime ValidTo { get; set; }

        [BsonElement("isActive")]
        public bool IsActive { get; set; } = true;
    }
}

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace HVTravel.Domain.Entities
{
    [BsonIgnoreExtraElements]
    public class Tour
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("code")]
        public string Code { get; set; }

        [Required]
        [BsonElement("name")]
        public string Name { get; set; }

        [Required]
        [BsonElement("description")]
        public string Description { get; set; }
        
        [BsonElement("shortDescription")]
        public string ShortDescription { get; set; }

        [BsonElement("destination")]
        public Destination Destination { get; set; }

        [BsonElement("images")]
        public List<string> Images { get; set; } = new List<string>();

        [Required]
        [BsonElement("price")]
        public TourPrice Price { get; set; }

        [Required]
        [BsonElement("duration")]
        public TourDuration Duration { get; set; }

        [BsonElement("startDates")]
        public List<DateTime> StartDates { get; set; } = new List<DateTime>();

        [BsonElement("schedule")]
        public List<ScheduleItem> Schedule { get; set; } = new List<ScheduleItem>();

        [BsonElement("generatedInclusions")]
        public List<string> GeneratedInclusions { get; set; } = new List<string>();

        [BsonElement("generatedExclusions")]
        public List<string> GeneratedExclusions { get; set; } = new List<string>();

        [BsonElement("maxParticipants")]
        public int MaxParticipants { get; set; }

        [BsonElement("currentParticipants")]
        public int CurrentParticipants { get; set; }

        public int RemainingSpots => MaxParticipants - CurrentParticipants;

        [BsonElement("rating")]
        public double Rating { get; set; }

        [BsonElement("reviewCount")]
        public int ReviewCount { get; set; }

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        [BsonElement("updatedAt")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("version")]
        public uint Version { get; set; } = 0;

        [BsonElement("status")]
        public string Status { get; set; } = "Active"; // Active, Inactive, SoldOut, ComingSoon
    }

    [BsonIgnoreExtraElements]
    public class Destination
    {
        [BsonElement("city")]
        public string City { get; set; }
        
        [BsonElement("country")]
        public string Country { get; set; }
        
        [BsonElement("region")]
        public string Region { get; set; }
    }

    [BsonIgnoreExtraElements]
    public class TourPrice
    {
        [BsonElement("adult")]
        public decimal Adult { get; set; }
        
        [BsonElement("child")]
        public decimal Child { get; set; }
        
        [BsonElement("infant")]
        public decimal Infant { get; set; }
        
        [BsonElement("currency")]
        public string Currency { get; set; } = "VND";

        [BsonElement("discount")]
        public double Discount { get; set; }
    }

    [BsonIgnoreExtraElements]
    public class TourDuration
    {
        [BsonElement("days")]
        public int Days { get; set; }
        
        [BsonElement("nights")]
        public int Nights { get; set; }
        
        [BsonElement("text")]
        public string Text { get; set; } // e.g., "3 Days 2 Nights"
    }

    [BsonIgnoreExtraElements]
    public class ScheduleItem
    {
        [BsonElement("day")]
        public int Day { get; set; }
        
        [BsonElement("title")]
        public string Title { get; set; }
        
        [BsonElement("description")]
        public string Description { get; set; }
        
        [BsonElement("activities")]
        public List<string> Activities { get; set; } = new List<string>();
    }
}

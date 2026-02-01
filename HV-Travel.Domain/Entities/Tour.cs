using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace HVTravel.Domain.Entities
{
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
        
        [BsonElement("short_description")]
        public string ShortDescription { get; set; }

        [Required]
        [BsonElement("category")]
        public string Category { get; set; } // e.g., Adventure, Culture, Food, Nature

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

        [BsonElement("start_dates")]
        public List<DateTime> StartDates { get; set; } = new List<DateTime>();

        [BsonElement("schedule")]
        public List<ScheduleItem> Schedule { get; set; } = new List<ScheduleItem>();

        [BsonElement("generated_inclusions")]
        public List<string> GeneratedInclusions { get; set; } = new List<string>();

        [BsonElement("generated_exclusions")]
        public List<string> GeneratedExclusions { get; set; } = new List<string>();

        [BsonElement("max_participants")]
        public int MaxParticipants { get; set; }

        [BsonElement("current_participants")]
        public int CurrentParticipants { get; set; }

        [BsonElement("rating")]
        public double Rating { get; set; }

        [BsonElement("review_count")]
        public int ReviewCount { get; set; }

        [BsonElement("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        [BsonElement("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("status")]
        public string Status { get; set; } = "Active"; // Active, Inactive, SoldOut, ComingSoon
    }

    public class Destination
    {
        [BsonElement("city")]
        public string City { get; set; }
        
        [BsonElement("country")]
        public string Country { get; set; }
        
        [BsonElement("region")]
        public string Region { get; set; }
    }

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
    }

    public class TourDuration
    {
        [BsonElement("days")]
        public int Days { get; set; }
        
        [BsonElement("nights")]
        public int Nights { get; set; }
        
        [BsonElement("text")]
        public string Text { get; set; } // e.g., "3 Days 2 Nights"
    }

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

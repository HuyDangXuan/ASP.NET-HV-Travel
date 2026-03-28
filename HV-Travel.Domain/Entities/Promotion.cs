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

        [BsonElement("title")]
        public string Title { get; set; } = string.Empty;

        [BsonElement("campaignType")]
        public string CampaignType { get; set; } = "Voucher"; // Voucher, FlashSale, Seasonal, Loyalty

        [Required]
        [BsonElement("discountPercentage")]
        public double DiscountPercentage { get; set; }

        [BsonElement("discountValue")]
        public decimal DiscountValue { get; set; }

        [BsonElement("description")]
        public string Description { get; set; }

        [BsonElement("badgeText")]
        public string BadgeText { get; set; } = string.Empty;

        [BsonElement("campaignScope")]
        public string CampaignScope { get; set; } = "Web";

        [BsonElement("minimumSpend")]
        public decimal MinimumSpend { get; set; }

        [BsonElement("usageLimit")]
        public int UsageLimit { get; set; }

        [BsonElement("usageCount")]
        public int UsageCount { get; set; }

        [BsonElement("eligibleSegments")]
        public List<string> EligibleSegments { get; set; } = new List<string>();

        [BsonElement("applicableDestinations")]
        public List<string> ApplicableDestinations { get; set; } = new List<string>();

        [BsonElement("terms")]
        public string Terms { get; set; } = string.Empty;

        [BsonElement("imageUrl")]
        public string ImageUrl { get; set; } = string.Empty;

        [BsonElement("priority")]
        public int Priority { get; set; }

        [BsonElement("highlightPriceText")]
        public string HighlightPriceText { get; set; } = string.Empty;

        [BsonElement("isFlashSale")]
        public bool IsFlashSale { get; set; }

        [BsonElement("validFrom")]
        public DateTime ValidFrom { get; set; }

        [BsonElement("validTo")]
        public DateTime ValidTo { get; set; }

        [BsonElement("isActive")]
        public bool IsActive { get; set; } = true;
    }
}

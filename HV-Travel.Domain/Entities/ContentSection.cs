using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HVTravel.Domain.Entities;

[BsonIgnoreExtraElements]
public class ContentSection
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("pageKey")]
    public string PageKey { get; set; } = string.Empty;

    [BsonElement("sectionKey")]
    public string SectionKey { get; set; } = string.Empty;

    [BsonElement("title")]
    public string Title { get; set; } = string.Empty;

    [BsonElement("description")]
    public string Description { get; set; } = string.Empty;

    [BsonElement("isEnabled")]
    public bool IsEnabled { get; set; } = true;

    [BsonElement("isPublished")]
    public bool IsPublished { get; set; } = true;

    [BsonElement("displayOrder")]
    public int DisplayOrder { get; set; }

    [BsonElement("fields")]
    public List<ContentField> Fields { get; set; } = new();

    [BsonElement("presentation")]
    public ContentSectionPresentation Presentation { get; set; } = ContentPresentationDefaults.CreateSectionPresentation();

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

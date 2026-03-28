using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HVTravel.Domain.Entities;

[BsonIgnoreExtraElements]
public class SiteSettings
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("settingsKey")]
    public string SettingsKey { get; set; } = "default";

    [BsonElement("groups")]
    public List<SiteSettingsGroup> Groups { get; set; } = new();

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

[BsonIgnoreExtraElements]
public class SiteSettingsGroup
{
    [BsonElement("groupKey")]
    public string GroupKey { get; set; } = string.Empty;

    [BsonElement("title")]
    public string Title { get; set; } = string.Empty;

    [BsonElement("description")]
    public string Description { get; set; } = string.Empty;

    [BsonElement("isEnabled")]
    public bool IsEnabled { get; set; } = true;

    [BsonElement("displayOrder")]
    public int DisplayOrder { get; set; }

    [BsonElement("fields")]
    public List<ContentField> Fields { get; set; } = new();
}

[BsonIgnoreExtraElements]
public class ContentField
{
    [BsonElement("key")]
    public string Key { get; set; } = string.Empty;

    [BsonElement("label")]
    public string Label { get; set; } = string.Empty;

    [BsonElement("fieldType")]
    public string FieldType { get; set; } = "text";

    [BsonElement("value")]
    public string Value { get; set; } = string.Empty;

    [BsonElement("placeholder")]
    public string Placeholder { get; set; } = string.Empty;

    [BsonElement("style")]
    public ContentTextStyle Style { get; set; } = ContentPresentationDefaults.CreateTextStyle();
}

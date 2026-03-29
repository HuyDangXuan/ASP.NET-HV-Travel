using MongoDB.Bson.Serialization.Attributes;

namespace HVTravel.Domain.Entities;

[BsonIgnoreExtraElements]
public class ContentSectionPresentation
{
    [BsonElement("container")]
    public ContentContainerStyle Container { get; set; } = new();

    [BsonElement("eyebrowText")]
    public ContentTextStyle EyebrowText { get; set; } = new();

    [BsonElement("titleText")]
    public ContentTextStyle TitleText { get; set; } = new();

    [BsonElement("descriptionText")]
    public ContentTextStyle DescriptionText { get; set; } = new();
}

[BsonIgnoreExtraElements]
public class ContentContainerStyle
{
    [BsonElement("align")]
    public string Align { get; set; } = string.Empty;

    [BsonElement("backgroundPreset")]
    public string BackgroundPreset { get; set; } = "default";

    [BsonElement("customBackgroundHex")]
    public string CustomBackgroundHex { get; set; } = string.Empty;
}

[BsonIgnoreExtraElements]
public class ContentTextStyle
{
    [BsonElement("align")]
    public string Align { get; set; } = string.Empty;

    [BsonElement("fontPreset")]
    public string FontPreset { get; set; } = string.Empty;

    [BsonElement("customFontFamily")]
    public string CustomFontFamily { get; set; } = string.Empty;

    [BsonElement("sizePreset")]
    public string SizePreset { get; set; } = string.Empty;

    [BsonElement("customSizeValue")]
    public decimal? CustomSizeValue { get; set; }

    [BsonElement("customSizeUnit")]
    public string CustomSizeUnit { get; set; } = string.Empty;

    [BsonElement("colorPreset")]
    public string ColorPreset { get; set; } = "default";

    [BsonElement("customColorHex")]
    public string CustomColorHex { get; set; } = string.Empty;
}

public static class ContentPresentationDefaults
{
    public static ContentSectionPresentation CreateSectionPresentation()
    {
        return new ContentSectionPresentation
        {
            Container = CreateContainerStyle(),
            EyebrowText = CreateTextStyle(),
            TitleText = CreateTextStyle(),
            DescriptionText = CreateTextStyle()
        };
    }

    public static ContentContainerStyle CreateContainerStyle()
    {
        return new ContentContainerStyle
        {
            Align = string.Empty,
            BackgroundPreset = "default",
            CustomBackgroundHex = string.Empty
        };
    }

    public static ContentTextStyle CreateTextStyle()
    {
        return new ContentTextStyle
        {
            Align = string.Empty,
            FontPreset = string.Empty,
            CustomFontFamily = string.Empty,
            SizePreset = string.Empty,
            CustomSizeValue = null,
            CustomSizeUnit = string.Empty,
            ColorPreset = "default",
            CustomColorHex = string.Empty
        };
    }

    public static void EnsureSection(ContentSection section)
    {
        section.Presentation = CloneSectionPresentation(section.Presentation);

        foreach (var field in section.Fields)
        {
            EnsureField(field);
        }
    }

    public static void EnsureField(ContentField field)
    {
        field.Style = CloneTextStyle(field.Style);
    }

    public static ContentSectionPresentation CloneSectionPresentation(ContentSectionPresentation? source)
    {
        return new ContentSectionPresentation
        {
            Container = CloneContainerStyle(source?.Container),
            EyebrowText = CloneTextStyle(source?.EyebrowText),
            TitleText = CloneTextStyle(source?.TitleText),
            DescriptionText = CloneTextStyle(source?.DescriptionText)
        };
    }

    public static ContentContainerStyle CloneContainerStyle(ContentContainerStyle? source)
    {
        return new ContentContainerStyle
        {
            Align = source?.Align ?? string.Empty,
            BackgroundPreset = source?.BackgroundPreset ?? "default",
            CustomBackgroundHex = source?.CustomBackgroundHex ?? string.Empty
        };
    }

    public static ContentTextStyle CloneTextStyle(ContentTextStyle? source)
    {
        return new ContentTextStyle
        {
            Align = source?.Align ?? string.Empty,
            FontPreset = source?.FontPreset ?? string.Empty,
            CustomFontFamily = source?.CustomFontFamily ?? string.Empty,
            SizePreset = source?.SizePreset ?? string.Empty,
            CustomSizeValue = source?.CustomSizeValue,
            CustomSizeUnit = source?.CustomSizeUnit ?? string.Empty,
            ColorPreset = source?.ColorPreset ?? "default",
            CustomColorHex = source?.CustomColorHex ?? string.Empty
        };
    }
}

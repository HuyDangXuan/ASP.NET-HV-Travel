using MongoDB.Bson.Serialization.Attributes;

namespace HVTravel.Domain.Entities;

public partial class Tour
{
    [BsonElement("routing")]
    public TourRouting? Routing { get; set; }
}

[BsonIgnoreExtraElements]
public class TourRouting
{
    [BsonElement("schemaVersion")]
    public int SchemaVersion { get; set; } = 1;

    [BsonElement("stops")]
    public List<TourRouteStop> Stops { get; set; } = new();
}

[BsonIgnoreExtraElements]
public class TourRouteStop
{
    [BsonElement("id")]
    public string Id { get; set; } = string.Empty;

    [BsonElement("day")]
    public int Day { get; set; }

    [BsonElement("order")]
    public int Order { get; set; }

    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("type")]
    public string Type { get; set; } = string.Empty;

    [BsonElement("coordinates")]
    public GeoPoint Coordinates { get; set; } = new();

    [BsonElement("visitMinutes")]
    public int VisitMinutes { get; set; }

    [BsonElement("attractionScore")]
    public double AttractionScore { get; set; }

    [BsonElement("note")]
    public string Note { get; set; } = string.Empty;
}

[BsonIgnoreExtraElements]
public class GeoPoint
{
    [BsonElement("lat")]
    public double Lat { get; set; }

    [BsonElement("lng")]
    public double Lng { get; set; }
}

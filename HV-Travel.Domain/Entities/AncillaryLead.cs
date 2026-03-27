using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HVTravel.Domain.Entities;

[BsonIgnoreExtraElements]
public class AncillaryLead
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonRepresentation(BsonType.ObjectId)]
    [BsonElement("customerId")]
    public string CustomerId { get; set; } = string.Empty;

    [BsonElement("serviceType")]
    public string ServiceType { get; set; } = string.Empty;

    [BsonElement("fullName")]
    public string FullName { get; set; } = string.Empty;

    [BsonElement("email")]
    public string Email { get; set; } = string.Empty;

    [BsonElement("phone")]
    public string Phone { get; set; } = string.Empty;

    [BsonElement("destination")]
    public string Destination { get; set; } = string.Empty;

    [BsonElement("departureDate")]
    public DateTime? DepartureDate { get; set; }

    [BsonElement("returnDate")]
    public DateTime? ReturnDate { get; set; }

    [BsonElement("travellersCount")]
    public int TravellersCount { get; set; }

    [BsonElement("budgetText")]
    public string BudgetText { get; set; } = string.Empty;

    [BsonElement("requestNote")]
    public string RequestNote { get; set; } = string.Empty;

    [BsonElement("status")]
    public string Status { get; set; } = "New";

    [BsonElement("quoteStatus")]
    public string QuoteStatus { get; set; } = "Open";

    [BsonElement("assignedTo")]
    public string AssignedTo { get; set; } = string.Empty;

    [BsonElement("source")]
    public string Source { get; set; } = "Website";

    [BsonElement("slaDueAt")]
    public DateTime SlaDueAt { get; set; } = DateTime.UtcNow.AddHours(8);

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

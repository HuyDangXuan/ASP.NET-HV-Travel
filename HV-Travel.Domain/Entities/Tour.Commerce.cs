using MongoDB.Bson.Serialization.Attributes;

namespace HVTravel.Domain.Entities;

public partial class Tour
{
    [BsonElement("slug")]
    public string Slug { get; set; } = string.Empty;

    [BsonElement("seo")]
    public SeoMetadata Seo { get; set; } = new();

    [BsonElement("cancellationPolicy")]
    public TourCancellationPolicy CancellationPolicy { get; set; } = new();

    [BsonElement("confirmationType")]
    public string ConfirmationType { get; set; } = "Instant";

    [BsonElement("highlights")]
    public List<string> Highlights { get; set; } = new();

    [BsonElement("meetingPoint")]
    public string MeetingPoint { get; set; } = string.Empty;

    [BsonElement("supplierRef")]
    public SupplierReference SupplierRef { get; set; } = new();

    [BsonElement("badgeSet")]
    public List<string> BadgeSet { get; set; } = new();

    [BsonElement("departures")]
    public List<TourDeparture> Departures { get; set; } = new();

    [BsonIgnore]
    public IReadOnlyList<TourDeparture> EffectiveDepartures =>
        Departures is { Count: > 0 }
            ? Departures.OrderBy(item => item.StartDate).ToList()
            : (StartDates ?? new List<DateTime>())
                .OrderBy(item => item)
                .Select((date, index) => new TourDeparture
                {
                    Id = string.IsNullOrWhiteSpace(Id) ? $"legacy-{index}" : $"{Id}-legacy-{index}",
                    StartDate = date,
                    AdultPrice = Price?.Adult ?? 0m,
                    ChildPrice = Price?.Child ?? 0m,
                    InfantPrice = Price?.Infant ?? 0m,
                    DiscountPercentage = (decimal)(Price?.Discount ?? 0d),
                    Capacity = MaxParticipants,
                    BookedCount = CurrentParticipants,
                    ConfirmationType = ConfirmationType,
                    Status = Status,
                    CutoffHours = 24
                })
                .ToList();

    public TourDeparture? ResolveDeparture(string? departureId, DateTime? selectedStartDate = null)
    {
        if (!string.IsNullOrWhiteSpace(departureId))
        {
            var byId = EffectiveDepartures.FirstOrDefault(item => string.Equals(item.Id, departureId, StringComparison.Ordinal));
            if (byId != null)
            {
                return byId;
            }
        }

        if (selectedStartDate.HasValue)
        {
            return EffectiveDepartures.FirstOrDefault(item => item.StartDate.Date == selectedStartDate.Value.Date);
        }

        return EffectiveDepartures
            .Where(item => item.StartDate >= DateTime.UtcNow.Date)
            .OrderBy(item => item.StartDate)
            .FirstOrDefault()
            ?? EffectiveDepartures.FirstOrDefault();
    }
}

[BsonIgnoreExtraElements]
public class TourDeparture
{
    [BsonElement("id")]
    public string Id { get; set; } = string.Empty;

    [BsonElement("startDate")]
    public DateTime StartDate { get; set; }

    [BsonElement("adultPrice")]
    public decimal AdultPrice { get; set; }

    [BsonElement("childPrice")]
    public decimal ChildPrice { get; set; }

    [BsonElement("infantPrice")]
    public decimal InfantPrice { get; set; }

    [BsonElement("discountPercentage")]
    public decimal DiscountPercentage { get; set; }

    [BsonElement("capacity")]
    public int Capacity { get; set; }

    [BsonElement("bookedCount")]
    public int BookedCount { get; set; }

    [BsonElement("confirmationType")]
    public string ConfirmationType { get; set; } = "Instant";

    [BsonElement("status")]
    public string Status { get; set; } = "Scheduled";

    [BsonElement("cutoffHours")]
    public int CutoffHours { get; set; } = 24;

    [BsonIgnore]
    public int RemainingCapacity => Math.Max(0, Capacity - BookedCount);
}

[BsonIgnoreExtraElements]
public class SeoMetadata
{
    [BsonElement("title")]
    public string Title { get; set; } = string.Empty;

    [BsonElement("description")]
    public string Description { get; set; } = string.Empty;

    [BsonElement("canonicalPath")]
    public string CanonicalPath { get; set; } = string.Empty;

    [BsonElement("openGraphImageUrl")]
    public string OpenGraphImageUrl { get; set; } = string.Empty;
}

[BsonIgnoreExtraElements]
public class TourCancellationPolicy
{
    [BsonElement("summary")]
    public string Summary { get; set; } = string.Empty;

    [BsonElement("isFreeCancellation")]
    public bool IsFreeCancellation { get; set; }

    [BsonElement("freeCancellationBeforeHours")]
    public int FreeCancellationBeforeHours { get; set; }
}

[BsonIgnoreExtraElements]
public class SupplierReference
{
    [BsonElement("supplierId")]
    public string SupplierId { get; set; } = string.Empty;

    [BsonElement("supplierCode")]
    public string SupplierCode { get; set; } = string.Empty;

    [BsonElement("supplierName")]
    public string SupplierName { get; set; } = string.Empty;

    [BsonElement("sourceSystem")]
    public string SourceSystem { get; set; } = string.Empty;
}


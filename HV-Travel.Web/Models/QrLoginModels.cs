using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HVTravel.Web.Models;

public static class QrLoginStatuses
{
    public const string Pending = "pending";
    public const string Scanned = "scanned";
    public const string Approved = "approved";
    public const string Denied = "denied";
    public const string Expired = "expired";
    public const string Consumed = "consumed";
}

[BsonIgnoreExtraElements]
public class WebQrLoginSession
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("sessionId")]
    public string SessionId { get; set; } = string.Empty;

    [BsonElement("code")]
    public string Code { get; set; } = string.Empty;

    [BsonElement("browserNonce")]
    public string BrowserNonce { get; set; } = string.Empty;

    [BsonElement("status")]
    public string Status { get; set; } = QrLoginStatuses.Pending;

    [BsonElement("customerId")]
    public string? CustomerId { get; set; }

    [BsonElement("returnUrl")]
    public string? ReturnUrl { get; set; }

    [BsonElement("browserLabel")]
    public string BrowserLabel { get; set; } = string.Empty;

    [BsonElement("ipAddress")]
    public string? IpAddress { get; set; }

    [BsonElement("userAgent")]
    public string? UserAgent { get; set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("expiresAt")]
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow;

    [BsonElement("approvedAt")]
    public DateTime? ApprovedAt { get; set; }

    [BsonElement("deniedAt")]
    public DateTime? DeniedAt { get; set; }

    [BsonElement("consumedAt")]
    public DateTime? ConsumedAt { get; set; }

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class CreateQrLoginSessionRequest
{
    public string? ReturnUrl { get; set; }
}

public sealed class CreateQrLoginSessionResponse
{
    public string SessionId { get; set; } = string.Empty;
    public string QrCode { get; set; } = string.Empty;
    public string QrImageUrl { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public int PollIntervalMs { get; set; }
    public string BrowserLabel { get; set; } = string.Empty;
}

public sealed class QrLoginStatusResponse
{
    public string Status { get; set; } = QrLoginStatuses.Pending;
    public DateTime ExpiresAt { get; set; }
    public string? Message { get; set; }
}

public sealed class ResolveQrLoginRequest
{
    public string Code { get; set; } = string.Empty;
}

public sealed class ResolveQrLoginResponse
{
    public string SessionId { get; set; } = string.Empty;
    public string BrowserLabel { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public string Status { get; set; } = QrLoginStatuses.Pending;
}

public sealed class ApproveQrLoginRequest
{
    public string Code { get; set; } = string.Empty;
}

public sealed class ApproveQrLoginResponse
{
    public string Status { get; set; } = QrLoginStatuses.Approved;
    public string SessionId { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}

public sealed class DenyQrLoginRequest
{
    public string Code { get; set; } = string.Empty;
}

public sealed class CompleteQrLoginRequest
{
    public string SessionId { get; set; } = string.Empty;
}

public sealed class CompleteQrLoginResult
{
    public HVTravel.Domain.Entities.Customer Customer { get; set; } = null!;
    public string? ReturnUrl { get; set; }
}

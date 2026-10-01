using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HVTravel.Domain.Entities;
using HVTravel.Domain.Interfaces;
using HVTravel.Infrastructure.Data;
using HVTravel.Web.Models;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;

namespace HVTravel.Web.Services;

public interface IWebQrLoginService
{
    Task<CreateQrLoginSessionResponse> CreateSessionAsync(
        string? returnUrl,
        string browserNonce,
        string browserLabel,
        string? ipAddress,
        string? userAgent);

    Task<QrLoginStatusResponse?> GetStatusAsync(string sessionId);
    Task<CompleteQrLoginResult> CompleteAsync(string sessionId, string browserNonce);
}

public sealed class WebQrLoginService : IWebQrLoginService
{
    private readonly IMongoCollection<WebQrLoginSession> _collection;
    private readonly IRepository<Customer> _customerRepository;
    private readonly TimeSpan _lifetime;
    private readonly int _pollIntervalMs;

    public WebQrLoginService(
        MongoContext mongoContext,
        IConfiguration configuration,
        IRepository<Customer> customerRepository)
    {
        var collectionName =
            configuration.GetValue<string>("HVTravelDatabase:WebQrLoginSessionCollectionName")
            ?? "webQrLoginSessions";

        _collection = mongoContext.GetCollection<WebQrLoginSession>(collectionName);
        _customerRepository = customerRepository;
        _lifetime = TimeSpan.FromSeconds(configuration.GetValue("WebQrLogin:LifetimeSeconds", 90));
        _pollIntervalMs = configuration.GetValue("WebQrLogin:PollIntervalMs", 2000);

    }

    public async Task<CreateQrLoginSessionResponse> CreateSessionAsync(
        string? returnUrl,
        string browserNonce,
        string browserLabel,
        string? ipAddress,
        string? userAgent)
    {
        var now = DateTime.UtcNow;
        var session = new WebQrLoginSession
        {
            SessionId = GenerateToken(16),
            Code = GenerateToken(24),
            BrowserNonce = browserNonce,
            ReturnUrl = returnUrl,
            BrowserLabel = browserLabel,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            Status = QrLoginStatuses.Pending,
            CreatedAt = now,
            UpdatedAt = now,
            ExpiresAt = now.Add(_lifetime)
        };

        await _collection.InsertOneAsync(session);

        var qrCode = $"hvtravel://qr-login?code={Uri.EscapeDataString(session.Code)}";
        return new CreateQrLoginSessionResponse
        {
            SessionId = session.SessionId,
            QrCode = qrCode,
            QrImageUrl = $"https://api.qrserver.com/v1/create-qr-code/?size=220x220&data={Uri.EscapeDataString(qrCode)}",
            ExpiresAt = session.ExpiresAt,
            PollIntervalMs = _pollIntervalMs,
            BrowserLabel = session.BrowserLabel
        };
    }

    public async Task<QrLoginStatusResponse?> GetStatusAsync(string sessionId)
    {
        var session = await _collection.Find(item => item.SessionId == sessionId).FirstOrDefaultAsync();
        if (session == null)
        {
            return null;
        }

        session = await EnsureCurrentStatusAsync(session);
        return new QrLoginStatusResponse
        {
            Status = session.Status,
            ExpiresAt = session.ExpiresAt,
            Message = BuildStatusMessage(session.Status)
        };
    }

    public async Task<CompleteQrLoginResult> CompleteAsync(string sessionId, string browserNonce)
    {
        var session = await _collection.Find(item => item.SessionId == sessionId).FirstOrDefaultAsync();
        if (session == null)
        {
            throw new InvalidOperationException("Không tìm thấy phiên đăng nhập QR.");
        }

        session = await EnsureCurrentStatusAsync(session);
        if (session.Status != QrLoginStatuses.Approved)
        {
            throw new InvalidOperationException("Phiên QR chưa được xác nhận trên ứng dụng.");
        }

        if (!string.Equals(session.BrowserNonce, browserNonce, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Trình duyệt hiện tại không khớp với phiên QR.");
        }

        if (string.IsNullOrWhiteSpace(session.CustomerId))
        {
            throw new InvalidOperationException("Phiên QR chưa gắn với tài khoản khách hàng.");
        }

        var customer = await _customerRepository.GetByIdAsync(session.CustomerId);
        if (customer == null)
        {
            throw new InvalidOperationException("Không tìm thấy khách hàng cho phiên QR này.");
        }

        if (!string.Equals(customer.Status, "Active", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Tài khoản hiện không khả dụng để đăng nhập.");
        }

        session.Status = QrLoginStatuses.Consumed;
        session.ConsumedAt = DateTime.UtcNow;
        session.UpdatedAt = DateTime.UtcNow;
        await ReplaceAsync(session);

        return new CompleteQrLoginResult
        {
            Customer = customer,
            ReturnUrl = session.ReturnUrl
        };
    }

    private async Task<WebQrLoginSession> EnsureCurrentStatusAsync(WebQrLoginSession session)
    {
        if (session.Status is QrLoginStatuses.Consumed or QrLoginStatuses.Denied or QrLoginStatuses.Expired)
        {
            return session;
        }

        if (session.ExpiresAt <= DateTime.UtcNow)
        {
            session.Status = QrLoginStatuses.Expired;
            session.UpdatedAt = DateTime.UtcNow;
            await ReplaceAsync(session);
        }

        return session;
    }

    private async Task ReplaceAsync(WebQrLoginSession session)
    {
        await _collection.ReplaceOneAsync(item => item.Id == session.Id, session);
    }

    private static string BuildStatusMessage(string status)
    {
        return status switch
        {
            QrLoginStatuses.Pending => "Đang chờ quét mã QR từ ứng dụng.",
            QrLoginStatuses.Scanned => "Ứng dụng đã quét mã. Vui lòng xác nhận trên điện thoại.",
            QrLoginStatuses.Approved => "Đăng nhập đã được xác nhận. Đang hoàn tất trên trình duyệt.",
            QrLoginStatuses.Denied => "Yêu cầu đăng nhập đã bị từ chối trên ứng dụng.",
            QrLoginStatuses.Consumed => "Phiên đăng nhập QR đã hoàn tất.",
            _ => "Mã QR đã hết hạn. Vui lòng tạo mã mới."
        };
    }

    private static string GenerateToken(int bytesLength)
    {
        return Convert.ToHexString(RandomNumberGenerator.GetBytes(bytesLength)).ToLowerInvariant();
    }
}

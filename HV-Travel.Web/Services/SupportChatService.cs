using System.Security.Claims;
using System.ComponentModel.DataAnnotations;
using HVTravel.Domain.Entities;
using HVTravel.Domain.Interfaces;
using HVTravel.Web.Models;

namespace HVTravel.Web.Services;

public class SupportChatService : ISupportChatService
{
    private const string TourAiChannel = "tour-ai";
    private readonly IRepository<ChatConversation> _conversationRepository;
    private readonly IRepository<ChatMessage> _messageRepository;

    public SupportChatService(
        IRepository<ChatConversation> conversationRepository,
        IRepository<ChatMessage> messageRepository)
    {
        _conversationRepository = conversationRepository;
        _messageRepository = messageRepository;
    }

    public async Task<ChatConversation> BootstrapConversationAsync(ChatBootstrapRequest request, ClaimsPrincipal user)
    {
        var isCustomer = user.Identity?.IsAuthenticated == true && user.IsInRole("Customer");
        var customerId = isCustomer ? user.FindFirst(ClaimTypes.NameIdentifier)?.Value : null;
        var profile = ResolveProfile(request, user);

        ValidateBootstrapRequest(profile);

        ChatConversation? conversation = null;

        if (isCustomer && !string.IsNullOrWhiteSpace(customerId))
        {
            conversation = (await _conversationRepository.FindAsync(c => c.CustomerId == customerId && c.Status != "closed" && c.Channel != TourAiChannel))
                .OrderByDescending(c => c.UpdatedAt)
                .FirstOrDefault();
        }
        else if (!string.IsNullOrWhiteSpace(request.VisitorSessionId))
        {
            conversation = (await _conversationRepository.FindAsync(c => c.VisitorSessionId == request.VisitorSessionId && c.Status != "closed" && c.Channel != TourAiChannel))
                .OrderByDescending(c => c.UpdatedAt)
                .FirstOrDefault();
        }

        if (conversation == null)
        {
            conversation = new ChatConversation
            {
                ConversationCode = $"CHAT{DateTime.UtcNow:yyyyMMddHHmmss}{Random.Shared.Next(100, 999)}",
                Channel = "web",
                ParticipantType = isCustomer ? "customer" : "guest",
                CustomerId = customerId,
                VisitorSessionId = request.VisitorSessionId,
                GuestProfile = new GuestChatProfile
                {
                    DisplayName = profile.DisplayName,
                    Email = profile.Email,
                    PhoneNumber = profile.PhoneNumber
                },
                SourcePage = string.IsNullOrWhiteSpace(request.SourcePage) ? "/" : request.SourcePage,
                LastMessageAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _conversationRepository.AddAsync(conversation);

            var systemMessage = new ChatMessage
            {
                ConversationId = conversation.Id,
                SenderType = "system",
                SenderDisplayName = "HV Travel",
                MessageType = "system",
                ClientMessageId = CreateClientMessageId(),
                Content = "Cuộc trò chuyện đã được tạo. Đội ngũ HV Travel sẽ phản hồi sớm nhất có thể.",
                SentAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };
            await _messageRepository.AddAsync(systemMessage);
        }
        else
        {
            conversation.SourcePage = string.IsNullOrWhiteSpace(request.SourcePage) ? conversation.SourcePage : request.SourcePage;
            conversation.VisitorSessionId = string.IsNullOrWhiteSpace(request.VisitorSessionId) ? conversation.VisitorSessionId : request.VisitorSessionId;
            conversation.GuestProfile.DisplayName = profile.DisplayName;
            conversation.GuestProfile.Email = profile.Email;
            conversation.GuestProfile.PhoneNumber = profile.PhoneNumber;

            conversation.UpdatedAt = DateTime.UtcNow;
            await _conversationRepository.UpdateAsync(conversation.Id, conversation);
        }

        return conversation;
    }

    public async Task<ChatConversation?> GetConversationAsync(string conversationId)
    {
        return await _conversationRepository.GetByIdAsync(conversationId);
    }

    public async Task<List<ChatMessage>> GetMessagesAsync(string conversationId, int take = 100)
    {
        return (await _messageRepository.FindAsync(m => m.ConversationId == conversationId))
            .OrderBy(m => m.SentAt)
            .TakeLast(take)
            .ToList();
    }

    public async Task<List<ChatConversation>> GetAdminConversationsAsync()
    {
        return (await _conversationRepository.FindAsync(c => c.Channel != TourAiChannel))
            .OrderByDescending(c => c.LastMessageAt)
            .ToList();
    }

    public async Task<ChatMessage> AddPublicMessageAsync(string conversationId, string content, ClaimsPrincipal user)
    {
        var conversation = await RequireConversationAsync(conversationId);
        var isCustomer = user.Identity?.IsAuthenticated == true && user.IsInRole("Customer");
        var displayName = isCustomer
            ? user.FindFirst("FullName")?.Value ?? user.Identity?.Name ?? "Khách hàng"
            : (!string.IsNullOrWhiteSpace(conversation.GuestProfile.DisplayName) ? conversation.GuestProfile.DisplayName : "Khách truy cập");

        var message = new ChatMessage
        {
            ConversationId = conversation.Id,
            SenderType = isCustomer ? "customer" : "guest",
            SenderUserId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value,
            SenderDisplayName = displayName,
            ClientMessageId = CreateClientMessageId(),
            Content = content.Trim(),
            MessageType = "text",
            SentAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        await _messageRepository.AddAsync(message);

        conversation.LastMessagePreview = BuildPreview(message.Content);
        conversation.LastMessageAt = message.SentAt;
        conversation.UpdatedAt = message.SentAt;
        conversation.Status = "waitingStaff";
        conversation.UnreadForAdminCount += 1;
        conversation.UnreadForCustomerCount = 0;
        await _conversationRepository.UpdateAsync(conversation.Id, conversation);

        return message;
    }

    public async Task<ChatMessage> AddAdminMessageAsync(string conversationId, string content, ClaimsPrincipal user)
    {
        var conversation = await RequireConversationAsync(conversationId);
        var senderDisplayName = user.FindFirst("FullName")?.Value
            ?? user.Identity?.Name
            ?? user.FindFirst(ClaimTypes.Name)?.Value
            ?? "Nhân viên HV Travel";

        var message = new ChatMessage
        {
            ConversationId = conversation.Id,
            SenderType = "staff",
            SenderUserId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.Identity?.Name,
            SenderDisplayName = senderDisplayName,
            ClientMessageId = CreateClientMessageId(),
            Content = content.Trim(),
            MessageType = "text",
            SentAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        await _messageRepository.AddAsync(message);

        conversation.AssignedStaffUserId ??= message.SenderUserId;
        conversation.LastMessagePreview = BuildPreview(message.Content);
        conversation.LastMessageAt = message.SentAt;
        conversation.UpdatedAt = message.SentAt;
        conversation.Status = "waitingCustomer";
        conversation.UnreadForCustomerCount += 1;
        conversation.UnreadForAdminCount = 0;
        await _conversationRepository.UpdateAsync(conversation.Id, conversation);

        return message;
    }

    public async Task MarkConversationReadAsync(string conversationId, string readerType)
    {
        var conversation = await RequireConversationAsync(conversationId);
        var messages = await GetMessagesAsync(conversationId);
        var now = DateTime.UtcNow;

        foreach (var message in messages.Where(m => !m.IsRead))
        {
            var shouldMarkRead = readerType switch
            {
                "admin" => message.SenderType is "guest" or "customer",
                "customer" => message.SenderType == "staff",
                _ => false
            };

            if (!shouldMarkRead)
            {
                continue;
            }

            message.IsRead = true;
            message.ReadAt = now;
            await _messageRepository.UpdateAsync(message.Id, message);
        }

        if (readerType == "admin")
        {
            conversation.UnreadForAdminCount = 0;
        }
        else if (readerType == "customer")
        {
            conversation.UnreadForCustomerCount = 0;
        }

        conversation.UpdatedAt = now;
        await _conversationRepository.UpdateAsync(conversation.Id, conversation);
    }

    private async Task<ChatConversation> RequireConversationAsync(string conversationId)
    {
        var conversation = await _conversationRepository.GetByIdAsync(conversationId);
        if (conversation == null)
        {
            throw new InvalidOperationException("Conversation not found.");
        }

        return conversation;
    }

    private static string BuildPreview(string content)
    {
        return content.Length <= 120 ? content : $"{content[..117]}...";
    }

    private static string CreateClientMessageId()
    {
        return Guid.NewGuid().ToString("N");
    }

    private static GuestChatProfile ResolveProfile(ChatBootstrapRequest request, ClaimsPrincipal user)
    {
        var displayName = request.DisplayName.Trim();
        if (string.IsNullOrWhiteSpace(displayName))
        {
            displayName = user.FindFirst("FullName")?.Value?.Trim() ?? string.Empty;
        }

        var email = request.Email.Trim();
        if (string.IsNullOrWhiteSpace(email))
        {
            email = user.FindFirst(ClaimTypes.Email)?.Value?.Trim()
                ?? user.FindFirst(ClaimTypes.Name)?.Value?.Trim()
                ?? string.Empty;
        }

        var phoneNumber = request.PhoneNumber.Trim();
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            phoneNumber = user.FindFirst("PhoneNumber")?.Value?.Trim()
                ?? user.FindFirst(ClaimTypes.MobilePhone)?.Value?.Trim()
                ?? string.Empty;
        }

        return new GuestChatProfile
        {
            DisplayName = displayName,
            Email = email,
            PhoneNumber = phoneNumber
        };
    }

    private static void ValidateBootstrapRequest(GuestChatProfile profile)
    {
        if (string.IsNullOrWhiteSpace(profile.DisplayName))
        {
            throw new ArgumentException("Display name is required.");
        }

        if (string.IsNullOrWhiteSpace(profile.Email))
        {
            throw new ArgumentException("Email is required.");
        }

        var emailValidator = new EmailAddressAttribute();
        if (!emailValidator.IsValid(profile.Email))
        {
            throw new ArgumentException("Email is invalid.");
        }

        if (string.IsNullOrWhiteSpace(profile.PhoneNumber))
        {
            throw new ArgumentException("Phone number is required.");
        }
    }
}

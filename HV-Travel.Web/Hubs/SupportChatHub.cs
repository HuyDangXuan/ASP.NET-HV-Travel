using System.Security.Claims;
using HVTravel.Web.Models;
using HVTravel.Web.Security;
using HVTravel.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace HVTravel.Web.Hubs;

public class SupportChatHub : Hub
{
    private const string AdminInboxGroup = "admin-inbox";
    private readonly ISupportChatService _supportChatService;

    public SupportChatHub(ISupportChatService supportChatService)
    {
        _supportChatService = supportChatService;
    }

    public async Task JoinConversation(string conversationId, string visitorSessionId = "")
    {
        var conversation = await _supportChatService.GetConversationAsync(conversationId);
        if (conversation == null || !CanAccessConversation(conversation, visitorSessionId))
        {
            throw new HubException("Conversation not found.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, conversationId);
    }

    [Authorize(AuthenticationSchemes = AuthSchemes.AdminScheme, Roles = "Admin,Manager,Staff")]
    public async Task JoinAdminInbox()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, AdminInboxGroup);
    }

    public async Task SendPublicMessage(string conversationId, string visitorSessionId, string content)
    {
        var conversation = await _supportChatService.GetConversationAsync(conversationId);
        if (conversation == null || !CanAccessConversation(conversation, visitorSessionId))
        {
            throw new HubException("Conversation not found.");
        }

        var message = await _supportChatService.AddPublicMessageAsync(conversationId, content, Context.User ?? new ClaimsPrincipal());
        var updatedConversation = await _supportChatService.GetConversationAsync(conversationId);
        if (updatedConversation == null)
        {
            return;
        }

        await Clients.Group(conversationId).SendAsync("ReceiveMessage", ToMessageDto(message));
        await Clients.Group(AdminInboxGroup).SendAsync("ConversationUpdated", ToConversationDto(updatedConversation));
    }

    [Authorize(AuthenticationSchemes = AuthSchemes.AdminScheme, Roles = "Admin,Manager,Staff")]
    public async Task SendAdminMessage(string conversationId, string content)
    {
        var message = await _supportChatService.AddAdminMessageAsync(conversationId, content, Context.User ?? new ClaimsPrincipal());
        var updatedConversation = await _supportChatService.GetConversationAsync(conversationId);
        if (updatedConversation == null)
        {
            return;
        }

        await Clients.Group(conversationId).SendAsync("ReceiveMessage", ToMessageDto(message));
        await Clients.Group(AdminInboxGroup).SendAsync("ConversationUpdated", ToConversationDto(updatedConversation));
    }

    private bool CanAccessConversation(HVTravel.Domain.Entities.ChatConversation conversation, string visitorSessionId)
    {
        var user = Context.User;
        if (user?.Identity?.IsAuthenticated == true && user.IsInRole("Customer"))
        {
            var customerId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return !string.IsNullOrWhiteSpace(customerId) && conversation.CustomerId == customerId;
        }

        if (user?.Identity?.IsAuthenticated == true && (user.IsInRole("Admin") || user.IsInRole("Manager") || user.IsInRole("Staff")))
        {
            return true;
        }

        return !string.IsNullOrWhiteSpace(visitorSessionId) && conversation.VisitorSessionId == visitorSessionId;
    }

    private static ChatMessageDto ToMessageDto(HVTravel.Domain.Entities.ChatMessage message)
    {
        return new ChatMessageDto
        {
            Id = message.Id,
            ConversationId = message.ConversationId,
            ClientMessageId = message.ClientMessageId,
            SenderType = message.SenderType,
            SenderDisplayName = message.SenderDisplayName,
            Content = message.Content,
            IsRead = message.IsRead,
            SentAt = message.SentAt
        };
    }

    private static ChatConversationDto ToConversationDto(HVTravel.Domain.Entities.ChatConversation conversation)
    {
        return new ChatConversationDto
        {
            Id = conversation.Id,
            ConversationCode = conversation.ConversationCode,
            Channel = conversation.Channel,
            Status = conversation.Status,
            ParticipantType = conversation.ParticipantType,
            DisplayName = string.IsNullOrWhiteSpace(conversation.GuestProfile.DisplayName) ? "Khách truy cập" : conversation.GuestProfile.DisplayName,
            SourcePage = conversation.SourcePage,
            ContextType = conversation.ContextType ?? string.Empty,
            ContextId = conversation.ContextId ?? string.Empty,
            ContextLabel = conversation.ContextLabel ?? string.Empty,
            LastMessagePreview = conversation.LastMessagePreview,
            LastMessageAt = conversation.LastMessageAt,
            UnreadForAdminCount = conversation.UnreadForAdminCount,
            UnreadForCustomerCount = conversation.UnreadForCustomerCount
        };
    }
}

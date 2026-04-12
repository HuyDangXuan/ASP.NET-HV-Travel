using System.Security.Claims;
using HVTravel.Domain.Entities;
using HVTravel.Web.Models;
using HVTravel.Web.Services;
using Microsoft.AspNetCore.SignalR;

namespace HVTravel.Web.Hubs;

public sealed class TourAiChatHub : Hub
{
    private readonly ITourAiChatService _tourAiChatService;
    private readonly ITourAiPendingTracker _pendingTracker;

    public TourAiChatHub(ITourAiChatService tourAiChatService, ITourAiPendingTracker pendingTracker)
    {
        _tourAiChatService = tourAiChatService;
        _pendingTracker = pendingTracker;
    }

    public async Task<TourAiBootstrapResponse> JoinConversation(string conversationId, string visitorSessionId = "")
    {
        var conversation = await _tourAiChatService.GetConversationAsync(conversationId);
        if (conversation == null || !CanAccessConversation(conversation, visitorSessionId))
        {
            throw new HubException("Conversation not found.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, conversationId);

        var messages = await _tourAiChatService.GetMessagesAsync(conversationId);
        return new TourAiBootstrapResponse
        {
            Conversation = ToConversationDto(conversation),
            Messages = messages.Select(ToMessageDto).ToList(),
            IsAssistantPending = _pendingTracker.IsPending(conversationId)
        };
    }

    private bool CanAccessConversation(ChatConversation conversation, string visitorSessionId)
    {
        var user = Context.User;
        if (user?.Identity?.IsAuthenticated == true && user.IsInRole("Customer"))
        {
            var customerId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return !string.IsNullOrWhiteSpace(customerId) && conversation.CustomerId == customerId;
        }

        return !string.IsNullOrWhiteSpace(visitorSessionId) && conversation.VisitorSessionId == visitorSessionId;
    }

    private static ChatConversationDto ToConversationDto(ChatConversation conversation)
    {
        return new ChatConversationDto
        {
            Id = conversation.Id,
            ConversationCode = conversation.ConversationCode,
            Channel = conversation.Channel,
            Status = conversation.Status,
            ParticipantType = conversation.ParticipantType,
            DisplayName = string.IsNullOrWhiteSpace(conversation.GuestProfile.DisplayName)
                ? "Khach xem tour"
                : conversation.GuestProfile.DisplayName,
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

    private static ChatMessageDto ToMessageDto(ChatMessage message)
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
}

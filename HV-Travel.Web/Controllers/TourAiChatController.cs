using HVTravel.Domain.Entities;
using HVTravel.Web.Models;
using HVTravel.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace HVTravel.Web.Controllers;

[Route("[controller]")]
public sealed class TourAiChatController : Controller
{
    private readonly ITourAiChatService _tourAiChatService;
    private readonly ITourAiPendingTracker _pendingTracker;

    public TourAiChatController(ITourAiChatService tourAiChatService, ITourAiPendingTracker pendingTracker)
    {
        _tourAiChatService = tourAiChatService;
        _pendingTracker = pendingTracker;
    }

    [HttpPost("bootstrap")]
    public async Task<IActionResult> Bootstrap([FromBody] TourAiBootstrapRequest request)
    {
        try
        {
            var bootstrapResult = await _tourAiChatService.BootstrapConversationAsync(request, User);
            var conversation = bootstrapResult.Conversation;
            var messages = await _tourAiChatService.GetMessagesAsync(conversation.Id);

            return Json(new TourAiBootstrapResponse
            {
                Conversation = ToConversationDto(conversation),
                Messages = messages.Select(ToMessageDto).ToList(),
                SuggestedPrompts = bootstrapResult.SuggestedPrompts.ToList(),
                IsAssistantPending = _pendingTracker.IsPending(conversation.Id)
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Tour chat context was not found." });
        }
    }

    [HttpPost("message")]
    public async Task<IActionResult> Message([FromBody] TourAiSendMessageRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _tourAiChatService.EnqueueMessageAsync(request, User, cancellationToken);
            return Json(new TourAiSendAcceptedResponse
            {
                Conversation = ToConversationDto(result.Conversation),
                UserMessage = ToMessageDto(result.UserMessage),
                IsAssistantPending = result.IsAssistantPending
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return NotFound(new { message = "Conversation not found." });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Conversation not found." });
        }
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

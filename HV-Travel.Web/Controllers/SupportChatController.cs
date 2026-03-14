using System.Security.Claims;
using HVTravel.Web.Models;
using HVTravel.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace HVTravel.Web.Controllers;

[Route("[controller]")]
public class SupportChatController : Controller
{
    private readonly ISupportChatService _supportChatService;

    public SupportChatController(ISupportChatService supportChatService)
    {
        _supportChatService = supportChatService;
    }

    [HttpPost("bootstrap")]
    public async Task<IActionResult> Bootstrap([FromBody] ChatBootstrapRequest request)
    {
        var conversation = await _supportChatService.BootstrapConversationAsync(request, User);
        var messages = await _supportChatService.GetMessagesAsync(conversation.Id);

        return Json(new
        {
            conversation = ToConversationDto(conversation),
            messages = messages.Select(ToMessageDto)
        });
    }

    [HttpGet("history")]
    public async Task<IActionResult> History(string conversationId, string visitorSessionId = "")
    {
        var conversation = await _supportChatService.GetConversationAsync(conversationId);
        if (conversation == null || !CanAccessConversation(conversation, visitorSessionId))
        {
            return NotFound();
        }

        var messages = await _supportChatService.GetMessagesAsync(conversationId);
        await _supportChatService.MarkConversationReadAsync(conversationId, "customer");

        return Json(new
        {
            conversation = ToConversationDto(conversation),
            messages = messages.Select(ToMessageDto)
        });
    }

    [HttpPost("mark-read")]
    public async Task<IActionResult> MarkRead([FromBody] ChatMarkReadRequest request)
    {
        var conversation = await _supportChatService.GetConversationAsync(request.ConversationId);
        if (conversation == null || !CanAccessConversation(conversation, request.VisitorSessionId))
        {
            return NotFound();
        }

        await _supportChatService.MarkConversationReadAsync(conversation.Id, "customer");
        return Ok(new { success = true });
    }

    private bool CanAccessConversation(HVTravel.Domain.Entities.ChatConversation conversation, string visitorSessionId)
    {
        if (User.Identity?.IsAuthenticated == true && User.IsInRole("Customer"))
        {
            var customerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return !string.IsNullOrWhiteSpace(customerId) && conversation.CustomerId == customerId;
        }

        return !string.IsNullOrWhiteSpace(visitorSessionId) && conversation.VisitorSessionId == visitorSessionId;
    }

    private static ChatConversationDto ToConversationDto(HVTravel.Domain.Entities.ChatConversation conversation)
    {
        return new ChatConversationDto
        {
            Id = conversation.Id,
            ConversationCode = conversation.ConversationCode,
            Status = conversation.Status,
            ParticipantType = conversation.ParticipantType,
            DisplayName = conversation.ParticipantType == "customer"
                ? (conversation.GuestProfile.DisplayName ?? "Khách hàng")
                : (string.IsNullOrWhiteSpace(conversation.GuestProfile.DisplayName) ? "Khách truy cập" : conversation.GuestProfile.DisplayName),
            SourcePage = conversation.SourcePage,
            LastMessagePreview = conversation.LastMessagePreview,
            LastMessageAt = conversation.LastMessageAt,
            UnreadForAdminCount = conversation.UnreadForAdminCount,
            UnreadForCustomerCount = conversation.UnreadForCustomerCount
        };
    }

    private static ChatMessageDto ToMessageDto(HVTravel.Domain.Entities.ChatMessage message)
    {
        return new ChatMessageDto
        {
            Id = message.Id,
            ConversationId = message.ConversationId,
            SenderType = message.SenderType,
            SenderDisplayName = message.SenderDisplayName,
            Content = message.Content,
            IsRead = message.IsRead,
            SentAt = message.SentAt
        };
    }
}

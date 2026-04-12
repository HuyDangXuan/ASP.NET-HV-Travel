using HVTravel.Domain.Entities;
using HVTravel.Web.Models;
using HVTravel.Web.Security;
using HVTravel.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HVTravel.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(AuthenticationSchemes = AuthSchemes.AdminScheme, Roles = "Admin,Manager,Staff")]
[Route("Admin/[controller]")]
public class MessagesController : Controller
{
    private readonly ISupportChatService _supportChatService;

    public MessagesController(ISupportChatService supportChatService)
    {
        _supportChatService = supportChatService;
    }

    [HttpGet("")]
    [HttpGet("Index")]
    public IActionResult Index()
    {
        ViewData["Title"] = "Tin nhắn";
        ViewData["AdminSection"] = "messages";
        return View();
    }

    [HttpGet("Conversations")]
    public async Task<IActionResult> Conversations()
    {
        var conversations = await _supportChatService.GetAdminConversationsAsync();
        return Json(conversations.Select(ToConversationDto));
    }

    [HttpGet("Conversation/{conversationId}")]
    public async Task<IActionResult> Conversation(string conversationId)
    {
        var conversation = await _supportChatService.GetConversationAsync(conversationId);
        if (conversation == null)
        {
            return NotFound();
        }

        var messages = await _supportChatService.GetMessagesAsync(conversationId);
        await _supportChatService.MarkConversationReadAsync(conversationId, "admin");

        return Json(new
        {
            conversation = ToConversationDto(conversation),
            messages = messages.Select(ToMessageDto)
        });
    }

    [HttpPost("MarkRead/{conversationId}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkRead(string conversationId)
    {
        await _supportChatService.MarkConversationReadAsync(conversationId, "admin");
        return Ok(new { success = true });
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
                ? "Khách truy cập"
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

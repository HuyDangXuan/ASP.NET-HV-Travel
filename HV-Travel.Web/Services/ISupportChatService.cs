using System.Security.Claims;
using HVTravel.Domain.Entities;
using HVTravel.Web.Models;

namespace HVTravel.Web.Services;

public interface ISupportChatService
{
    Task<ChatConversation> BootstrapConversationAsync(ChatBootstrapRequest request, ClaimsPrincipal user);

    Task<ChatConversation?> GetConversationAsync(string conversationId);

    Task<List<ChatMessage>> GetMessagesAsync(string conversationId, int take = 100);

    Task<List<ChatConversation>> GetAdminConversationsAsync();

    Task<ChatMessage> AddPublicMessageAsync(string conversationId, string content, ClaimsPrincipal user);

    Task<ChatMessage> AddAdminMessageAsync(string conversationId, string content, ClaimsPrincipal user);

    Task MarkConversationReadAsync(string conversationId, string readerType);
}

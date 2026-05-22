using System.Security.Claims;
using HVTravel.Domain.Entities;
using HVTravel.Web.Models;

namespace HVTravel.Web.Services;

public interface ITourAiChatService
{
    Task<TourAiBootstrapResult> BootstrapConversationAsync(TourAiBootstrapRequest request, ClaimsPrincipal user);

    Task<ChatConversation?> GetConversationAsync(string conversationId);

    Task<List<ChatMessage>> GetMessagesAsync(string conversationId, int take = 100);

    Task<TourAiChatSendAcceptedResult> EnqueueMessageAsync(TourAiSendMessageRequest request, ClaimsPrincipal user, CancellationToken cancellationToken = default);

    Task<TourAiAssistantReplyResult?> GenerateAssistantReplyAsync(string conversationId, string userMessageId, CancellationToken cancellationToken = default);
}

public sealed class TourAiBootstrapResult
{
    public ChatConversation Conversation { get; init; } = new();

    public IReadOnlyList<string> SuggestedPrompts { get; init; } = Array.Empty<string>();
}

public sealed class TourAiChatSendAcceptedResult
{
    public ChatConversation Conversation { get; init; } = new();

    public ChatMessage UserMessage { get; init; } = new();

    public bool IsAssistantPending { get; init; }
}

public sealed class TourAiAssistantReplyResult
{
    public ChatConversation Conversation { get; init; } = new();

    public ChatMessage AssistantMessage { get; init; } = new();
}

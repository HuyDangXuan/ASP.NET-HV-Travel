namespace HVTravel.Web.Services;

public interface IGroqChatClient
{
    Task<string> CompleteChatAsync(IReadOnlyList<GroqChatMessage> messages, CancellationToken cancellationToken = default);
}

public sealed record GroqChatMessage(string Role, string Content);

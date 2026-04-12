namespace HVTravel.Web.Models;

public class ChatBootstrapRequest
{
    public string VisitorSessionId { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;

    public string SourcePage { get; set; } = "/";
}

public class ChatMessageDto
{
    public string Id { get; set; } = string.Empty;

    public string ConversationId { get; set; } = string.Empty;

    public string ClientMessageId { get; set; } = string.Empty;

    public string SenderType { get; set; } = string.Empty;

    public string SenderDisplayName { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public bool IsRead { get; set; }

    public DateTime SentAt { get; set; }
}

public class ChatConversationDto
{
    public string Id { get; set; } = string.Empty;

    public string ConversationCode { get; set; } = string.Empty;

    public string Channel { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string ParticipantType { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string SourcePage { get; set; } = string.Empty;

    public string ContextType { get; set; } = string.Empty;

    public string ContextId { get; set; } = string.Empty;

    public string ContextLabel { get; set; } = string.Empty;

    public string LastMessagePreview { get; set; } = string.Empty;

    public DateTime LastMessageAt { get; set; }

    public int UnreadForAdminCount { get; set; }

    public int UnreadForCustomerCount { get; set; }
}

public class ChatMarkReadRequest
{
    public string ConversationId { get; set; } = string.Empty;

    public string VisitorSessionId { get; set; } = string.Empty;
}

public class TourAiBootstrapRequest
{
    public string TourId { get; set; } = string.Empty;

    public string VisitorSessionId { get; set; } = string.Empty;

    public string SourcePage { get; set; } = "/";
}

public class TourAiSendMessageRequest
{
    public string ConversationId { get; set; } = string.Empty;

    public string VisitorSessionId { get; set; } = string.Empty;

    public string ClientMessageId { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;
}

public class TourAiBootstrapResponse
{
    public ChatConversationDto Conversation { get; set; } = new();

    public List<ChatMessageDto> Messages { get; set; } = [];

    public bool IsAssistantPending { get; set; }
}

public class TourAiSendAcceptedResponse
{
    public ChatConversationDto Conversation { get; set; } = new();

    public ChatMessageDto UserMessage { get; set; } = new();

    public bool IsAssistantPending { get; set; }
}

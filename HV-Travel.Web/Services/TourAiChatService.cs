using System.Security.Claims;
using System.Text;
using HVTravel.Application.Models;
using HVTravel.Application.Services;
using HVTravel.Domain.Entities;
using HVTravel.Domain.Interfaces;
using HVTravel.Domain.Utils;
using HVTravel.Web.Models;
using Microsoft.Extensions.Logging;

namespace HVTravel.Web.Services;

public sealed class TourAiChatService : ITourAiChatService
{
    private const string TourAiChannel = "tour-ai";
    private const string TourContextType = "tour";
    private const string AssistantDisplayName = "HV Travel AI";
    private const int PromptHistoryLimit = 12;

    private readonly IRepository<ChatConversation> _conversationRepository;
    private readonly IRepository<ChatMessage> _messageRepository;
    private readonly ITourRepository _tourRepository;
    private readonly ITourAiRouteAdvisorContextBuilder _routeAdvisorContextBuilder;
    private readonly IGroqChatClient _groqChatClient;
    private readonly ITourAiJobQueue _jobQueue;
    private readonly ITourAiPendingTracker _pendingTracker;
    private readonly ILogger<TourAiChatService> _logger;

    public TourAiChatService(
        IRepository<ChatConversation> conversationRepository,
        IRepository<ChatMessage> messageRepository,
        ITourRepository tourRepository,
        ITourAiRouteAdvisorContextBuilder routeAdvisorContextBuilder,
        IGroqChatClient groqChatClient,
        ITourAiJobQueue jobQueue,
        ITourAiPendingTracker pendingTracker,
        ILogger<TourAiChatService> logger)
    {
        _conversationRepository = conversationRepository;
        _messageRepository = messageRepository;
        _tourRepository = tourRepository;
        _routeAdvisorContextBuilder = routeAdvisorContextBuilder;
        _groqChatClient = groqChatClient;
        _jobQueue = jobQueue;
        _pendingTracker = pendingTracker;
        _logger = logger;
    }

    public async Task<TourAiBootstrapResult> BootstrapConversationAsync(TourAiBootstrapRequest request, ClaimsPrincipal user)
    {
        var tourId = request.TourId.Trim();
        if (string.IsNullOrWhiteSpace(tourId))
        {
            throw new ArgumentException("Thiếu TourId.", nameof(request));
        }

        var tour = await GetPublicTourAsync(tourId);
        var routeStyle = ResolveRouteStyle(request.RouteStyle, request.SourcePage);
        var routeAdvisorContext = await _routeAdvisorContextBuilder.BuildAsync(tour, routeStyle);
        var participant = ResolveParticipant(user, request.VisitorSessionId);

        var conversation = await FindConversationAsync(tour.Id, participant.CustomerId, participant.VisitorSessionId);
        if (conversation == null)
        {
            conversation = new ChatConversation
            {
                ConversationCode = $"TAI{DateTime.UtcNow:yyyyMMddHHmmss}{Random.Shared.Next(100, 999)}",
                Channel = TourAiChannel,
                Status = "open",
                ParticipantType = participant.ParticipantType,
                CustomerId = participant.CustomerId,
                VisitorSessionId = participant.VisitorSessionId,
                GuestProfile = participant.Profile,
                SourcePage = ResolveSourcePage(request.SourcePage, routeStyle),
                ContextType = TourContextType,
                ContextId = tour.Id,
                ContextLabel = tour.Name,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                LastMessageAt = DateTime.UtcNow
            };

            await _conversationRepository.AddAsync(conversation);
        }
        else
        {
            conversation.Channel = TourAiChannel;
            conversation.Status = "open";
            conversation.SourcePage = ResolveSourcePage(request.SourcePage, routeStyle);
            conversation.ContextType = TourContextType;
            conversation.ContextId = tour.Id;
            conversation.ContextLabel = tour.Name;
            conversation.CustomerId ??= participant.CustomerId;
            conversation.VisitorSessionId ??= participant.VisitorSessionId;
            conversation.GuestProfile = MergeProfile(conversation.GuestProfile, participant.Profile);
            conversation.UpdatedAt = DateTime.UtcNow;
            await _conversationRepository.UpdateAsync(conversation.Id, conversation);
        }

        var messages = await GetMessagesAsync(conversation.Id, 1);
        if (messages.Count == 0)
        {
            var welcomeMessage = new ChatMessage
            {
                ConversationId = conversation.Id,
                SenderType = "assistant",
                SenderDisplayName = AssistantDisplayName,
                MessageType = "system",
                ClientMessageId = CreateClientMessageId(),
                Content = BuildWelcomeMessage(tour),
                IsRead = true,
                ReadAt = DateTime.UtcNow,
                SentAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            await _messageRepository.AddAsync(welcomeMessage);
            await UpdateConversationSummaryAsync(conversation, welcomeMessage);
        }

        return new TourAiBootstrapResult
        {
            Conversation = conversation,
            SuggestedPrompts = routeAdvisorContext.SuggestedPrompts
        };
    }

    public async Task<ChatConversation?> GetConversationAsync(string conversationId)
    {
        var conversation = await _conversationRepository.GetByIdAsync(conversationId);
        if (conversation == null || !string.Equals(conversation.Channel, TourAiChannel, StringComparison.Ordinal))
        {
            return null;
        }

        return conversation;
    }

    public async Task<List<ChatMessage>> GetMessagesAsync(string conversationId, int take = 100)
    {
        return (await _messageRepository.FindAsync(message => message.ConversationId == conversationId))
            .OrderBy(message => message.SentAt)
            .TakeLast(take)
            .ToList();
    }

    public async Task<TourAiChatSendAcceptedResult> EnqueueMessageAsync(
        TourAiSendMessageRequest request,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.ConversationId))
        {
            throw new ArgumentException("Thiếu ConversationId.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.Content))
        {
            throw new ArgumentException("Vui lòng nhập nội dung câu hỏi.", nameof(request));
        }

        var conversation = await RequireConversationAsync(request.ConversationId);
        EnsureConversationAccess(conversation, user, request.VisitorSessionId);

        var clientMessageId = ResolveClientMessageId(request.ClientMessageId);
        var existingMessage = await FindMessageByClientMessageIdAsync(conversation.Id, clientMessageId);
        if (existingMessage != null)
        {
            return new TourAiChatSendAcceptedResult
            {
                Conversation = conversation,
                UserMessage = existingMessage,
                IsAssistantPending = _pendingTracker.IsPending(conversation.Id)
            };
        }

        if (!_pendingTracker.TryStart(conversation.Id))
        {
            throw new InvalidOperationException("AI đang xử lý câu hỏi trước đó. Vui lòng đợi thêm một chút.");
        }

        var participant = ResolveParticipant(user, conversation.VisitorSessionId ?? request.VisitorSessionId);
        var userMessage = new ChatMessage
        {
            ConversationId = conversation.Id,
            SenderType = participant.IsCustomer ? "customer" : "guest",
            SenderUserId = participant.CustomerId,
            SenderDisplayName = participant.IsCustomer
                ? (!string.IsNullOrWhiteSpace(participant.Profile.DisplayName) ? participant.Profile.DisplayName : "Bạn")
                : "Bạn",
            MessageType = "text",
            ClientMessageId = clientMessageId,
            Content = request.Content.Trim(),
            IsRead = true,
            ReadAt = DateTime.UtcNow,
            SentAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        try
        {
            await _messageRepository.AddAsync(userMessage);

            conversation.GuestProfile = MergeProfile(conversation.GuestProfile, participant.Profile);
            conversation.CustomerId ??= participant.CustomerId;
            conversation.VisitorSessionId ??= participant.VisitorSessionId;
            await UpdateConversationSummaryAsync(conversation, userMessage);

            await _jobQueue.EnqueueAsync(
                new TourAiReplyJob(conversation.Id, userMessage.Id, conversation.VisitorSessionId ?? request.VisitorSessionId, DateTime.UtcNow),
                cancellationToken);

            return new TourAiChatSendAcceptedResult
            {
                Conversation = conversation,
                UserMessage = userMessage,
                IsAssistantPending = true
            };
        }
        catch
        {
            _pendingTracker.Complete(conversation.Id);
            throw;
        }
    }

    public async Task<TourAiAssistantReplyResult?> GenerateAssistantReplyAsync(
        string conversationId,
        string userMessageId,
        CancellationToken cancellationToken = default)
    {
        var conversation = await GetConversationAsync(conversationId);
        if (conversation == null)
        {
            return null;
        }

        var userMessage = await _messageRepository.GetByIdAsync(userMessageId);
        if (userMessage == null || !string.Equals(userMessage.ConversationId, conversationId, StringComparison.Ordinal))
        {
            return null;
        }

        var assistantContent = await BuildAssistantReplyAsync(conversation, cancellationToken);
        var assistantMessage = new ChatMessage
        {
            ConversationId = conversation.Id,
            SenderType = "assistant",
            SenderDisplayName = AssistantDisplayName,
            MessageType = "text",
            ClientMessageId = CreateClientMessageId(),
            Content = NormalizeAssistantContent(assistantContent),
            IsRead = true,
            ReadAt = DateTime.UtcNow,
            SentAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        await _messageRepository.AddAsync(assistantMessage);
        await UpdateConversationSummaryAsync(conversation, assistantMessage);

        return new TourAiAssistantReplyResult
        {
            Conversation = conversation,
            AssistantMessage = assistantMessage
        };
    }

    private async Task<ChatConversation?> FindConversationAsync(string tourId, string? customerId, string visitorSessionId)
    {
        IEnumerable<ChatConversation> conversations;
        if (!string.IsNullOrWhiteSpace(customerId))
        {
            conversations = await _conversationRepository.FindAsync(conversation =>
                conversation.CustomerId == customerId
                && conversation.Channel == TourAiChannel
                && conversation.ContextType == TourContextType
                && conversation.ContextId == tourId
                && conversation.Status != "closed");
        }
        else
        {
            conversations = await _conversationRepository.FindAsync(conversation =>
                conversation.VisitorSessionId == visitorSessionId
                && conversation.Channel == TourAiChannel
                && conversation.ContextType == TourContextType
                && conversation.ContextId == tourId
                && conversation.Status != "closed");
        }

        return conversations.OrderByDescending(conversation => conversation.UpdatedAt).FirstOrDefault();
    }

    private async Task<ChatMessage?> FindMessageByClientMessageIdAsync(string conversationId, string clientMessageId)
    {
        var messages = await _messageRepository.FindAsync(message =>
            message.ConversationId == conversationId && message.ClientMessageId == clientMessageId);

        return messages.OrderByDescending(message => message.SentAt).FirstOrDefault();
    }

    private async Task<string> BuildAssistantReplyAsync(ChatConversation conversation, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(conversation.ContextId))
            {
                return BuildFallbackReply();
            }

            var tour = await _tourRepository.GetByIdAsync(conversation.ContextId);
            if (tour == null || !IsPubliclyVisible(tour.Status))
            {
                return BuildUnavailableTourReply();
            }

            var recentMessages = await GetMessagesAsync(conversation.Id, PromptHistoryLimit);
            var routeStyle = ResolveRouteStyle(null, conversation.SourcePage);
            var routeAdvisorContext = await _routeAdvisorContextBuilder.BuildAsync(tour, routeStyle, cancellationToken);
            var promptMessages = BuildPromptMessages(routeAdvisorContext, recentMessages);
            return await _groqChatClient.CompleteChatAsync(promptMessages, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate a Groq response for AI tour chat conversation {ConversationId}.", conversation.Id);
            return BuildFallbackReply();
        }
    }

    private static List<GroqChatMessage> BuildPromptMessages(TourAiRouteAdvisorContext routeAdvisorContext, IReadOnlyList<ChatMessage> recentMessages)
    {
        var promptMessages = new List<GroqChatMessage>
        {
            new(
                "system",
                """
                Bạn là trợ lý AI tư vấn tour của HV Travel trên trang chi tiết tour.
                Luôn trả lời bằng tiếng Việt tự nhiên, đầy đủ dấu, rõ ràng và đúng ngữ cảnh chăm sóc khách hàng.
                Tuyệt đối không viết tiếng Việt không dấu, không dùng kiểu telex, không dùng ASCII fallback.
                Chỉ được dùng dữ liệu tour và lịch sử hội thoại đã cung cấp.
                Nếu dữ liệu không có thông tin người dùng hỏi, phải nói rõ bạn chưa thấy dữ liệu đó trong tour hiện tại.
                Không bịa thêm giá, lịch trình, điều khoản, giờ giấc, dịch vụ, khách sạn, phương tiện hoặc ưu đãi.
                Nếu cần xác nhận sâu hơn hoặc dữ liệu đang thiếu, hãy mời người dùng mở khung chat hỗ trợ bên dưới để gặp tư vấn viên thật.
                """),
            new("system", routeAdvisorContext.SnapshotText),
            new(
                "system",
                """
                Quy tắc theo lộ trình: nếu người dùng hỏi về di chuyển, lịch trình, thời gian hành trình hoặc traffic, ưu tiên RouteInsight trong context.
                Nếu người dùng hỏi tour phù hợp kiểu hành trình nào, dùng routeStyle và recommendation signals đã cung cấp.
                Nếu so sánh tour khác, chỉ dùng related tour summaries trong context; không tự thêm tour ngoài context.
                Nếu tour chưa có routing, nói rõ tour này chưa có dữ liệu lộ trình có cấu trúc.
                Không bịa ETA thời gian thực, giao thông thời gian thực, khách sạn, phương tiện hoặc dịch vụ ngoài dữ liệu.
                Không lộ tọa độ, lat/lng, raw coordinates hoặc dữ liệu nội bộ không dành cho public.
                """)
        };

        foreach (var message in recentMessages)
        {
            var role = message.SenderType switch
            {
                "assistant" => "assistant",
                "system" => "system",
                "guest" or "customer" => "user",
                _ => string.Empty
            };

            if (string.IsNullOrWhiteSpace(role) || string.IsNullOrWhiteSpace(message.Content))
            {
                continue;
            }

            promptMessages.Add(new GroqChatMessage(role, message.Content));
        }

        return promptMessages;
    }

    private static string BuildTourSnapshot(Tour tour)
    {
        var routeInsight = new RouteInsightService().Build(tour);
        var departures = (tour.Departures?.Count > 0 ? tour.Departures : tour.EffectiveDepartures.ToList())
            .OrderBy(item => item.StartDate)
            .Take(6)
            .ToList();

        var builder = new StringBuilder();
        builder.AppendLine("Dữ liệu tour hiện tại:");
        builder.AppendLine($"- Tên tour: {tour.Name}");
        builder.AppendLine($"- Mã tour: {ValueOrFallback(tour.Code)}");
        builder.AppendLine($"- Điểm đến: {BuildDestinationLabel(tour)}");
        builder.AppendLine($"- Thời lượng: {ValueOrFallback(tour.Duration?.Text)}");
        builder.AppendLine($"- Mô tả ngắn: {ValueOrFallback(RichTextContentFormatter.ToPlainTextSummary(tour.ShortDescription ?? tour.Description, 320))}");
        builder.AppendLine($"- Mô tả chi tiết: {ValueOrFallback(RichTextContentFormatter.ToPlainText(tour.Description))}");
        builder.AppendLine($"- Điểm nổi bật: {BuildListLine(tour.Highlights)}");
        builder.AppendLine($"- Bao gồm: {BuildListLine(tour.GeneratedInclusions)}");
        builder.AppendLine($"- Không bao gồm: {BuildListLine(tour.GeneratedExclusions)}");
        builder.AppendLine($"- Điểm hẹn: {ValueOrFallback(tour.MeetingPoint)}");
        builder.AppendLine($"- Hủy tour: {ValueOrFallback(tour.CancellationPolicy?.Summary)}");
        builder.AppendLine($"- Xác nhận: {ValueOrFallback(tour.ConfirmationType)}");
        builder.AppendLine($"- Giá người lớn mặc định: {FormatCurrency(tour.Price?.Adult)}");
        builder.AppendLine($"- Giá trẻ em mặc định: {FormatCurrency(tour.Price?.Child)}");
        builder.AppendLine($"- Giá em bé mặc định: {FormatCurrency(tour.Price?.Infant)}");
        builder.AppendLine("- Lịch trình:");

        if (tour.Schedule != null && tour.Schedule.Count > 0)
        {
            foreach (var item in tour.Schedule.OrderBy(entry => entry.Day).Take(8))
            {
                builder.AppendLine($"  * Ngày {item.Day:00}: {ValueOrFallback(item.Title)}. {ValueOrFallback(RichTextContentFormatter.ToPlainText(item.Description))}");
            }
        }
        else
        {
            builder.AppendLine("  * Chưa có lịch trình chi tiết.");
        }

        builder.AppendLine("- Các đợt khởi hành:");
        if (departures.Count > 0)
        {
            foreach (var departure in departures)
            {
                builder.AppendLine(
                    $"  * {departure.StartDate:dd/MM/yyyy}: người lớn {FormatCurrency(departure.AdultPrice)}, trẻ em {FormatCurrency(departure.ChildPrice)}, em bé {FormatCurrency(departure.InfantPrice)}, còn {departure.RemainingCapacity} chỗ, xác nhận {ValueOrFallback(departure.ConfirmationType)}");
            }
        }
        else
        {
            builder.AppendLine("  * Chưa có lịch khởi hành đang mở bán.");
        }

        if (routeInsight.HasRouting)
        {
            builder.AppendLine("- Tóm tắt lộ trình:");
            builder.AppendLine($"  * Tổng số điểm dừng: {routeInsight.StopCount}");
            builder.AppendLine($"  * Tổng phút tham quan: {routeInsight.TotalVisitMinutes}");
            builder.AppendLine($"  * Tổng phút di chuyển ước lượng: {routeInsight.TotalTravelMinutes}");
            builder.AppendLine($"  * Tổng thời lượng hành trình: {routeInsight.TotalJourneyMinutes}");

            foreach (var day in routeInsight.Days.Take(6))
            {
                builder.AppendLine(
                    $"  * Ngày {day.Day:00}: {day.StopCount} điểm dừng, tham quan {day.VisitMinutes} phút, di chuyển {day.TravelMinutes} phút.");
            }
        }

        if (routeInsight.HasRouting && routeInsight.Days.Any(day => !string.IsNullOrWhiteSpace(day.PeakDayPart)))
        {
            builder.AppendLine($"- Peak traffic: {BuildTrafficSummary(routeInsight.Days)}");
        }

        return builder.ToString().Trim();
    }

    private async Task<Tour> GetPublicTourAsync(string tourId)
    {
        var tour = await _tourRepository.GetByIdAsync(tourId);
        if (tour == null || !IsPubliclyVisible(tour.Status))
        {
            throw new KeyNotFoundException("Không tìm thấy tour.");
        }

        return tour;
    }

    private async Task<ChatConversation> RequireConversationAsync(string conversationId)
    {
        var conversation = await GetConversationAsync(conversationId);
        if (conversation == null)
        {
            throw new KeyNotFoundException("Không tìm thấy cuộc trò chuyện.");
        }

        return conversation;
    }

    private static void EnsureConversationAccess(ChatConversation conversation, ClaimsPrincipal user, string visitorSessionId)
    {
        if (user.Identity?.IsAuthenticated == true && user.IsInRole("Customer"))
        {
            var customerId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrWhiteSpace(customerId) && conversation.CustomerId == customerId)
            {
                return;
            }
        }

        if (!string.IsNullOrWhiteSpace(visitorSessionId) && conversation.VisitorSessionId == visitorSessionId)
        {
            return;
        }

        throw new UnauthorizedAccessException("Không tìm thấy cuộc trò chuyện.");
    }

    private async Task UpdateConversationSummaryAsync(ChatConversation conversation, ChatMessage lastMessage)
    {
        conversation.LastMessagePreview = BuildPreview(lastMessage.Content);
        conversation.LastMessageAt = lastMessage.SentAt;
        conversation.UpdatedAt = lastMessage.SentAt;
        conversation.UnreadForAdminCount = 0;
        conversation.UnreadForCustomerCount = 0;
        await _conversationRepository.UpdateAsync(conversation.Id, conversation);
    }

    private static string BuildPreview(string content)
    {
        return content.Length <= 120 ? content : $"{content[..117]}...";
    }

    private static string ResolveClientMessageId(string clientMessageId)
    {
        return string.IsNullOrWhiteSpace(clientMessageId) ? CreateClientMessageId() : clientMessageId.Trim();
    }

    private static string CreateClientMessageId()
    {
        return Guid.NewGuid().ToString("N");
    }

    private static string BuildWelcomeMessage(Tour tour)
    {
        return $"Chào bạn, mình là AI tư vấn của HV Travel. Mình đang đọc dữ liệu của tour \"{tour.Name}\" và có thể hỗ trợ bạn hỏi nhanh về lịch trình, giá, khởi hành, dịch vụ bao gồm hoặc chính sách hiện có.";
    }

    private static string BuildFallbackReply()
    {
        return "Mình chưa thể phản hồi chính xác từ dữ liệu tour ngay lúc này. Bạn vui lòng mở khung chat hỗ trợ bên dưới để tư vấn viên HV Travel kiểm tra trực tiếp giúp bạn nhé.";
    }

    private static string BuildUnavailableTourReply()
    {
        return "Mình chưa đọc được dữ liệu tour này vào lúc này hoặc tour không còn hiển thị công khai. Bạn vui lòng mở khung chat hỗ trợ bên dưới để đội ngũ HV Travel hỗ trợ trực tiếp nhé.";
    }

    private static ParticipantContext ResolveParticipant(ClaimsPrincipal user, string visitorSessionId)
    {
        var isCustomer = user.Identity?.IsAuthenticated == true && user.IsInRole("Customer");
        var customerId = isCustomer ? user.FindFirst(ClaimTypes.NameIdentifier)?.Value?.Trim() : null;
        var resolvedVisitorSessionId = !string.IsNullOrWhiteSpace(visitorSessionId)
            ? visitorSessionId.Trim()
            : $"visitor-{Guid.NewGuid():N}";

        var displayName = user.FindFirst("FullName")?.Value?.Trim()
            ?? user.Identity?.Name?.Trim()
            ?? (isCustomer ? "Khách hàng" : "Khách xem tour");
        var email = user.FindFirst(ClaimTypes.Email)?.Value?.Trim()
            ?? user.FindFirst(ClaimTypes.Name)?.Value?.Trim()
            ?? string.Empty;
        var phoneNumber = user.FindFirst("PhoneNumber")?.Value?.Trim()
            ?? user.FindFirst(ClaimTypes.MobilePhone)?.Value?.Trim()
            ?? string.Empty;

        return new ParticipantContext
        {
            IsCustomer = isCustomer,
            CustomerId = customerId,
            VisitorSessionId = resolvedVisitorSessionId,
            ParticipantType = isCustomer ? "customer" : "guest",
            Profile = new GuestChatProfile
            {
                DisplayName = displayName,
                Email = email,
                PhoneNumber = phoneNumber
            }
        };
    }

    private static GuestChatProfile MergeProfile(GuestChatProfile current, GuestChatProfile incoming)
    {
        return new GuestChatProfile
        {
            DisplayName = !string.IsNullOrWhiteSpace(incoming.DisplayName) ? incoming.DisplayName : current.DisplayName,
            Email = !string.IsNullOrWhiteSpace(incoming.Email) ? incoming.Email : current.Email,
            PhoneNumber = !string.IsNullOrWhiteSpace(incoming.PhoneNumber) ? incoming.PhoneNumber : current.PhoneNumber
        };
    }

    private static string ResolveSourcePage(string? sourcePage, string routeStyle)
    {
        var resolved = string.IsNullOrWhiteSpace(sourcePage) ? "/" : sourcePage.Trim();
        if (TryReadRouteStyleFromSourcePage(resolved, out _))
        {
            return resolved;
        }

        var separator = resolved.Contains('?') ? "&" : "?";
        return $"{resolved}{separator}routeStyle={Uri.EscapeDataString(RouteRecommendationStyles.Normalize(routeStyle))}";
    }

    private static string ResolveRouteStyle(string? routeStyle, string? sourcePage)
    {
        if (!string.IsNullOrWhiteSpace(routeStyle))
        {
            return RouteRecommendationStyles.Normalize(routeStyle);
        }

        return TryReadRouteStyleFromSourcePage(sourcePage, out var routeStyleFromSourcePage)
            ? RouteRecommendationStyles.Normalize(routeStyleFromSourcePage)
            : RouteRecommendationStyles.Balanced;
    }

    private static bool TryReadRouteStyleFromSourcePage(string? sourcePage, out string routeStyle)
    {
        routeStyle = string.Empty;
        if (string.IsNullOrWhiteSpace(sourcePage))
        {
            return false;
        }

        var queryStart = sourcePage.IndexOf('?');
        if (queryStart < 0 || queryStart >= sourcePage.Length - 1)
        {
            return false;
        }

        var query = sourcePage[(queryStart + 1)..];
        var fragmentStart = query.IndexOf('#');
        if (fragmentStart >= 0)
        {
            query = query[..fragmentStart];
        }

        foreach (var pair in query.Split('&', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var separator = pair.IndexOf('=');
            var key = separator >= 0 ? pair[..separator] : pair;
            if (!string.Equals(Uri.UnescapeDataString(key), "routeStyle", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            routeStyle = separator >= 0 ? Uri.UnescapeDataString(pair[(separator + 1)..]) : string.Empty;
            return !string.IsNullOrWhiteSpace(routeStyle);
        }

        return false;
    }

    private static bool IsPubliclyVisible(string? status)
    {
        return status is "Active" or "ComingSoon" or "SoldOut";
    }

    private static string BuildDestinationLabel(Tour tour)
    {
        var parts = new[] { tour.Destination?.City, tour.Destination?.Country }
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToList();
        return parts.Count > 0 ? string.Join(", ", parts) : "Chưa có dữ liệu";
    }

    private static string BuildListLine(IEnumerable<string>? values)
    {
        var items = values?.Where(value => !string.IsNullOrWhiteSpace(value)).ToList() ?? [];
        return items.Count > 0 ? string.Join("; ", items) : "Chưa có dữ liệu";
    }

    private static string ValueOrFallback(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "Chưa có dữ liệu" : value.Trim();
    }

    private static string FormatCurrency(decimal? value)
    {
        var amount = value.GetValueOrDefault();
        return amount > 0m ? $"{amount:N0} VND" : "Chưa có dữ liệu";
    }

    private static string BuildTrafficSummary(IEnumerable<RouteInsightDay> days)
    {
        var highlights = days
            .Where(day => !string.IsNullOrWhiteSpace(day.PeakDayPart))
            .Take(3)
            .Select(day => $"ngày {day.Day:00} {TranslateDayPart(day.PeakDayPart)} {TranslateCongestion(day.PeakCongestionLevel)}")
            .ToList();

        return highlights.Count > 0 ? string.Join("; ", highlights) : "ổn định";
    }

    private static string TranslateDayPart(string? value)
    {
        return value switch
        {
            "early_morning" => "sáng sớm",
            "morning_peak" => "giờ cao điểm sáng",
            "late_morning" => "cuối buổi sáng",
            "midday" => "giữa ngày",
            "afternoon" => "buổi chiều",
            "evening_peak" => "giờ cao điểm tối",
            _ => "buổi tối"
        };
    }

    private static string TranslateCongestion(string? value)
    {
        return value switch
        {
            "high" => "mật độ cao",
            "moderate" => "mật độ vừa",
            _ => "mật độ thấp"
        };
    }

    private static string NormalizeAssistantContent(string content)
    {
        return TextEncodingRepair.NormalizeText(content).Trim();
    }

    private sealed class ParticipantContext
    {
        public bool IsCustomer { get; init; }

        public string? CustomerId { get; init; }

        public string VisitorSessionId { get; init; } = string.Empty;

        public string ParticipantType { get; init; } = "guest";

        public GuestChatProfile Profile { get; init; } = new();
    }
}

using System.Collections.Concurrent;
using System.Threading.Channels;
using HVTravel.Web.Hubs;
using HVTravel.Web.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HVTravel.Web.Services;

public sealed record TourAiReplyJob(
    string ConversationId,
    string UserMessageId,
    string VisitorSessionId,
    DateTime EnqueuedAt);

public interface ITourAiJobQueue
{
    ValueTask EnqueueAsync(TourAiReplyJob job, CancellationToken cancellationToken = default);

    ValueTask<TourAiReplyJob> DequeueAsync(CancellationToken cancellationToken);
}

public interface ITourAiPendingTracker
{
    bool TryStart(string conversationId);

    bool IsPending(string conversationId);

    void Complete(string conversationId);
}

public sealed class TourAiJobQueue : ITourAiJobQueue
{
    private readonly Channel<TourAiReplyJob> _channel = Channel.CreateUnbounded<TourAiReplyJob>(new UnboundedChannelOptions
    {
        SingleReader = true,
        SingleWriter = false
    });

    public ValueTask EnqueueAsync(TourAiReplyJob job, CancellationToken cancellationToken = default)
    {
        return _channel.Writer.WriteAsync(job, cancellationToken);
    }

    public ValueTask<TourAiReplyJob> DequeueAsync(CancellationToken cancellationToken)
    {
        return _channel.Reader.ReadAsync(cancellationToken);
    }
}

public sealed class TourAiPendingTracker : ITourAiPendingTracker
{
    private readonly ConcurrentDictionary<string, byte> _pendingConversations = new(StringComparer.Ordinal);

    public bool TryStart(string conversationId)
    {
        return _pendingConversations.TryAdd(conversationId, 0);
    }

    public bool IsPending(string conversationId)
    {
        return _pendingConversations.ContainsKey(conversationId);
    }

    public void Complete(string conversationId)
    {
        _pendingConversations.TryRemove(conversationId, out _);
    }
}

public sealed class TourAiReplyWorker : BackgroundService
{
    private readonly ITourAiJobQueue _jobQueue;
    private readonly ITourAiPendingTracker _pendingTracker;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHubContext<TourAiChatHub> _hubContext;
    private readonly ILogger<TourAiReplyWorker> _logger;

    public TourAiReplyWorker(
        ITourAiJobQueue jobQueue,
        ITourAiPendingTracker pendingTracker,
        IServiceScopeFactory scopeFactory,
        IHubContext<TourAiChatHub> hubContext,
        ILogger<TourAiReplyWorker> logger)
    {
        _jobQueue = jobQueue;
        _pendingTracker = pendingTracker;
        _scopeFactory = scopeFactory;
        _hubContext = hubContext;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            TourAiReplyJob job;

            try
            {
                job = await _jobQueue.DequeueAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<ITourAiChatService>();
                var result = await service.GenerateAssistantReplyAsync(job.ConversationId, job.UserMessageId, stoppingToken);
                if (result == null)
                {
                    continue;
                }

                await _hubContext.Clients.Group(job.ConversationId).SendAsync(
                    "ReceiveMessage",
                    ToMessageDto(result.AssistantMessage),
                    stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to process AI reply job for conversation {ConversationId} and user message {UserMessageId}.",
                    job.ConversationId,
                    job.UserMessageId);
            }
            finally
            {
                _pendingTracker.Complete(job.ConversationId);
            }
        }
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
}

using HVTravel.Application.Interfaces;
using HVTravel.Application.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HVTravel.Web.Services;

public class MeilisearchBootstrapHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly MeilisearchOptions _options;
    private readonly ILogger<MeilisearchBootstrapHostedService> _logger;

    public MeilisearchBootstrapHostedService(
        IServiceScopeFactory serviceScopeFactory,
        IOptions<MeilisearchOptions> options,
        ILogger<MeilisearchBootstrapHostedService> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _options = options.Value ?? new MeilisearchOptions();
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled || !_options.BootstrapOnStartup)
        {
            return;
        }

        try
        {
            await using var scope = _serviceScopeFactory.CreateAsyncScope();
            var searchIndexingService = scope.ServiceProvider.GetRequiredService<ISearchIndexingService>();

            _logger.LogInformation("Bootstrapping Meilisearch indexes from Mongo.");
            await searchIndexingService.RebuildAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Meilisearch bootstrap was canceled during shutdown.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Meilisearch bootstrap failed. Search will continue with repository fallback.");
        }
    }
}

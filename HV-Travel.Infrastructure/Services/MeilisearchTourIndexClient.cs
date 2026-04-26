using HVTravel.Application.Interfaces;
using HVTravel.Application.Models;
using HVTravel.Application.Services;
using Microsoft.Extensions.Options;

namespace HVTravel.Infrastructure.Services;

public class MeilisearchTourIndexClient : IMeilisearchTourIndexClient
{
    private readonly IMeilisearchDocumentIndexClient _client;
    private readonly MeilisearchOptions _options;

    public MeilisearchTourIndexClient(
        IMeilisearchDocumentIndexClient client,
        IOptions<MeilisearchOptions> options)
    {
        _client = client;
        _options = options.Value ?? new MeilisearchOptions();
    }

    public Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        return _client.IsHealthyAsync(cancellationToken);
    }

    public Task EnsureConfiguredAsync(CancellationToken cancellationToken = default)
    {
        return _client.EnsureConfiguredAsync(MeilisearchIndexDefinitions.Tours(_options), cancellationToken);
    }

    public async Task<MeilisearchTourSearchResponse> SearchAsync(MeilisearchTourSearchCommand command, CancellationToken cancellationToken = default)
    {
        var response = await _client.SearchAsync<TourSearchDocument>(
            MeilisearchIndexDefinitions.Tours(_options),
            new MeilisearchDocumentSearchCommand
            {
                Query = command.Query,
                Limit = command.Limit,
                Offset = command.Offset,
                Filter = command.Filter,
                Sort = command.Sort,
                Facets = command.Facets
            },
            cancellationToken);

        return new MeilisearchTourSearchResponse
        {
            Ids = response.Ids,
            EstimatedTotalHits = response.EstimatedTotalHits,
            FacetDistribution = response.FacetDistribution
        };
    }

    public Task UpsertDocumentsAsync(IReadOnlyCollection<TourSearchDocument> documents, CancellationToken cancellationToken = default)
    {
        return _client.UpsertDocumentsAsync(MeilisearchIndexDefinitions.Tours(_options), documents, cancellationToken);
    }

    public Task DeleteDocumentsAsync(IReadOnlyCollection<string> ids, CancellationToken cancellationToken = default)
    {
        return _client.DeleteDocumentsAsync(MeilisearchIndexDefinitions.Tours(_options), ids, cancellationToken);
    }

    public Task ReplaceAllDocumentsAsync(IReadOnlyCollection<TourSearchDocument> documents, CancellationToken cancellationToken = default)
    {
        return _client.ReplaceAllDocumentsAsync(MeilisearchIndexDefinitions.Tours(_options), documents, cancellationToken);
    }
}

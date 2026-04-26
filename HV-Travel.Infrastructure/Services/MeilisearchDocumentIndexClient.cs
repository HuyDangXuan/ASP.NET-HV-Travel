using System.Net.Http.Headers;
using HVTravel.Application.Interfaces;
using HVTravel.Application.Models;
using Meilisearch;
using Microsoft.Extensions.Options;

namespace HVTravel.Infrastructure.Services;

public class MeilisearchDocumentIndexClient : IMeilisearchDocumentIndexClient
{
    private readonly MeilisearchOptions _options;
    private readonly HttpClient _httpClient;
    private readonly MeilisearchClient _client;
    private readonly SemaphoreSlim _configurationLock = new(1, 1);
    private readonly HashSet<string> _configuredIndexes = new(StringComparer.OrdinalIgnoreCase);

    public MeilisearchDocumentIndexClient(IOptions<MeilisearchOptions> options)
    {
        _options = options.Value ?? new MeilisearchOptions();
        _httpClient = new HttpClient();
        _client = new MeilisearchClient(_options.Url, _options.ApiKey);
    }

    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled || string.IsNullOrWhiteSpace(_options.Url))
        {
            return false;
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(new Uri(_options.Url.TrimEnd('/') + "/"), "health"));
            if (!string.IsNullOrWhiteSpace(_options.ApiKey))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
            }

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task EnsureConfiguredAsync(MeilisearchIndexDefinition definition, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled || _configuredIndexes.Contains(definition.Name))
        {
            return;
        }

        await _configurationLock.WaitAsync(cancellationToken);
        try
        {
            if (_configuredIndexes.Contains(definition.Name))
            {
                return;
            }

            var index = _client.Index(definition.Name);
            await index.UpdateSearchableAttributesAsync(definition.SearchableAttributes.ToArray());
            await index.UpdateFilterableAttributesAsync(definition.FilterableAttributes.ToArray());
            await index.UpdateSortableAttributesAsync(definition.SortableAttributes.ToArray());
            _configuredIndexes.Add(definition.Name);
        }
        finally
        {
            _configurationLock.Release();
        }
    }

    public async Task<MeilisearchDocumentSearchResponse<TDocument>> SearchAsync<TDocument>(
        MeilisearchIndexDefinition definition,
        MeilisearchDocumentSearchCommand command,
        CancellationToken cancellationToken = default)
        where TDocument : class
    {
        await EnsureConfiguredAsync(definition, cancellationToken);

        var index = _client.Index(definition.Name);
        var result = await index.SearchAsync<TDocument>(
            command.Query ?? string.Empty,
            new SearchQuery
            {
                Limit = command.Limit,
                Offset = command.Offset,
                Filter = string.IsNullOrWhiteSpace(command.Filter) ? null : command.Filter,
                Sort = command.Sort.ToArray(),
                Facets = command.Facets.ToArray()
            });

        return new MeilisearchDocumentSearchResponse<TDocument>
        {
            Documents = result.Hits.ToList(),
            Ids = result.Hits
                .Select(item => GetDocumentId(item))
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .ToList()!,
            EstimatedTotalHits = ReadEstimatedTotalHits(result),
            FacetDistribution = ReadFacetDistribution(result)
        };
    }

    public async Task UpsertDocumentsAsync<TDocument>(
        MeilisearchIndexDefinition definition,
        IReadOnlyCollection<TDocument> documents,
        CancellationToken cancellationToken = default)
        where TDocument : class
    {
        await EnsureConfiguredAsync(definition, cancellationToken);
        if (documents.Count == 0)
        {
            return;
        }

        var index = _client.Index(definition.Name);
        await index.UpdateDocumentsAsync(documents);
    }

    public async Task DeleteDocumentsAsync(
        MeilisearchIndexDefinition definition,
        IReadOnlyCollection<string> ids,
        CancellationToken cancellationToken = default)
    {
        await EnsureConfiguredAsync(definition, cancellationToken);
        if (ids.Count == 0)
        {
            return;
        }

        var index = _client.Index(definition.Name);
        await index.DeleteDocumentsAsync(ids.ToArray());
    }

    public async Task ReplaceAllDocumentsAsync<TDocument>(
        MeilisearchIndexDefinition definition,
        IReadOnlyCollection<TDocument> documents,
        CancellationToken cancellationToken = default)
        where TDocument : class
    {
        await EnsureConfiguredAsync(definition, cancellationToken);

        var index = _client.Index(definition.Name);
        await index.DeleteAllDocumentsAsync();
        if (documents.Count != 0)
        {
            await index.AddDocumentsAsync(documents);
        }
    }

    private static string GetDocumentId<TDocument>(TDocument document)
        where TDocument : class
    {
        var property = typeof(TDocument).GetProperty("Id");
        return property?.GetValue(document) as string ?? string.Empty;
    }

    private static IReadOnlyDictionary<string, IReadOnlyDictionary<string, int>> ReadFacetDistribution(object result)
    {
        var output = new Dictionary<string, IReadOnlyDictionary<string, int>>(StringComparer.OrdinalIgnoreCase);
        var property = result.GetType().GetProperty("FacetDistribution") ?? result.GetType().GetProperty("FacetsDistribution");
        if (property?.GetValue(result) is not System.Collections.IDictionary rawDictionary)
        {
            return output;
        }

        foreach (System.Collections.DictionaryEntry entry in rawDictionary)
        {
            if (entry.Key is not string facetKey || entry.Value is not System.Collections.IDictionary values)
            {
                continue;
            }

            var inner = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (System.Collections.DictionaryEntry valueEntry in values)
            {
                if (valueEntry.Key is not string valueKey)
                {
                    continue;
                }

                inner[valueKey] = ConvertToInt(valueEntry.Value);
            }

            output[facetKey] = inner;
        }

        return output;
    }

    private static int ReadEstimatedTotalHits(object result)
    {
        var estimatedTotalHits = result.GetType().GetProperty("EstimatedTotalHits")?.GetValue(result);
        if (estimatedTotalHits != null)
        {
            return ConvertToInt(estimatedTotalHits);
        }

        var totalHits = result.GetType().GetProperty("TotalHits")?.GetValue(result);
        if (totalHits != null)
        {
            return ConvertToInt(totalHits);
        }

        var hits = result.GetType().GetProperty("Hits")?.GetValue(result) as System.Collections.ICollection;
        return hits?.Count ?? 0;
    }

    private static int ConvertToInt(object? value)
    {
        return value switch
        {
            null => 0,
            int intValue => intValue,
            long longValue => (int)longValue,
            _ => int.TryParse(value.ToString(), out var parsed) ? parsed : 0
        };
    }
}

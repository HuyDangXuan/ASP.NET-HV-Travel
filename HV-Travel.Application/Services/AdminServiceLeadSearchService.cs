using HVTravel.Application.Interfaces;
using HVTravel.Application.Models;
using HVTravel.Domain.Entities;
using HVTravel.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HVTravel.Application.Services;

public class AdminServiceLeadSearchService : IAdminServiceLeadSearchService
{
    private readonly IRepository<AncillaryLead> _leadRepository;
    private readonly IMeilisearchDocumentIndexClient _client;
    private readonly MeilisearchOptions _options;
    private readonly ILogger<AdminServiceLeadSearchService> _logger;

    public AdminServiceLeadSearchService(
        IRepository<AncillaryLead> leadRepository,
        IMeilisearchDocumentIndexClient client,
        IOptions<MeilisearchOptions> options,
        ILogger<AdminServiceLeadSearchService> logger)
    {
        _leadRepository = leadRepository;
        _client = client;
        _options = options.Value ?? new MeilisearchOptions();
        _logger = logger;
    }

    public Task<IReadOnlyList<AncillaryLead>> SearchAsync(AdminServiceLeadSearchRequest request, CancellationToken cancellationToken = default)
    {
        request ??= new AdminServiceLeadSearchRequest();
        return SearchFallbackExecutor.ExecuteAsync(
            isAvailableAsync: ct => IsMeilisearchAvailableAsync(ct),
            preferredAsync: ct => SearchWithMeilisearchAsync(request, ct),
            fallbackAsync: () => SearchWithRepositoryAsync(request),
            logger: _logger,
            scope: "AdminServiceLeads",
            cancellationToken: cancellationToken);
    }

    private Task<bool> IsMeilisearchAvailableAsync(CancellationToken cancellationToken)
    {
        return !_options.Enabled
            ? Task.FromResult(false)
            : _client.IsHealthyAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<AncillaryLead>> SearchWithMeilisearchAsync(AdminServiceLeadSearchRequest request, CancellationToken cancellationToken)
    {
        var filters = new List<string>();
        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            filters.Add($"status = {MeilisearchQueryHelpers.Quote(request.Status)}");
        }

        if (!string.IsNullOrWhiteSpace(request.ServiceType))
        {
            filters.Add($"serviceType = {MeilisearchQueryHelpers.Quote(request.ServiceType)}");
        }

        var response = await _client.SearchAsync<ServiceLeadSearchDocument>(
            MeilisearchIndexDefinitions.ServiceLeads(_options),
            new MeilisearchDocumentSearchCommand
            {
                Query = request.Search?.Trim() ?? string.Empty,
                Limit = 1000,
                Filter = MeilisearchQueryHelpers.JoinAnd(filters),
                Sort = ["createdAt:desc"]
            },
            cancellationToken);

        if (response.Ids.Count == 0)
        {
            return Array.Empty<AncillaryLead>();
        }

        return await _leadRepository.GetByIdsAsync(response.Ids);
    }

    private async Task<IReadOnlyList<AncillaryLead>> SearchWithRepositoryAsync(AdminServiceLeadSearchRequest request)
    {
        var leads = (await _leadRepository.GetAllAsync()).AsEnumerable();
        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            leads = leads.Where(lead => string.Equals(lead.Status, request.Status, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(request.ServiceType))
        {
            leads = leads.Where(lead => string.Equals(lead.ServiceType, request.ServiceType, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            leads = leads.Where(lead =>
                lead.FullName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                lead.Email.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                lead.Destination.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        return leads
            .OrderByDescending(static lead => lead.CreatedAt)
            .ToList();
    }
}

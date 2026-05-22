using System.Linq.Expressions;
using HVTravel.Domain.Entities;
using HVTravel.Domain.Interfaces;
using HVTravel.Domain.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;

namespace HVTravel.Web.Tests;

internal sealed class RecordingTourRepository : ITourRepository
{
    private readonly Dictionary<string, Tour> _byId = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Tour> _bySlug = new(StringComparer.Ordinal);

    public int GetByIdCallCount { get; private set; }
    public int GetBySlugCallCount { get; private set; }
    public Tour? LastAdded { get; private set; }
    public Tour? LastUpdated { get; private set; }

    public void Seed(Tour tour)
    {
        _byId[tour.Id] = tour;
        if (!string.IsNullOrWhiteSpace(tour.Slug))
        {
            _bySlug[tour.Slug] = tour;
        }
    }

    public Task<IEnumerable<Tour>> GetAllAsync() => Task.FromResult<IEnumerable<Tour>>(_byId.Values.ToList());

    public Task<Tour> GetByIdAsync(string id)
    {
        GetByIdCallCount++;
        _byId.TryGetValue(id, out var tour);
        return Task.FromResult(tour)!;
    }

    public Task<IEnumerable<Tour>> FindAsync(Expression<Func<Tour, bool>> predicate)
    {
        var compiled = predicate.Compile();
        return Task.FromResult<IEnumerable<Tour>>(_byId.Values.Where(compiled).ToList());
    }

    public Task AddAsync(Tour entity)
    {
        LastAdded = entity;
        Seed(entity);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(string id, Tour entity)
    {
        LastUpdated = entity;
        _byId[id] = entity;
        if (!string.IsNullOrWhiteSpace(entity.Slug))
        {
            _bySlug[entity.Slug] = entity;
        }
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string id)
    {
        _byId.Remove(id);
        return Task.CompletedTask;
    }

    public Task<PaginatedResult<Tour>> GetPagedAsync(int pageIndex, int pageSize, Expression<Func<Tour, bool>>? filter = null)
    {
        var values = _byId.Values.AsEnumerable();
        if (filter != null)
        {
            values = values.Where(filter.Compile());
        }

        var items = values.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToList();
        return Task.FromResult(new PaginatedResult<Tour>(items, values.Count(), pageIndex, pageSize));
    }

    public Task<TourSearchResult> SearchAsync(TourSearchRequest request)
    {
        return Task.FromResult(new TourSearchResult
        {
            Items = _byId.Values.ToList(),
            CurrentPage = 1,
            TotalItems = _byId.Count,
            TotalPages = _byId.Count == 0 ? 0 : 1
        });
    }

    public Task<IReadOnlyList<Tour>> GetByIdsAsync(IEnumerable<string> ids)
    {
        var orderedIds = ids.ToList();
        var items = orderedIds.Where(_byId.ContainsKey).Select(id => _byId[id]).ToList();
        return Task.FromResult<IReadOnlyList<Tour>>(items);
    }

    public Task<Tour?> GetBySlugAsync(string slug)
    {
        GetBySlugCallCount++;
        _bySlug.TryGetValue(slug, out var tour);
        return Task.FromResult(tour);
    }

    public Task<bool> IncrementParticipantsAsync(string tourId, int count) => Task.FromResult(true);

    public Task<bool> ReserveDepartureAsync(string tourId, string departureId, int travellerCount) => Task.FromResult(true);
}

internal sealed class FakeUrlHelper : IUrlHelper
{
    public ActionContext ActionContext { get; } = new(new DefaultHttpContext(), new RouteData(), new ActionDescriptor(), new ModelStateDictionary());

    public string? Action(UrlActionContext actionContext)
    {
        var id = actionContext.Values?.GetType().GetProperty("id")?.GetValue(actionContext.Values)?.ToString() ?? string.Empty;
        var scheme = string.IsNullOrWhiteSpace(actionContext.Protocol) ? "https" : actionContext.Protocol;
        return $"{scheme}://example.test/PublicTours/Details/{id}";
    }

    public string? Content(string? contentPath) => contentPath;
    public bool IsLocalUrl(string? url) => true;
    public string? Link(string? routeName, object? values) => null;
    public string? RouteUrl(UrlRouteContext routeContext) => null;
}

internal static class TestPaths
{
    public static string RepoRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current != null)
        {
            var webDir = Path.Combine(current.FullName, "HV-Travel.Web");
            var domainDir = Path.Combine(current.FullName, "HV-Travel.Domain");
            if (Directory.Exists(webDir) && Directory.Exists(domainDir))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root from test base directory.");
    }

    public static string ReadRepoFile(params string[] segments)
    {
        var path = Path.Combine(new[] { RepoRoot() }.Concat(segments).ToArray());
        return File.ReadAllText(path);
    }
}

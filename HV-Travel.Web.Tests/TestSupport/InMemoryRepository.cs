using System.Linq.Expressions;
using HVTravel.Domain.Interfaces;
using HVTravel.Domain.Models;

namespace HV_Travel.Web.Tests.TestSupport;

internal sealed class InMemoryRepository<T> : IRepository<T> where T : class
{
    private readonly List<T> _items;

    public InMemoryRepository(IEnumerable<T>? items = null)
    {
        _items = items?.ToList() ?? new List<T>();
    }

    public Task<IEnumerable<T>> GetAllAsync()
    {
        return Task.FromResult<IEnumerable<T>>(_items.ToList());
    }

    public Task<T> GetByIdAsync(string id)
    {
        var item = _items.FirstOrDefault(entity => string.Equals(GetId(entity), id, StringComparison.Ordinal));
        return Task.FromResult(item)!;
    }

    public Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
    {
        var compiled = predicate.Compile();
        return Task.FromResult<IEnumerable<T>>(_items.Where(compiled).ToList());
    }

    public Task AddAsync(T entity)
    {
        EnsureId(entity);
        _items.Add(entity);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(string id, T entity)
    {
        var index = _items.FindIndex(item => string.Equals(GetId(item), id, StringComparison.Ordinal));
        if (index >= 0)
        {
            _items[index] = entity;
        }

        return Task.CompletedTask;
    }

    public Task DeleteAsync(string id)
    {
        var index = _items.FindIndex(item => string.Equals(GetId(item), id, StringComparison.Ordinal));
        if (index >= 0)
        {
            _items.RemoveAt(index);
        }

        return Task.CompletedTask;
    }

    public Task<PaginatedResult<T>> GetPagedAsync(int pageIndex, int pageSize, Expression<Func<T, bool>>? filter = null)
    {
        var query = _items.AsEnumerable();
        if (filter != null)
        {
            query = query.Where(filter.Compile());
        }

        var pageItems = query.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToList();
        return Task.FromResult(new PaginatedResult<T>(pageItems, query.Count(), pageIndex, pageSize));
    }

    private static string? GetId(T entity)
    {
        return typeof(T).GetProperty("Id")?.GetValue(entity) as string;
    }

    private static void EnsureId(T entity)
    {
        var property = typeof(T).GetProperty("Id");
        if (property == null || property.PropertyType != typeof(string))
        {
            return;
        }

        var currentValue = property.GetValue(entity) as string;
        if (!string.IsNullOrWhiteSpace(currentValue))
        {
            return;
        }

        property.SetValue(entity, Guid.NewGuid().ToString("N"));
    }
}

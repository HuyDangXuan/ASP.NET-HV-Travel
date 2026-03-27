using System.Linq.Expressions;
using HVTravel.Domain.Entities;
using HVTravel.Domain.Interfaces;
using HVTravel.Domain.Models;
using HVTravel.Web.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace HV_Travel.Web.Tests;

public class PublicToursControllerSearchTests
{
    [Fact]
    public async Task Index_SearchMatchesPlainTextWhenDescriptionContainsHtmlEntities()
    {
        var repository = new InMemoryRepository<Tour>(
        [
            new Tour
            {
                Id = "tour-1",
                Name = "Khám phá Hà N?i",
                Description = "<p>Ðu?c tham quan c&aacute;c danh lam th?ng c?nh</p>",
                ShortDescription = "<p>Tr?i nghi?m th? dô</p>",
                Status = "Active",
                Destination = new Destination { City = "Hà N?i", Country = "Vi?t Nam", Region = "Mi?n B?c" },
                Price = new TourPrice { Adult = 1000000 },
                Duration = new TourDuration { Days = 2, Nights = 1, Text = "2 ngày 1 dêm" }
            }
        ]);

        var controller = new PublicToursController(repository);

        var result = await controller.Index(search: "các", sort: null);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<List<Tour>>(view.Model);
        var tour = Assert.Single(model);
        Assert.Equal("tour-1", tour.Id);
    }

    private sealed class InMemoryRepository<T> : IRepository<T> where T : class
    {
        private readonly List<T> _items;

        public InMemoryRepository(IEnumerable<T> items)
        {
            _items = items.ToList();
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

            var items = query.Skip(pageIndex * pageSize).Take(pageSize).ToList();
            return Task.FromResult(new PaginatedResult<T>(items, query.Count(), pageIndex, pageSize));
        }

        private static string? GetId(T entity)
        {
            return typeof(T).GetProperty("Id")?.GetValue(entity) as string;
        }
    }
}

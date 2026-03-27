using System.Linq.Expressions;
using HVTravel.Domain.Entities;
using HVTravel.Domain.Interfaces;
using HVTravel.Domain.Models;
using HVTravel.Web.Areas.Admin.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;

namespace HV_Travel.Web.Tests;

public class AdminToursControllerRichTextNormalizationTests
{
    [Fact]
    public async Task Create_NormalizesDoubleEncodedRichTextBeforeSaving()
    {
        var repository = new InMemoryRepository<Tour>();
        var controller = CreateController(repository);
        var tour = new Tour
        {
            Id = "tour-1",
            Name = "Kham pha Ha Noi",
            Description = "&amp;lt;p&amp;gt;Mo ta&amp;lt;/p&amp;gt;",
            ShortDescription = "&amp;lt;p&amp;gt;Tom tat&amp;lt;/p&amp;gt;",
            Destination = new Destination { City = "Ha Noi", Country = "Viet Nam", Region = "Mien Bac" },
            Price = new TourPrice { Adult = 1000000 },
            Duration = new TourDuration { Days = 2, Nights = 1, Text = "2 ngay 1 dem" },
            Schedule =
            [
                new ScheduleItem
                {
                    Day = 1,
                    Title = "Ngay dau",
                    Description = "&amp;lt;p&amp;gt;Hoat dong ngay thu nhat&amp;lt;/p&amp;gt;"
                }
            ]
        };

        var result = await controller.Create(tour, saveAction: null);

        Assert.IsType<RedirectToActionResult>(result);
        var saved = Assert.Single(repository.Items);
        Assert.Equal("<p>Mo ta</p>", saved.Description);
        Assert.Equal("<p>Tom tat</p>", saved.ShortDescription);
        Assert.Equal("<p>Hoat dong ngay thu nhat</p>", saved.Schedule.Single().Description);
    }

    [Fact]
    public async Task Edit_NormalizesDoubleEncodedRichTextBeforeUpdating()
    {
        var existing = new Tour
        {
            Id = "tour-2",
            Name = "Tour cu",
            Description = "<p>Cu</p>",
            ShortDescription = "<p>Cu</p>",
            Destination = new Destination { City = "Ha Noi", Country = "Viet Nam", Region = "Mien Bac" },
            Price = new TourPrice { Adult = 1000000 },
            Duration = new TourDuration { Days = 2, Nights = 1, Text = "2 ngay 1 dem" },
            Schedule = [new ScheduleItem { Day = 1, Title = "Ngay dau", Description = "<p>Cu</p>" }]
        };
        var repository = new InMemoryRepository<Tour>([existing]);
        var controller = CreateController(repository);

        var updated = new Tour
        {
            Id = "tour-2",
            Name = "Tour moi",
            Description = "&amp;lt;p&amp;gt;Mo ta moi&amp;lt;/p&amp;gt;",
            ShortDescription = "&amp;lt;p&amp;gt;Tom tat moi&amp;lt;/p&amp;gt;",
            Destination = new Destination { City = "Ha Noi", Country = "Viet Nam", Region = "Mien Bac" },
            Price = new TourPrice { Adult = 1200000 },
            Duration = new TourDuration { Days = 3, Nights = 2, Text = "3 ngay 2 dem" },
            Schedule = [new ScheduleItem { Day = 1, Title = "Ngay dau", Description = "&amp;lt;p&amp;gt;Ngay moi&amp;lt;/p&amp;gt;" }]
        };

        var result = await controller.Edit("tour-2", updated, saveAction: null);

        Assert.IsType<RedirectToActionResult>(result);
        var saved = Assert.Single(repository.Items);
        Assert.Equal("<p>Mo ta moi</p>", saved.Description);
        Assert.Equal("<p>Tom tat moi</p>", saved.ShortDescription);
        Assert.Equal("<p>Ngay moi</p>", saved.Schedule.Single().Description);
    }

    private static ToursController CreateController(IRepository<Tour> repository)
    {
        var controller = new ToursController(repository);
        var httpContext = new DefaultHttpContext();
        httpContext.Features.Set<IFormFeature>(new StubFormFeature(new FormCollection(new Dictionary<string, StringValues>())));
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        return controller;
    }

    private sealed class StubFormFeature : IFormFeature
    {
        public StubFormFeature(IFormCollection form)
        {
            Form = form;
        }

        public bool HasFormContentType => true;

        public IFormCollection? Form { get; set; }

        public IFormCollection ReadForm()
        {
            return Form ?? new FormCollection(new Dictionary<string, StringValues>());
        }

        public Task<IFormCollection> ReadFormAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(ReadForm());
        }
    }

    private sealed class InMemoryRepository<T> : IRepository<T> where T : class
    {
        public List<T> Items { get; }

        public InMemoryRepository()
        {
            Items = [];
        }

        public InMemoryRepository(IEnumerable<T> items)
        {
            Items = items.ToList();
        }

        public Task<IEnumerable<T>> GetAllAsync() => Task.FromResult<IEnumerable<T>>(Items.ToList());

        public Task<T> GetByIdAsync(string id)
        {
            var item = Items.FirstOrDefault(entity => string.Equals(GetId(entity), id, StringComparison.Ordinal));
            return Task.FromResult(item)!;
        }

        public Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
        {
            var compiled = predicate.Compile();
            return Task.FromResult<IEnumerable<T>>(Items.Where(compiled).ToList());
        }

        public Task AddAsync(T entity)
        {
            Items.Add(entity);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(string id, T entity)
        {
            var index = Items.FindIndex(item => string.Equals(GetId(item), id, StringComparison.Ordinal));
            if (index >= 0)
            {
                Items[index] = entity;
            }

            return Task.CompletedTask;
        }

        public Task DeleteAsync(string id)
        {
            var index = Items.FindIndex(item => string.Equals(GetId(item), id, StringComparison.Ordinal));
            if (index >= 0)
            {
                Items.RemoveAt(index);
            }

            return Task.CompletedTask;
        }

        public Task<PaginatedResult<T>> GetPagedAsync(int pageIndex, int pageSize, Expression<Func<T, bool>>? filter = null)
        {
            var query = Items.AsEnumerable();
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

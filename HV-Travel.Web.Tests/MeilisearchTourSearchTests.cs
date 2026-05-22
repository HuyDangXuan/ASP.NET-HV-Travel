using System.Linq.Expressions;
using HVTravel.Application.Interfaces;
using HVTravel.Application.Models;
using HVTravel.Application.Services;
using HVTravel.Domain.Entities;
using HVTravel.Domain.Interfaces;
using HVTravel.Domain.Models;
using Microsoft.Extensions.Options;
using Xunit;

namespace HVTravel.Web.Tests;

public class MeilisearchTourSearchTests
{
    [Fact]
    public async Task TourSearchService_UsesAvailablePreferredBackend_WhenHealthy()
    {
        var repo = new SearchTestTourRepository([]);
        var mongoBackend = new StubTourSearchBackend("mongo", priority: 0, available: true, buildResult("mongo-tour"));
        var meiliBackend = new StubTourSearchBackend("meili", priority: 100, available: true, buildResult("meili-tour"));
        var service = new TourSearchService(repo, new RouteRecommendationService(new RouteInsightService()), [mongoBackend, meiliBackend]);

        var result = await service.SearchAsync(new TourSearchRequest
        {
            Page = 1,
            PageSize = 10,
            PublicOnly = true
        });

        Assert.Single(result.Items);
        Assert.Equal("meili-tour", result.Items[0].Id);
        Assert.Equal(1, meiliBackend.SearchCallCount);
        Assert.Equal(0, mongoBackend.SearchCallCount);
    }

    [Fact]
    public async Task TourSearchService_FallsBackToMongo_WhenPreferredBackendUnavailable()
    {
        var repo = new SearchTestTourRepository([]);
        var mongoBackend = new StubTourSearchBackend("mongo", priority: 0, available: true, buildResult("mongo-tour"));
        var meiliBackend = new StubTourSearchBackend("meili", priority: 100, available: false, buildResult("meili-tour"));
        var service = new TourSearchService(repo, new RouteRecommendationService(new RouteInsightService()), [mongoBackend, meiliBackend]);

        var result = await service.SearchAsync(new TourSearchRequest
        {
            Page = 1,
            PageSize = 10,
            PublicOnly = true
        });

        Assert.Single(result.Items);
        Assert.Equal("mongo-tour", result.Items[0].Id);
        Assert.Equal(0, meiliBackend.SearchCallCount);
        Assert.Equal(1, mongoBackend.SearchCallCount);
    }

    [Fact]
    public async Task TourSearchService_FallsBackToMongo_WhenPreferredBackendThrows()
    {
        var repo = new SearchTestTourRepository([]);
        var mongoBackend = new StubTourSearchBackend("mongo", priority: 0, available: true, buildResult("mongo-tour"));
        var meiliBackend = new StubTourSearchBackend("meili", priority: 100, available: true, buildResult("meili-tour"))
        {
            ThrowOnSearch = true
        };

        var service = new TourSearchService(repo, new RouteRecommendationService(new RouteInsightService()), [mongoBackend, meiliBackend]);

        var result = await service.SearchAsync(new TourSearchRequest
        {
            Page = 1,
            PageSize = 10,
            PublicOnly = true
        });

        Assert.Single(result.Items);
        Assert.Equal("mongo-tour", result.Items[0].Id);
        Assert.Equal(1, meiliBackend.SearchCallCount);
        Assert.Equal(1, mongoBackend.SearchCallCount);
    }

    [Fact]
    public async Task MeilisearchTourSearchBackend_HydratesHits_UsingRepositoryOrder()
    {
        var repo = new SearchTestTourRepository(
        [
            BuildTour("tour-1", "Da Nang Escape", "Da Nang", "Mien Trung", "Active"),
            BuildTour("tour-2", "Hue Highlights", "Hue", "Mien Trung", "Active"),
            BuildTour("tour-3", "Ha Noi Discovery", "Ha Noi", "Mien Bac", "Active")
        ]);

        var client = new StubMeilisearchTourIndexClient
        {
            IsHealthy = true,
            SearchResponse = new MeilisearchTourSearchResponse
            {
                Ids = ["tour-3", "tour-1"],
                EstimatedTotalHits = 2
            }
        };

        var backend = new MeilisearchTourSearchBackend(
            client,
            repo,
            Options.Create(new MeilisearchOptions
            {
                Enabled = true,
                IndexName = "tours"
            }));

        var result = await backend.SearchAsync(new TourSearchRequest
        {
            Search = "tour",
            Page = 1,
            PageSize = 10,
            PublicOnly = true
        });

        Assert.Equal(["tour-3", "tour-1"], result.Items.Select(item => item.Id).ToArray());
        Assert.Equal(["tour-3", "tour-1"], repo.LastRequestedIds);
    }

    [Fact]
    public async Task MeilisearchTourSearchBackend_MapsSupportedFiltersAndSorting()
    {
        var repo = new SearchTestTourRepository([]);
        var client = new StubMeilisearchTourIndexClient
        {
            IsHealthy = true,
            SearchResponse = new MeilisearchTourSearchResponse()
        };

        var backend = new MeilisearchTourSearchBackend(
            client,
            repo,
            Options.Create(new MeilisearchOptions
            {
                Enabled = true,
                IndexName = "tours"
            }));

        await backend.SearchAsync(new TourSearchRequest
        {
            Search = "da nang",
            Region = "Mien Trung",
            Destination = "Da Nang",
            MinPrice = 1000000m,
            MaxPrice = 3000000m,
            DepartureMonth = 6,
            MaxDays = 5,
            Collection = "budget",
            AvailableOnly = true,
            Travellers = 2,
            ConfirmationType = "Instant",
            CancellationType = "FreeCancellation",
            PromotionOnly = true,
            Sort = "price_asc",
            Page = 2,
            PageSize = 9,
            PublicOnly = true
        });

        Assert.NotNull(client.LastSearchCommand);
        Assert.Equal("da nang", client.LastSearchCommand!.Query);
        Assert.Equal(9, client.LastSearchCommand.Limit);
        Assert.Equal(9, client.LastSearchCommand.Offset);
        Assert.Contains("status IN", client.LastSearchCommand.Filter, StringComparison.Ordinal);
        Assert.Contains("region =", client.LastSearchCommand.Filter, StringComparison.Ordinal);
        Assert.Contains("destination =", client.LastSearchCommand.Filter, StringComparison.Ordinal);
        Assert.Contains("startingAdultPrice >=", client.LastSearchCommand.Filter, StringComparison.Ordinal);
        Assert.Contains("startingAdultPrice <=", client.LastSearchCommand.Filter, StringComparison.Ordinal);
        Assert.Contains("durationDays <=", client.LastSearchCommand.Filter, StringComparison.Ordinal);
        Assert.Contains("departureMonths =", client.LastSearchCommand.Filter, StringComparison.Ordinal);
        Assert.Contains("maxRemainingCapacity >=", client.LastSearchCommand.Filter, StringComparison.Ordinal);
        Assert.Contains("confirmationTypes =", client.LastSearchCommand.Filter, StringComparison.Ordinal);
        Assert.Contains("isFreeCancellation = true", client.LastSearchCommand.Filter, StringComparison.Ordinal);
        Assert.Contains("hasPromotion = true", client.LastSearchCommand.Filter, StringComparison.Ordinal);
        Assert.Contains("isBudget = true", client.LastSearchCommand.Filter, StringComparison.Ordinal);
        Assert.Equal(["startingAdultPrice:asc"], client.LastSearchCommand.Sort);
    }

    [Fact]
    public void TourMutationSources_InvokeSearchIndexingService()
    {
        var controllerSource = TestPaths.ReadRepoFile("HV-Travel.Web", "Areas", "Admin", "Controllers", "ToursController.cs");
        var serviceSource = TestPaths.ReadRepoFile("HV-Travel.Application", "Services", "TourService.cs");

        Assert.Contains("ITourSearchIndexingService", controllerSource, StringComparison.Ordinal);
        Assert.Contains("UpsertTourAsync", controllerSource, StringComparison.Ordinal);
        Assert.Contains("DeleteTourAsync", controllerSource, StringComparison.Ordinal);
        Assert.Contains("ITourSearchIndexingService", serviceSource, StringComparison.Ordinal);
        Assert.Contains("UpsertTourAsync", serviceSource, StringComparison.Ordinal);
        Assert.Contains("DeleteTourAsync", serviceSource, StringComparison.Ordinal);
    }

    [Fact]
    public void PublicTours_CollectionChips_OnlyUseSupportedKeys()
    {
        var source = TestPaths.ReadRepoFile("HV-Travel.Web", "Views", "PublicTours", "Index.cshtml");
        var defaultsSource = TestPaths.ReadRepoFile("HV-Travel.Web", "Services", "PublicContentDefaults.cs");

        Assert.Contains("Key = \"domestic\"", source, StringComparison.Ordinal);
        Assert.Contains("Key = \"international\"", source, StringComparison.Ordinal);
        Assert.Contains("Key = \"premium\"", source, StringComparison.Ordinal);
        Assert.Contains("Key = \"budget\"", source, StringComparison.Ordinal);
        Assert.Contains("Key = \"deal\"", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Key = \"family\"", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Key = \"couple\"", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Key = \"seasonal\"", source, StringComparison.Ordinal);

        Assert.Contains("internationalLabel", defaultsSource, StringComparison.Ordinal);
        Assert.Contains("budgetLabel", defaultsSource, StringComparison.Ordinal);
    }

    [Fact]
    public void SearchInfrastructure_DeclaresPinnedMeilisearchConfig_AndDockerService()
    {
        var appSettings = TestPaths.ReadRepoFile("HV-Travel.Web", "appsettings.json");
        var dockerCompose = TestPaths.ReadRepoFile("docker-compose.yml");
        var dockerComposeDev = TestPaths.ReadRepoFile("docker-compose.dev.yml");
        var infrastructureProject = TestPaths.ReadRepoFile("HV-Travel.Infrastructure", "HV-Travel.Infrastructure.csproj");

        Assert.Contains("\"Meilisearch\"", appSettings, StringComparison.Ordinal);
        Assert.Contains("\"BootstrapOnStartup\"", appSettings, StringComparison.Ordinal);
        Assert.Contains("meilisearch:", dockerCompose, StringComparison.Ordinal);
        Assert.Contains("getmeili/meilisearch:v1.37", dockerCompose, StringComparison.Ordinal);
        Assert.DoesNotContain("getmeili/meilisearch:latest", dockerCompose, StringComparison.Ordinal);
        Assert.Contains("meilisearch:", dockerComposeDev, StringComparison.Ordinal);
        Assert.Contains("PackageReference Include=\"MeiliSearch\"", infrastructureProject, StringComparison.Ordinal);
    }

    private static TourSearchResult buildResult(string id)
    {
        return new TourSearchResult
        {
            Items = [BuildTour(id, id, "Da Nang", "Mien Trung", "Active")],
            TotalItems = 1,
            TotalPages = 1,
            CurrentPage = 1
        };
    }

    private static Tour BuildTour(string id, string name, string city, string region, string status)
    {
        return new Tour
        {
            Id = id,
            Slug = id,
            Code = id.ToUpperInvariant(),
            Name = name,
            Description = $"{name} description",
            ShortDescription = $"{name} short description",
            Status = status,
            CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Destination = new Destination
            {
                City = city,
                Region = region,
                Country = "Vietnam"
            },
            Duration = new TourDuration
            {
                Days = 3,
                Nights = 2,
                Text = "3 ngày 2 đêm"
            },
            Price = new TourPrice
            {
                Adult = 1500000m,
                Currency = "VND"
            },
            Rating = 4.7,
            BadgeSet = ["deal"],
            Highlights = ["Ẩm thực", "Biển"],
            Departures =
            [
                new TourDeparture
                {
                    Id = $"dep-{id}",
                    StartDate = new DateTime(2025, 6, 10, 0, 0, 0, DateTimeKind.Utc),
                    AdultPrice = 1500000m,
                    Capacity = 10,
                    BookedCount = 4,
                    ConfirmationType = "Instant",
                    Status = "Scheduled"
                }
            ],
            CancellationPolicy = new TourCancellationPolicy
            {
                IsFreeCancellation = true,
                FreeCancellationBeforeHours = 48
            },
            Routing = new TourRouting
            {
                SchemaVersion = 1,
                Stops =
                [
                    new TourRouteStop
                    {
                        Id = $"stop-{id}",
                        Day = 1,
                        Order = 1,
                        Name = "Điểm đón",
                        Type = "meeting",
                        VisitMinutes = 20,
                        Coordinates = new GeoPoint
                        {
                            Lat = 16.0471,
                            Lng = 108.2062
                        }
                    }
                ]
            }
        };
    }

    private sealed class SearchTestTourRepository : ITourRepository
    {
        private readonly Dictionary<string, Tour> _tours;

        public SearchTestTourRepository(IEnumerable<Tour> tours)
        {
            _tours = tours.ToDictionary(item => item.Id, StringComparer.Ordinal);
        }

        public IReadOnlyList<string> LastRequestedIds { get; private set; } = Array.Empty<string>();

        public Task<IEnumerable<Tour>> GetAllAsync() => Task.FromResult<IEnumerable<Tour>>(_tours.Values.ToList());

        public Task<Tour> GetByIdAsync(string id) => Task.FromResult(_tours[id]);

        public Task<IEnumerable<Tour>> FindAsync(Expression<Func<Tour, bool>> predicate)
        {
            var compiled = predicate.Compile();
            return Task.FromResult<IEnumerable<Tour>>(_tours.Values.Where(compiled).ToList());
        }

        public Task AddAsync(Tour entity)
        {
            _tours[entity.Id] = entity;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(string id, Tour entity)
        {
            _tours[id] = entity;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(string id)
        {
            _tours.Remove(id);
            return Task.CompletedTask;
        }

        public Task<PaginatedResult<Tour>> GetPagedAsync(int pageIndex, int pageSize, Expression<Func<Tour, bool>>? filter = null)
        {
            var values = _tours.Values.AsEnumerable();
            if (filter != null)
            {
                values = values.Where(filter.Compile());
            }

            var items = values.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToList();
            return Task.FromResult(new PaginatedResult<Tour>(items, values.Count(), pageIndex, pageSize));
        }

        public Task<TourSearchResult> SearchAsync(TourSearchRequest request)
        {
            var items = _tours.Values
                .Where(tour => !request.PublicOnly || tour.Status is "Active" or "ComingSoon" or "SoldOut")
                .ToList();

            return Task.FromResult(new TourSearchResult
            {
                Items = items,
                TotalItems = items.Count,
                TotalPages = items.Count == 0 ? 0 : 1,
                CurrentPage = 1
            });
        }

        public Task<IReadOnlyList<Tour>> GetByIdsAsync(IEnumerable<string> ids)
        {
            var orderedIds = ids.ToList();
            LastRequestedIds = orderedIds;
            return Task.FromResult<IReadOnlyList<Tour>>(orderedIds.Where(_tours.ContainsKey).Select(id => _tours[id]).ToList());
        }

        public Task<Tour?> GetBySlugAsync(string slug)
        {
            return Task.FromResult<Tour?>(_tours.Values.FirstOrDefault(item => string.Equals(item.Slug, slug, StringComparison.Ordinal)));
        }

        public Task<bool> IncrementParticipantsAsync(string tourId, int count) => Task.FromResult(true);

        public Task<bool> ReserveDepartureAsync(string tourId, string departureId, int travellerCount) => Task.FromResult(true);
    }

    private sealed class StubTourSearchBackend : ITourSearchBackend
    {
        private readonly TourSearchResult _result;

        public StubTourSearchBackend(string name, int priority, bool available, TourSearchResult result)
        {
            Name = name;
            Priority = priority;
            Available = available;
            _result = result;
        }

        public string Name { get; }

        public int Priority { get; }

        public bool Available { get; set; }

        public bool ThrowOnSearch { get; set; }

        public int SearchCallCount { get; private set; }

        public Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Available);
        }

        public Task<TourSearchResult> SearchAsync(TourSearchRequest request, CancellationToken cancellationToken = default)
        {
            SearchCallCount++;
            if (ThrowOnSearch)
            {
                throw new InvalidOperationException($"{Name} failed");
            }

            return Task.FromResult(_result);
        }
    }

    private sealed class StubMeilisearchTourIndexClient : IMeilisearchTourIndexClient
    {
        public bool IsHealthy { get; set; }

        public MeilisearchTourSearchCommand? LastSearchCommand { get; private set; }

        public MeilisearchTourSearchResponse SearchResponse { get; set; } = new();

        public Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(IsHealthy);
        }

        public Task EnsureConfiguredAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<MeilisearchTourSearchResponse> SearchAsync(MeilisearchTourSearchCommand command, CancellationToken cancellationToken = default)
        {
            LastSearchCommand = command;
            return Task.FromResult(SearchResponse);
        }

        public Task UpsertDocumentsAsync(IReadOnlyCollection<TourSearchDocument> documents, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task DeleteDocumentsAsync(IReadOnlyCollection<string> ids, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task ReplaceAllDocumentsAsync(IReadOnlyCollection<TourSearchDocument> documents, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}

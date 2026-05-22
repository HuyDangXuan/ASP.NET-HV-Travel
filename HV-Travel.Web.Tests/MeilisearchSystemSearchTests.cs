using System.Linq.Expressions;
using HVTravel.Application.Interfaces;
using HVTravel.Application.Models;
using HVTravel.Application.Services;
using HVTravel.Domain.Entities;
using HVTravel.Domain.Interfaces;
using HVTravel.Domain.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace HVTravel.Web.Tests;

public class MeilisearchSystemSearchTests
{
    [Fact]
    public async Task BookingLookupSearchService_UsesMeilisearchFilters_ForExactLookup()
    {
        var booking = BuildBooking("booking-1", "HV0001", "alice@example.com", "0901 222 333");
        var bookingRepository = new InMemoryRepository<Booking>([booking]);
        var client = new StubMeilisearchDocumentIndexClient
        {
            IsHealthy = true
        };
        client.SetResponse(
            "hv_travel_bookings",
            new MeilisearchDocumentSearchResponse<BookingSearchDocument>
            {
                Ids = [booking.Id],
                Documents =
                [
                    new BookingSearchDocument
                    {
                        Id = booking.Id,
                        BookingCode = booking.BookingCode
                    }
                ],
                EstimatedTotalHits = 1
            });

        var service = new BookingLookupSearchService(
            bookingRepository,
            client,
            Options.Create(new MeilisearchOptions
            {
                Enabled = true,
                Indexes = new MeilisearchIndexOptions
                {
                    Bookings = "hv_travel_bookings"
                }
            }),
            NullLogger<BookingLookupSearchService>.Instance);

        var result = await service.LookupAsync(new BookingLookupSearchRequest
        {
            BookingCode = "HV0001",
            Email = "alice@example.com"
        });

        Assert.NotNull(result);
        Assert.Equal("booking-1", result!.Id);
        Assert.NotNull(client.LastCommands["hv_travel_bookings"]);
        Assert.Contains("bookingCode =", client.LastCommands["hv_travel_bookings"]!.Filter, StringComparison.Ordinal);
        Assert.Contains("contactEmail =", client.LastCommands["hv_travel_bookings"]!.Filter, StringComparison.Ordinal);
        Assert.Contains("publicLookupEnabled = true", client.LastCommands["hv_travel_bookings"]!.Filter, StringComparison.Ordinal);
    }

    [Fact]
    public async Task BookingLookupSearchService_FallsBackToRepository_WhenMeilisearchUnavailable()
    {
        var booking = BuildBooking("booking-1", "HV0001", "alice@example.com", "0901 222 333");
        var bookingRepository = new InMemoryRepository<Booking>([booking]);
        var service = new BookingLookupSearchService(
            bookingRepository,
            new StubMeilisearchDocumentIndexClient { IsHealthy = false },
            Options.Create(new MeilisearchOptions
            {
                Enabled = true,
                Indexes = new MeilisearchIndexOptions
                {
                    Bookings = "hv_travel_bookings"
                }
            }),
            NullLogger<BookingLookupSearchService>.Instance);

        var result = await service.LookupAsync(new BookingLookupSearchRequest
        {
            BookingCode = "HV0001",
            Phone = "0901222333"
        });

        Assert.NotNull(result);
        Assert.Equal("booking-1", result!.Id);
    }

    [Fact]
    public async Task AdminCustomerSearchService_UsesMeilisearchDocumentAggregates_AndFacets()
    {
        var customer = new Customer
        {
            Id = "customer-1",
            CustomerCode = "CUS000001",
            FullName = "Alice Nguyen",
            Email = "alice@example.com",
            PhoneNumber = "0901222333",
            Segment = "VIP",
            Status = "Active",
            Stats = new CustomerStats()
        };

        var customerRepository = new InMemoryRepository<Customer>([customer]);
        var bookingRepository = new InMemoryRepository<Booking>([]);
        var client = new StubMeilisearchDocumentIndexClient
        {
            IsHealthy = true
        };
        client.SetResponse(
            "hv_travel_customers",
            new MeilisearchDocumentSearchResponse<CustomerSearchDocument>
            {
                Ids = [customer.Id],
                Documents =
                [
                    new CustomerSearchDocument
                    {
                        Id = customer.Id,
                        FullName = customer.FullName,
                        Segment = "VIP",
                        TotalOrders = 4,
                        TotalSpending = 22000000m
                    }
                ],
                EstimatedTotalHits = 1,
                FacetDistribution = new Dictionary<string, IReadOnlyDictionary<string, int>>(StringComparer.OrdinalIgnoreCase)
                {
                    ["segment"] = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["VIP"] = 1
                    }
                }
            });

        var service = new AdminCustomerSearchService(
            customerRepository,
            bookingRepository,
            client,
            Options.Create(new MeilisearchOptions
            {
                Enabled = true,
                Indexes = new MeilisearchIndexOptions
                {
                    Customers = "hv_travel_customers"
                }
            }),
            NullLogger<AdminCustomerSearchService>.Instance);

        var result = await service.SearchAsync(new AdminCustomerSearchRequest
        {
            SearchQuery = "Alice",
            Page = 1,
            PageSize = 10
        });

        var item = Assert.Single(result.Page.Items);
        Assert.Equal(4, item.Stats.TotalOrders);
        Assert.Equal(22000000m, item.Stats.TotalSpending);
        Assert.Equal(1, result.TotalCustomers);
        Assert.Equal(0, result.NewCustomersCount);
        Assert.Equal(100d, result.SegmentPercentages["VIP"]);
    }

    [Fact]
    public void SearchControllers_UseSearchServices_InsteadOfDirectControllerLevelMongoSearch()
    {
        var publicToursSource = TestPaths.ReadRepoFile("HV-Travel.Web", "Controllers", "PublicToursController.cs");
        var bookingLookupSource = TestPaths.ReadRepoFile("HV-Travel.Web", "Controllers", "BookingLookupController.cs");
        var bookingsSource = TestPaths.ReadRepoFile("HV-Travel.Web", "Areas", "Admin", "Controllers", "BookingsController.cs");
        var usersSource = TestPaths.ReadRepoFile("HV-Travel.Web", "Areas", "Admin", "Controllers", "UsersController.cs");
        var reviewsSource = TestPaths.ReadRepoFile("HV-Travel.Web", "Areas", "Admin", "Controllers", "ReviewsController.cs");
        var serviceLeadsSource = TestPaths.ReadRepoFile("HV-Travel.Web", "Areas", "Admin", "Controllers", "ServiceLeadsController.cs");
        var customersSource = TestPaths.ReadRepoFile("HV-Travel.Web", "Areas", "Admin", "Controllers", "CustomersController.cs");
        var paymentsSource = TestPaths.ReadRepoFile("HV-Travel.Web", "Areas", "Admin", "Controllers", "PaymentsController.cs");

        Assert.Contains("_tourSearchService.SearchAsync", publicToursSource, StringComparison.Ordinal);
        Assert.Contains("IBookingLookupSearchService", bookingLookupSource, StringComparison.Ordinal);
        Assert.Contains("IAdminBookingSearchService", bookingsSource, StringComparison.Ordinal);
        Assert.Contains("IAdminUserSearchService", usersSource, StringComparison.Ordinal);
        Assert.Contains("IAdminReviewSearchService", reviewsSource, StringComparison.Ordinal);
        Assert.Contains("IAdminServiceLeadSearchService", serviceLeadsSource, StringComparison.Ordinal);
        Assert.Contains("IAdminCustomerSearchService", customersSource, StringComparison.Ordinal);
        Assert.Contains("IAdminPaymentSearchService", paymentsSource, StringComparison.Ordinal);
    }

    [Fact]
    public void SearchIndexingInfrastructure_CoversMultiIndexConfig_AndBootstrap()
    {
        var appSettings = TestPaths.ReadRepoFile("HV-Travel.Web", "appsettings.json");
        var dockerCompose = TestPaths.ReadRepoFile("docker-compose.yml");
        var bootstrapSource = TestPaths.ReadRepoFile("HV-Travel.Web", "Services", "MeilisearchBootstrapHostedService.cs");
        var indexingSource = TestPaths.ReadRepoFile("HV-Travel.Application", "Services", "SearchIndexingService.cs");

        Assert.Contains("\"Indexes\"", appSettings, StringComparison.Ordinal);
        Assert.Contains("\"Bookings\": \"hv_travel_bookings\"", appSettings, StringComparison.Ordinal);
        Assert.Contains("\"Customers\": \"hv_travel_customers\"", appSettings, StringComparison.Ordinal);
        Assert.Contains("Meilisearch__Indexes__Bookings=hv_travel_bookings", dockerCompose, StringComparison.Ordinal);
        Assert.Contains("Meilisearch__Indexes__Payments=hv_travel_payments", dockerCompose, StringComparison.Ordinal);
        Assert.Contains("ISearchIndexingService", bootstrapSource, StringComparison.Ordinal);
        Assert.Contains("UpsertBookingAsync", indexingSource, StringComparison.Ordinal);
        Assert.Contains("UpsertUserAsync", indexingSource, StringComparison.Ordinal);
        Assert.Contains("UpsertReviewAsync", indexingSource, StringComparison.Ordinal);
        Assert.Contains("UpsertServiceLeadAsync", indexingSource, StringComparison.Ordinal);
        Assert.Contains("UpsertCustomerAsync", indexingSource, StringComparison.Ordinal);
        Assert.Contains("ReplaceAllDocumentsAsync", indexingSource, StringComparison.Ordinal);
    }

    [Fact]
    public void AdminCustomers_Index_Preserves_And_Exposes_PageSize_Selection()
    {
        var customersControllerSource = TestPaths.ReadRepoFile("HV-Travel.Web", "Areas", "Admin", "Controllers", "CustomersController.cs");
        var customersViewSource = TestPaths.ReadRepoFile("HV-Travel.Web", "Areas", "Admin", "Views", "Customers", "Index.cshtml");

        Assert.Contains("ViewBag.CurrentPageSize = pageSize;", customersControllerSource, StringComparison.Ordinal);
        Assert.Contains("name=\"pageSize\"", customersViewSource, StringComparison.Ordinal);
        Assert.Contains("id=\"pageSize\"", customersViewSource, StringComparison.Ordinal);
        Assert.Contains("changePageSize(this.value)", customersViewSource, StringComparison.Ordinal);
        Assert.Contains("pageSize = currentPageSize", customersViewSource, StringComparison.Ordinal);
    }

    [Fact]
    public void AdminCustomers_Index_Aligns_PageSize_Group_With_Result_Count()
    {
        var customersViewSource = NormalizeWhitespace(TestPaths.ReadRepoFile("HV-Travel.Web", "Areas", "Admin", "Views", "Customers", "Index.cshtml"));

        Assert.Contains("flex flex-col sm:flex-row justify-between items-center gap-4", customersViewSource, StringComparison.Ordinal);
        Assert.Contains("flex items-center gap-4\"> <p class=\"text-xs text-slate-500 dark:text-slate-400\">", customersViewSource, StringComparison.Ordinal);
        Assert.Contains("@Model.Pagination.TotalCount</span> k&#7871;t qu&#7843; </p> <div class=\"flex items-center gap-2\"> <label for=\"pageSize\"", customersViewSource, StringComparison.Ordinal);
        Assert.Contains("</div> </div> <div class=\"flex items-center gap-2\"> <!-- Prev -->", customersViewSource, StringComparison.Ordinal);
    }

    [Fact]
    public void AdminCompactSelect_Uses_Shared_Stacking_And_DropUp_Logic()
    {
        var customSelectCss = TestPaths.ReadRepoFile("HV-Travel.Web", "wwwroot", "css", "admin-custom-select.css");
        var customSelectJs = TestPaths.ReadRepoFile("HV-Travel.Web", "wwwroot", "js", "admin-custom-select.js");

        Assert.Contains("z-index: 1200;", customSelectCss, StringComparison.Ordinal);
        Assert.Contains("z-index: 1210;", customSelectCss, StringComparison.Ordinal);
        Assert.Contains("isolation: isolate;", customSelectCss, StringComparison.Ordinal);
        Assert.Contains("container.classList.toggle('drop-up', shouldDropUp);", customSelectJs, StringComparison.Ordinal);
        Assert.Contains("window.innerHeight", customSelectJs, StringComparison.Ordinal);
    }

    private static string NormalizeWhitespace(string value)
    {
        return string.Join(' ', value.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));
    }

    private static Booking BuildBooking(string id, string bookingCode, string email, string phone)
    {
        return new Booking
        {
            Id = id,
            BookingCode = bookingCode,
            CustomerId = "customer-1",
            TourId = "tour-1",
            TourSnapshot = new TourSnapshot
            {
                Code = "TOUR01",
                Name = "Da Nang Escape",
                Duration = "3 ngày 2 đêm",
                StartDate = new DateTime(2025, 6, 10, 0, 0, 0, DateTimeKind.Utc)
            },
            ContactInfo = new ContactInfo
            {
                Name = "Alice",
                Email = email,
                Phone = phone
            },
            BookingDate = new DateTime(2025, 5, 1, 0, 0, 0, DateTimeKind.Utc),
            CreatedAt = new DateTime(2025, 5, 1, 0, 0, 0, DateTimeKind.Utc),
            TotalAmount = 5000000m,
            Status = "Confirmed",
            PaymentStatus = "Paid",
            PublicLookupEnabled = true,
            IsDeleted = false
        };
    }

    private sealed class InMemoryRepository<T> : IRepository<T>
        where T : class
    {
        private readonly Dictionary<string, T> _items;
        private readonly System.Reflection.PropertyInfo _idProperty;

        public InMemoryRepository(IEnumerable<T> items)
        {
            _idProperty = typeof(T).GetProperty("Id") ?? throw new InvalidOperationException($"{typeof(T).Name} must expose Id");
            _items = items.ToDictionary(GetId, StringComparer.Ordinal);
        }

        public Task<IEnumerable<T>> GetAllAsync() => Task.FromResult<IEnumerable<T>>(_items.Values.ToList());

        public Task<T> GetByIdAsync(string id) => Task.FromResult(_items.TryGetValue(id, out var item) ? item : null!);

        public Task<IReadOnlyList<T>> GetByIdsAsync(IEnumerable<string> ids)
        {
            var ordered = ids.Where(_items.ContainsKey).Select(id => _items[id]).ToList();
            return Task.FromResult<IReadOnlyList<T>>(ordered);
        }

        public Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
        {
            var compiled = predicate.Compile();
            return Task.FromResult<IEnumerable<T>>(_items.Values.Where(compiled).ToList());
        }

        public Task AddAsync(T entity)
        {
            _items[GetId(entity)] = entity;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(string id, T entity)
        {
            _items[id] = entity;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(string id)
        {
            _items.Remove(id);
            return Task.CompletedTask;
        }

        public Task<PaginatedResult<T>> GetPagedAsync(int pageIndex, int pageSize, Expression<Func<T, bool>> filter = null!)
        {
            var values = _items.Values.AsEnumerable();
            if (filter != null)
            {
                values = values.Where(filter.Compile());
            }

            var list = values.ToList();
            return Task.FromResult(new PaginatedResult<T>(list.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToList(), list.Count, pageIndex, pageSize));
        }

        private string GetId(T item)
        {
            return _idProperty.GetValue(item) as string ?? string.Empty;
        }
    }

    private sealed class StubMeilisearchDocumentIndexClient : IMeilisearchDocumentIndexClient
    {
        private readonly Dictionary<string, object> _responses = new(StringComparer.OrdinalIgnoreCase);

        public bool IsHealthy { get; set; }

        public Dictionary<string, MeilisearchDocumentSearchCommand?> LastCommands { get; } = new(StringComparer.OrdinalIgnoreCase);

        public void SetResponse<TDocument>(string indexName, MeilisearchDocumentSearchResponse<TDocument> response)
            where TDocument : class
        {
            _responses[indexName] = response;
        }

        public Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(IsHealthy);
        }

        public Task EnsureConfiguredAsync(MeilisearchIndexDefinition definition, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<MeilisearchDocumentSearchResponse<TDocument>> SearchAsync<TDocument>(MeilisearchIndexDefinition definition, MeilisearchDocumentSearchCommand command, CancellationToken cancellationToken = default) where TDocument : class
        {
            LastCommands[definition.Name] = command;
            if (_responses.TryGetValue(definition.Name, out var response))
            {
                return Task.FromResult((MeilisearchDocumentSearchResponse<TDocument>)response);
            }

            return Task.FromResult(new MeilisearchDocumentSearchResponse<TDocument>());
        }

        public Task UpsertDocumentsAsync<TDocument>(MeilisearchIndexDefinition definition, IReadOnlyCollection<TDocument> documents, CancellationToken cancellationToken = default) where TDocument : class
        {
            return Task.CompletedTask;
        }

        public Task DeleteDocumentsAsync(MeilisearchIndexDefinition definition, IReadOnlyCollection<string> ids, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task ReplaceAllDocumentsAsync<TDocument>(MeilisearchIndexDefinition definition, IReadOnlyCollection<TDocument> documents, CancellationToken cancellationToken = default) where TDocument : class
        {
            return Task.CompletedTask;
        }
    }
}

using System.Linq.Expressions;
using HVTravel.Domain.Entities;
using HVTravel.Domain.Interfaces;
using HVTravel.Domain.Models;
using HVTravel.Web.Services;

namespace HV_Travel.Web.Tests.TestSupport;

internal sealed class InMemoryTourRepository : ITourRepository
{
    private readonly List<Tour> _items;

    public InMemoryTourRepository(IEnumerable<Tour>? items = null)
    {
        _items = items?.ToList() ?? new List<Tour>();
    }

    public Task<IEnumerable<Tour>> GetAllAsync()
    {
        return Task.FromResult<IEnumerable<Tour>>(_items.ToList());
    }

    public Task<Tour> GetByIdAsync(string id)
    {
        var item = _items.FirstOrDefault(entity => string.Equals(entity.Id, id, StringComparison.Ordinal));
        return Task.FromResult(item)!;
    }

    public Task<Tour?> GetBySlugAsync(string slug)
    {
        return Task.FromResult(_items.FirstOrDefault(entity => string.Equals(entity.Slug, slug, StringComparison.OrdinalIgnoreCase)));
    }

    public Task<IEnumerable<Tour>> FindAsync(Expression<Func<Tour, bool>> predicate)
    {
        var compiled = predicate.Compile();
        return Task.FromResult<IEnumerable<Tour>>(_items.Where(compiled).ToList());
    }

    public Task AddAsync(Tour entity)
    {
        entity.Id ??= Guid.NewGuid().ToString("N");
        _items.Add(entity);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(string id, Tour entity)
    {
        var index = _items.FindIndex(item => string.Equals(item.Id, id, StringComparison.Ordinal));
        if (index >= 0)
        {
            _items[index] = entity;
        }

        return Task.CompletedTask;
    }

    public Task DeleteAsync(string id)
    {
        var index = _items.FindIndex(item => string.Equals(item.Id, id, StringComparison.Ordinal));
        if (index >= 0)
        {
            _items.RemoveAt(index);
        }

        return Task.CompletedTask;
    }

    public Task<PaginatedResult<Tour>> GetPagedAsync(int pageIndex, int pageSize, Expression<Func<Tour, bool>>? filter = null)
    {
        var query = _items.AsEnumerable();
        if (filter != null)
        {
            query = query.Where(filter.Compile());
        }

        var pageItems = query.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToList();
        return Task.FromResult(new PaginatedResult<Tour>(pageItems, query.Count(), pageIndex, pageSize));
    }

    public Task<TourSearchResult> SearchAsync(TourSearchRequest request)
    {
        var visibleTours = _items
            .Where(t => !request.PublicOnly || t.Status is "Active" or "ComingSoon" or "SoldOut")
            .ToList();

        IEnumerable<Tour> tours = visibleTours;

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var normalizedSearch = request.Search.Trim();
            tours = tours.Where(t =>
                (t.Name?.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) ?? false) ||
                RichTextContentFormatter.ToPlainText(t.ShortDescription).Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) ||
                RichTextContentFormatter.ToPlainText(t.Description).Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) ||
                (t.Destination?.City?.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (t.Destination?.Region?.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (t.Destination?.Country?.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        if (!string.IsNullOrWhiteSpace(request.Region))
        {
            tours = tours.Where(t => string.Equals(t.Destination?.Region, request.Region, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(request.Destination))
        {
            tours = tours.Where(t => string.Equals(t.Destination?.City, request.Destination, StringComparison.OrdinalIgnoreCase));
        }

        if (request.MinPrice.HasValue)
        {
            tours = tours.Where(t => GetStartingAdultPrice(t) >= request.MinPrice.Value);
        }

        if (request.MaxPrice.HasValue)
        {
            tours = tours.Where(t => GetStartingAdultPrice(t) <= request.MaxPrice.Value);
        }

        if (request.DepartureMonth.HasValue)
        {
            tours = tours.Where(t => GetEffectiveDepartures(t).Any(d => d.StartDate.Month == request.DepartureMonth.Value));
        }

        if (request.MaxDays.HasValue)
        {
            tours = tours.Where(t => (t.Duration?.Days ?? int.MaxValue) <= request.MaxDays.Value);
        }

        if (request.Travellers > 0)
        {
            tours = tours.Where(t => GetEffectiveDepartures(t).Any(d => d.RemainingCapacity >= request.Travellers));
        }

        if (!string.IsNullOrWhiteSpace(request.ConfirmationType))
        {
            tours = tours.Where(t =>
                GetEffectiveDepartures(t).Any(d => string.Equals(d.ConfirmationType, request.ConfirmationType, StringComparison.OrdinalIgnoreCase)) ||
                string.Equals(t.ConfirmationType, request.ConfirmationType, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(request.CancellationType))
        {
            tours = request.CancellationType.Trim().ToLowerInvariant() switch
            {
                "freecancellation" => tours.Where(t => t.CancellationPolicy?.IsFreeCancellation == true),
                _ => tours
            };
        }

        if (request.AvailableOnly)
        {
            tours = tours.Where(t => GetEffectiveDepartures(t).Any(d => d.RemainingCapacity >= Math.Max(1, request.Travellers)));
        }

        if (request.PromotionOnly)
        {
            tours = tours.Where(HasPromotion);
        }

        if (!string.IsNullOrWhiteSpace(request.Collection))
        {
            tours = request.Collection.Trim().ToLowerInvariant() switch
            {
                "domestic" => tours.Where(t => string.Equals(t.Destination?.Country, "Vietnam", StringComparison.OrdinalIgnoreCase) || string.Equals(t.Destination?.Country, "Việt Nam", StringComparison.OrdinalIgnoreCase)),
                "international" => tours.Where(t => !string.Equals(t.Destination?.Country, "Vietnam", StringComparison.OrdinalIgnoreCase) && !string.Equals(t.Destination?.Country, "Việt Nam", StringComparison.OrdinalIgnoreCase)),
                "premium" => tours.Where(t => GetStartingAdultPrice(t) >= 10000000m),
                "budget" => tours.Where(t => GetStartingAdultPrice(t) <= 3000000m),
                "family" => tours.Where(t => GetEffectiveDepartures(t).Any(d => d.RemainingCapacity >= Math.Max(4, request.Travellers))),
                "couple" => tours.Where(t => GetEffectiveDepartures(t).Any(d => d.RemainingCapacity >= 2) && (t.Duration?.Days ?? 0) <= 5),
                "seasonal" => tours.Where(t => GetEffectiveDepartures(t).Any(d => d.StartDate >= DateTime.UtcNow && d.StartDate <= DateTime.UtcNow.AddDays(90))),
                "deal" => tours.Where(HasPromotion),
                _ => tours
            };
        }

        tours = request.Sort switch
        {
            "price_asc" => tours.OrderBy(GetStartingAdultPrice),
            "price_desc" => tours.OrderByDescending(GetStartingAdultPrice),
            "rating" => tours.OrderByDescending(t => t.Rating),
            "newest" => tours.OrderByDescending(t => t.CreatedAt),
            "departure" => tours.OrderBy(t => GetEffectiveDepartures(t).Select(d => d.StartDate).DefaultIfEmpty(DateTime.MaxValue).Min()),
            "best_value" => tours.OrderByDescending(GetEffectiveDiscount).ThenByDescending(t => t.Rating).ThenBy(GetStartingAdultPrice),
            _ => tours.OrderByDescending(t => t.Rating)
        };

        var totalItems = tours.Count();
        var totalPages = (int)Math.Ceiling(totalItems / (double)Math.Max(1, request.PageSize));
        var currentPage = Math.Max(1, Math.Min(request.Page, totalPages == 0 ? 1 : totalPages));
        var items = tours
            .Skip((currentPage - 1) * Math.Max(1, request.PageSize))
            .Take(Math.Max(1, request.PageSize))
            .ToList();

        return Task.FromResult(new TourSearchResult
        {
            Items = items,
            TotalItems = totalItems,
            TotalPages = totalPages,
            CurrentPage = currentPage,
            Regions = BuildFacetOptions(visibleTours.Select(t => t.Destination?.Region), request.Region),
            Destinations = BuildFacetOptions(visibleTours.Select(t => t.Destination?.City), request.Destination),
            ConfirmationTypes = BuildFacetOptions(visibleTours.SelectMany(t => GetEffectiveDepartures(t).Select(d => d.ConfirmationType).Append(t.ConfirmationType)), request.ConfirmationType),
            CancellationTypes = BuildFacetOptions(visibleTours.Select(t => t.CancellationPolicy?.IsFreeCancellation == true ? "FreeCancellation" : "Strict"), request.CancellationType)
        });
    }

    public Task<bool> IncrementParticipantsAsync(string tourId, int count)
    {
        var tour = _items.FirstOrDefault(item => string.Equals(item.Id, tourId, StringComparison.Ordinal));
        if (tour == null || tour.CurrentParticipants + count > tour.MaxParticipants)
        {
            return Task.FromResult(false);
        }

        tour.CurrentParticipants += count;
        tour.UpdatedAt = DateTime.UtcNow;
        return Task.FromResult(true);
    }

    public Task<bool> ReserveDepartureAsync(string tourId, string departureId, int travellerCount)
    {
        var tour = _items.FirstOrDefault(item => string.Equals(item.Id, tourId, StringComparison.Ordinal));
        if (tour == null)
        {
            return Task.FromResult(false);
        }

        var departure = tour.Departures.FirstOrDefault(item => string.Equals(item.Id, departureId, StringComparison.Ordinal));
        if (departure == null)
        {
            return IncrementParticipantsAsync(tourId, travellerCount);
        }

        if (departure.BookedCount + travellerCount > departure.Capacity)
        {
            return Task.FromResult(false);
        }

        departure.BookedCount += travellerCount;
        tour.CurrentParticipants += travellerCount;
        tour.UpdatedAt = DateTime.UtcNow;
        return Task.FromResult(true);
    }

    private static IReadOnlyList<TourFacetOption> BuildFacetOptions(IEnumerable<string?> values, string? selected)
    {
        return values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .GroupBy(value => value!, StringComparer.OrdinalIgnoreCase)
            .Select(group => new TourFacetOption
            {
                Value = group.Key,
                Count = group.Count(),
                Selected = string.Equals(group.Key, selected, StringComparison.OrdinalIgnoreCase)
            })
            .OrderBy(option => option.Value)
            .ToList();
    }

    private static IEnumerable<TourDeparture> GetEffectiveDepartures(Tour tour)
    {
        if (tour.Departures is { Count: > 0 })
        {
            return tour.Departures;
        }

        return (tour.StartDates ?? new List<DateTime>())
            .Select((date, index) => new TourDeparture
            {
                Id = $"{tour.Id}-legacy-{index}",
                StartDate = date,
                AdultPrice = tour.Price?.Adult ?? 0m,
                ChildPrice = tour.Price?.Child ?? 0m,
                InfantPrice = tour.Price?.Infant ?? 0m,
                DiscountPercentage = (decimal)(tour.Price?.Discount ?? 0d),
                Capacity = tour.MaxParticipants,
                BookedCount = tour.CurrentParticipants,
                ConfirmationType = tour.ConfirmationType
            });
    }

    private static decimal GetStartingAdultPrice(Tour tour)
    {
        var departurePrices = GetEffectiveDepartures(tour).Select(item => item.AdultPrice).Where(value => value > 0m).ToList();
        return departurePrices.Count != 0 ? departurePrices.Min() : tour.Price?.Adult ?? 0m;
    }

    private static decimal GetEffectiveDiscount(Tour tour)
    {
        var departureDiscounts = GetEffectiveDepartures(tour).Select(item => item.DiscountPercentage).ToList();
        if (departureDiscounts.Count != 0)
        {
            return departureDiscounts.Max();
        }

        return (decimal)(tour.Price?.Discount ?? 0d);
    }

    private static bool HasPromotion(Tour tour)
    {
        return GetEffectiveDiscount(tour) > 0m || (tour.BadgeSet?.Any(badge => string.Equals(badge, "deal", StringComparison.OrdinalIgnoreCase)) ?? false);
    }
}

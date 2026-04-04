using HVTravel.Domain.Entities;
using HVTravel.Domain.Interfaces;
using HVTravel.Domain.Models;
using HVTravel.Infrastructure.Data;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;

namespace HVTravel.Infrastructure.Repositories
{
    public class TourRepository : Repository<Tour>, ITourRepository
    {
        public TourRepository(MongoContext context, IConfiguration configuration)
            : base(context, configuration)
        {
        }

        public async Task<TourSearchResult> SearchAsync(TourSearchRequest request)
        {
            request ??= new TourSearchRequest();

            var publicStatuses = new[] { "Active", "ComingSoon", "SoldOut" };
            var baseFilter = request.PublicOnly
                ? Builders<Tour>.Filter.In(t => t.Status, publicStatuses)
                : Builders<Tour>.Filter.Empty;

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var escaped = System.Text.RegularExpressions.Regex.Escape(request.Search.Trim());
                var regex = new MongoDB.Bson.BsonRegularExpression(escaped, "i");
                var searchFilter = Builders<Tour>.Filter.Or(
                    Builders<Tour>.Filter.Regex(t => t.Name, regex),
                    Builders<Tour>.Filter.Regex(t => t.Description, regex),
                    Builders<Tour>.Filter.Regex(t => t.ShortDescription, regex),
                    Builders<Tour>.Filter.Regex("destination.city", regex),
                    Builders<Tour>.Filter.Regex("destination.region", regex),
                    Builders<Tour>.Filter.Regex("destination.country", regex));
                baseFilter = Builders<Tour>.Filter.And(baseFilter, searchFilter);
            }

            if (!string.IsNullOrWhiteSpace(request.Region))
            {
                baseFilter = Builders<Tour>.Filter.And(baseFilter, Builders<Tour>.Filter.Eq("destination.region", request.Region.Trim()));
            }

            if (!string.IsNullOrWhiteSpace(request.Destination))
            {
                baseFilter = Builders<Tour>.Filter.And(baseFilter, Builders<Tour>.Filter.Eq("destination.city", request.Destination.Trim()));
            }

            if (request.MaxDays.HasValue)
            {
                baseFilter = Builders<Tour>.Filter.And(baseFilter, Builders<Tour>.Filter.Lte("duration.days", request.MaxDays.Value));
            }

            if (!string.IsNullOrWhiteSpace(request.ConfirmationType))
            {
                baseFilter = Builders<Tour>.Filter.And(baseFilter, Builders<Tour>.Filter.Or(
                    Builders<Tour>.Filter.Eq(t => t.ConfirmationType, request.ConfirmationType.Trim()),
                    Builders<Tour>.Filter.ElemMatch(t => t.Departures, departure => departure.ConfirmationType == request.ConfirmationType.Trim())));
            }

            if (!string.IsNullOrWhiteSpace(request.CancellationType) && string.Equals(request.CancellationType, "FreeCancellation", StringComparison.OrdinalIgnoreCase))
            {
                baseFilter = Builders<Tour>.Filter.And(baseFilter, Builders<Tour>.Filter.Eq("cancellationPolicy.isFreeCancellation", true));
            }

            if (!string.IsNullOrWhiteSpace(request.Collection))
            {
                baseFilter = Builders<Tour>.Filter.And(baseFilter, BuildCollectionFilter(request.Collection));
            }

            var visibleTours = await _collection.Find(baseFilter).ToListAsync();
            IEnumerable<Tour> tours = visibleTours;

            if (request.MinPrice.HasValue)
            {
                tours = tours.Where(tour => GetStartingAdultPrice(tour) >= request.MinPrice.Value);
            }

            if (request.MaxPrice.HasValue)
            {
                tours = tours.Where(tour => GetStartingAdultPrice(tour) <= request.MaxPrice.Value);
            }

            if (request.DepartureMonth.HasValue)
            {
                tours = tours.Where(tour => tour.EffectiveDepartures.Any(departure => departure.StartDate.Month == request.DepartureMonth.Value));
            }

            if (request.Travellers > 0)
            {
                tours = tours.Where(tour => tour.EffectiveDepartures.Any(departure => departure.RemainingCapacity >= request.Travellers));
            }

            if (request.AvailableOnly)
            {
                var requiredTravellers = Math.Max(1, request.Travellers);
                tours = tours.Where(tour => tour.EffectiveDepartures.Any(departure => departure.RemainingCapacity >= requiredTravellers));
            }

            if (request.PromotionOnly)
            {
                tours = tours.Where(HasPromotion);
            }

            tours = request.Sort switch
            {
                "price_asc" => tours.OrderBy(GetStartingAdultPrice),
                "price_desc" => tours.OrderByDescending(GetStartingAdultPrice),
                "rating" => tours.OrderByDescending(tour => tour.Rating),
                "newest" => tours.OrderByDescending(tour => tour.CreatedAt),
                "departure" => tours.OrderBy(tour => tour.EffectiveDepartures.Select(item => item.StartDate).DefaultIfEmpty(DateTime.MaxValue).Min()),
                "best_value" => tours.OrderByDescending(GetEffectiveDiscount).ThenByDescending(tour => tour.Rating).ThenBy(GetStartingAdultPrice),
                _ => tours.OrderByDescending(tour => tour.Rating)
            };

            var pageSize = Math.Max(1, request.PageSize);
            var totalItems = tours.Count();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            var currentPage = Math.Max(1, Math.Min(request.Page, totalPages == 0 ? 1 : totalPages));
            var items = tours
                .Skip((currentPage - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new TourSearchResult
            {
                Items = items,
                TotalItems = totalItems,
                TotalPages = totalPages,
                CurrentPage = currentPage,
                Regions = BuildFacetOptions(visibleTours.Select(tour => tour.Destination?.Region), request.Region),
                Destinations = BuildFacetOptions(visibleTours.Select(tour => tour.Destination?.City), request.Destination),
                ConfirmationTypes = BuildFacetOptions(
                    visibleTours.SelectMany(tour => tour.EffectiveDepartures.Select(departure => departure.ConfirmationType).Append(tour.ConfirmationType)),
                    request.ConfirmationType),
                CancellationTypes = BuildFacetOptions(
                    visibleTours.Select(tour => tour.CancellationPolicy?.IsFreeCancellation == true ? "FreeCancellation" : "Strict"),
                    request.CancellationType)
            };
        }

        public Task<Tour?> GetBySlugAsync(string slug)
        {
            return _collection.Find(tour => tour.Slug == slug).FirstOrDefaultAsync()!;
        }

        public async Task<bool> IncrementParticipantsAsync(string tourId, int count)
        {
            var filter = Builders<Tour>.Filter.And(
                Builders<Tour>.Filter.Eq(t => t.Id, tourId),
                Builders<Tour>.Filter.Where(t => t.CurrentParticipants + count <= t.MaxParticipants));

            var update = Builders<Tour>.Update
                .Inc(t => t.CurrentParticipants, count)
                .Set(t => t.UpdatedAt, DateTime.UtcNow)
                .Inc("version", 1);

            var result = await _collection.UpdateOneAsync(filter, update);
            return result.ModifiedCount > 0;
        }

        public async Task<bool> ReserveDepartureAsync(string tourId, string departureId, int travellerCount)
        {
            var tour = await _collection.Find(item => item.Id == tourId).FirstOrDefaultAsync();
            if (tour == null)
            {
                return false;
            }

            var departure = tour.Departures.FirstOrDefault(item => string.Equals(item.Id, departureId, StringComparison.Ordinal));
            if (departure == null)
            {
                return await IncrementParticipantsAsync(tourId, travellerCount);
            }

            if (departure.BookedCount + travellerCount > departure.Capacity)
            {
                return false;
            }

            departure.BookedCount += travellerCount;
            tour.CurrentParticipants += travellerCount;
            tour.UpdatedAt = DateTime.UtcNow;

            var filter = Builders<Tour>.Filter.And(
                Builders<Tour>.Filter.Eq(item => item.Id, tourId),
                Builders<Tour>.Filter.Eq(item => item.Version, tour.Version));

            tour.Version += 1;
            var result = await _collection.ReplaceOneAsync(filter, tour);
            return result.ModifiedCount > 0;
        }

        private static FilterDefinition<Tour> BuildCollectionFilter(string collection)
        {
            return collection.Trim().ToLowerInvariant() switch
            {
                "domestic" => Builders<Tour>.Filter.Or(
                    Builders<Tour>.Filter.Eq("destination.country", "Vietnam"),
                    Builders<Tour>.Filter.Eq("destination.country", "Vi?t Nam")),
                "international" => Builders<Tour>.Filter.Nin("destination.country", new[] { "Vietnam", "Vi?t Nam" }),
                "premium" => Builders<Tour>.Filter.Gte("price.adult", 10000000m),
                "budget" => Builders<Tour>.Filter.Lte("price.adult", 3000000m),
                "deal" => Builders<Tour>.Filter.Or(
                    Builders<Tour>.Filter.Gt("price.discount", 0d),
                    Builders<Tour>.Filter.AnyEq(t => t.BadgeSet, "deal")),
                _ => Builders<Tour>.Filter.Empty
            };
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

        private static decimal GetStartingAdultPrice(Tour tour)
        {
            var departurePrices = tour.EffectiveDepartures
                .Select(item => item.AdultPrice)
                .Where(value => value > 0m)
                .ToList();

            return departurePrices.Count != 0 ? departurePrices.Min() : tour.Price?.Adult ?? 0m;
        }

        private static decimal GetEffectiveDiscount(Tour tour)
        {
            var departureDiscounts = tour.EffectiveDepartures.Select(item => item.DiscountPercentage).ToList();
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
}

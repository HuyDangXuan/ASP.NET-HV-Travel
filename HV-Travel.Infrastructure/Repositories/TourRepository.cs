using System.Threading.Tasks;
using HVTravel.Domain.Entities;
using HVTravel.Domain.Interfaces;
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

        public async Task<bool> IncrementParticipantsAsync(string tourId, int count)
        {
            var filter = Builders<Tour>.Filter.And(
                Builders<Tour>.Filter.Eq(t => t.Id, tourId),
                Builders<Tour>.Filter.Where(t => t.CurrentParticipants + count <= t.MaxParticipants)
            );

            var update = Builders<Tour>.Update
                .Inc(t => t.CurrentParticipants, count)
                .Set(t => t.UpdatedAt, DateTime.UtcNow)
                .Inc("version", 1);

            var result = await _collection.UpdateOneAsync(filter, update);

            return result.ModifiedCount > 0;
        }
    }
}

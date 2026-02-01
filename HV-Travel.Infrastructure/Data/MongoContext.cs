using Microsoft.Extensions.Configuration;
using MongoDB.Driver;

namespace HVTravel.Infrastructure.Data
{
    public class MongoContext
    {
        private readonly IMongoDatabase _database;

        public MongoContext(IConfiguration configuration)
        {
            var client = new MongoClient(configuration.GetValue<string>("HVTravelDatabase:ConnectionString"));
            _database = client.GetDatabase(configuration.GetValue<string>("HVTravelDatabase:DatabaseName"));
        }

        public IMongoCollection<T> GetCollection<T>(string name)
        {
            return _database.GetCollection<T>(name);
        }
    }
}

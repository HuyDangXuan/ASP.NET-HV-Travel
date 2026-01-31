using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Linq.Expressions;
using VietVoyage.Domain.Interfaces;
using VietVoyage.Infrastructure.Data;

namespace VietVoyage.Infrastructure.Repositories
{
    public class Repository<T> : IRepository<T> where T : class
    {
        protected readonly IMongoCollection<T> _collection;

        public Repository(MongoContext context, IConfiguration configuration)
        {
            var collectionName = GetCollectionName(typeof(T), configuration);
            _collection = context.GetCollection<T>(collectionName);
        }

        private string GetCollectionName(Type type, IConfiguration configuration)
        {
            // Simple mapping based on type name + "s", or you can read from attributes/config
            // For now, let's look for a basic match in appsettings
            var name = type.Name + "s";
            // Check if there's a specific override in config
            var configName = configuration.GetValue<string>($"HVTravelDatabase:{type.Name}CollectionName");
            return !string.IsNullOrEmpty(configName) ? configName : name;
        }

        public async Task<IEnumerable<T>> GetAllAsync()
        {
            return await _collection.Find(_ => true).ToListAsync();
        }

        public async Task<T> GetByIdAsync(string id)
        {
            var filter = Builders<T>.Filter.Eq("Id", id);
            return await _collection.Find(filter).FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
        {
            return await _collection.Find(predicate).ToListAsync();
        }

        public async Task AddAsync(T entity)
        {
            await _collection.InsertOneAsync(entity);
        }

        public async Task UpdateAsync(string id, T entity)
        {
            var filter = Builders<T>.Filter.Eq("Id", id);
            await _collection.ReplaceOneAsync(filter, entity);
        }

        public async Task DeleteAsync(string id)
        {
            var filter = Builders<T>.Filter.Eq("Id", id);
            await _collection.DeleteOneAsync(filter);
        }
    }
}

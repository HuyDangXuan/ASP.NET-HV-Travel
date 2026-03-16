using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using HVTravel.Domain.Interfaces;
using HVTravel.Domain.Models;
using HVTravel.Infrastructure.Data;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Reflection;

namespace HVTravel.Infrastructure.Repositories
{
    public class Repository<T> : IRepository<T> where T : class
    {
        protected readonly IMongoCollection<T> _collection;
        private static readonly PropertyInfo? IdProperty = typeof(T).GetProperty("Id");

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
            var filter = BuildIdFilter(id);
            return await _collection.Find(filter).FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
        {
            return await _collection.Find(predicate).ToListAsync();
        }

        public async Task AddAsync(T entity)
        {
            if (IdProperty?.PropertyType == typeof(string))
            {
                var currentId = IdProperty.GetValue(entity) as string;
                if (string.IsNullOrWhiteSpace(currentId))
                {
                    IdProperty.SetValue(entity, ObjectId.GenerateNewId().ToString());
                }
            }

            await _collection.InsertOneAsync(entity);
        }

        public async Task UpdateAsync(string id, T entity)
        {
            var filter = BuildIdFilter(id);
            
            // Optimistic Concurrency Control (OCC)
            // If the entity has a Version property, we use it for the filter and increment it
            var versionProp = typeof(T).GetProperty("Version");
            if (versionProp != null && versionProp.PropertyType == typeof(uint))
            {
                var versionValue = versionProp.GetValue(entity);
                if (versionValue is not uint currentVersion)
                {
                    throw new InvalidOperationException($"Type {typeof(T).Name} has an invalid Version value.");
                }

                var versionFilter = BuildUIntPropertyFilter(versionProp, currentVersion);
                filter = Builders<T>.Filter.And(filter, versionFilter);
                
                // Increment version for the next save
                versionProp.SetValue(entity, currentVersion + 1);
            }

            var result = await _collection.ReplaceOneAsync(filter, entity);
            
            if (result.MatchedCount == 0 && versionProp != null)
            {
                throw new DbUpdateConcurrencyException("Dữ liệu đã bị thay đổi bởi một người dùng khác. Vui lòng tải lại trang.");
            }
        }

        public async Task DeleteAsync(string id)
        {
            var filter = BuildIdFilter(id);
            await _collection.DeleteOneAsync(filter);
        }

        public async Task<PaginatedResult<T>> GetPagedAsync(int pageIndex, int pageSize, Expression<Func<T, bool>>? filter = null)
        {
            var filterDef = filter != null
                ? Builders<T>.Filter.Where(filter)
                : Builders<T>.Filter.Empty;

            var count = (int)await _collection.CountDocumentsAsync(filterDef);
            var items = await _collection.Find(filterDef)
                .Skip((pageIndex - 1) * pageSize)
                .Limit(pageSize)
                .ToListAsync();

            return new PaginatedResult<T>(items, count, pageIndex, pageSize);
        }

        private static FilterDefinition<T> BuildIdFilter(string id)
        {
            if (IdProperty == null)
            {
                throw new InvalidOperationException($"Type {typeof(T).Name} does not contain an Id property.");
            }

            return Builders<T>.Filter.Eq(IdProperty.Name, id);
        }

        private static FilterDefinition<T> BuildUIntPropertyFilter(PropertyInfo propertyInfo, uint value)
        {
            return Builders<T>.Filter.Eq(propertyInfo.Name, value);
        }

    }
}

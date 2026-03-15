using HVTravel.Domain.Entities;
using MongoDB.Bson.Serialization;

namespace HVTravel.Infrastructure.Data.Serialization
{
    public static class MongoSerializationBootstrapper
    {
        private static readonly object SyncRoot = new();
        private static bool _isRegistered;

        public static void Register()
        {
            if (_isRegistered)
            {
                return;
            }

            lock (SyncRoot)
            {
                if (_isRegistered)
                {
                    return;
                }

                RegisterSerializer<Customer>();
                RegisterSerializer<User>();
                RegisterSerializer<Booking>();
                RegisterSerializer<Tour>();
                RegisterSerializer<Payment>();
                RegisterSerializer<Promotion>();
                RegisterSerializer<Review>();
                RegisterSerializer<Notification>();

                _isRegistered = true;
            }
        }

        private static void RegisterSerializer<T>()
        {
            BsonSerializer.RegisterSerializer(typeof(T), new LegacySnakeCaseAliasSerializer<T>());
        }
    }
}

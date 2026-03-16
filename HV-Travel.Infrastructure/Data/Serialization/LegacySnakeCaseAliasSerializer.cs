using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace HVTravel.Infrastructure.Data.Serialization
{
    internal sealed class LegacySnakeCaseAliasSerializer<T> : SerializerBase<T>, IBsonDocumentSerializer
    {
        private readonly BsonClassMapSerializer<T> _innerSerializer;

        public LegacySnakeCaseAliasSerializer()
        {
            _innerSerializer = new BsonClassMapSerializer<T>(BsonClassMap.LookupClassMap(typeof(T)));
        }

        public override T Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var document = BsonDocumentSerializer.Instance.Deserialize(context);
            var normalized = NormalizeDocument(document);

            using var reader = new BsonDocumentReader(normalized);
            var innerContext = BsonDeserializationContext.CreateRoot(reader);
            return _innerSerializer.Deserialize(innerContext, args);
        }

        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, T value)
        {
            _innerSerializer.Serialize(context, args, value);
        }

        public bool TryGetMemberSerializationInfo(string memberName, out BsonSerializationInfo serializationInfo)
        {
            return _innerSerializer.TryGetMemberSerializationInfo(memberName, out serializationInfo);
        }

        private static BsonDocument NormalizeDocument(BsonDocument document)
        {
            var normalized = new BsonDocument();

            foreach (var element in document)
            {
                var normalizedName = NormalizeElementName(element.Name);
                if (normalized.Contains(normalizedName))
                {
                    continue;
                }

                normalized[normalizedName] = NormalizeValue(element.Value);
            }

            return normalized;
        }

        private static BsonValue NormalizeValue(BsonValue value)
        {
            if (value == null || value.IsBsonNull)
            {
                return value!;
            }

            if (value is BsonDocument document)
            {
                return NormalizeDocument(document);
            }

            if (value is BsonArray array)
            {
                return new BsonArray(array.Select(NormalizeValue));
            }

            return value;
        }

        private static string NormalizeElementName(string elementName)
        {
            if (string.IsNullOrWhiteSpace(elementName) || !elementName.Contains('_') || elementName.StartsWith("_") || elementName.StartsWith("$"))
            {
                return elementName;
            }

            var segments = elementName.Split('_', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length == 0)
            {
                return elementName;
            }

            return segments[0] + string.Concat(segments.Skip(1).Select(Capitalize));
        }

        private static string Capitalize(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            return char.ToUpperInvariant(value[0]) + value[1..];
        }
    }
}



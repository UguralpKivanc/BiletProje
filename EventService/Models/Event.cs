using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace eventservice.Models
{
    [BsonIgnoreExtraElements]
    public class Event
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("Name")] // BURASI ÇOK KRİTİK: Compass'taki gibi büyük 'N'
        public string Name { get; set; } = string.Empty;

        [BsonElement("Location")] // Büyük 'L'
        public string Location { get; set; } = string.Empty;

        [BsonElement("Date")] // Büyük 'D'
        public DateTime Date { get; set; }

        [BsonElement("Price")] // Büyük 'P'
        public decimal Price { get; set; }
    }
}
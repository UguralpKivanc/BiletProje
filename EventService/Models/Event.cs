using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace eventservice.Models
{
    public class Event
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public decimal Price { get; set; }
    }
}
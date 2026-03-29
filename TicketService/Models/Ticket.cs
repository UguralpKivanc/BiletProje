using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace TicketService.Models
{
    [BsonIgnoreExtraElements]
    public class Ticket
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        public string EventName { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string Seat { get; set; } = string.Empty;
        public string Status { get; set; } = "Active";
        public decimal Price { get; set; }
        public DateTime PurchaseDate { get; set; } = DateTime.UtcNow;
    }
}

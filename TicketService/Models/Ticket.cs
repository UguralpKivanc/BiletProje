using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace TicketService.Models
{
    public class Ticket
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        // Hangi etkinliğe ait olduğunu anlamak için EventService'deki ID'yi buraya kaydedeceğiz
        public string EventId { get; set; } = string.Empty;

        public string CustomerName { get; set; } = string.Empty;
        public DateTime PurchaseDate { get; set; } = DateTime.Now;
    }
}
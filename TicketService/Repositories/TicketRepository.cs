using TicketService.Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace TicketService.Repositories
{
    public class TicketRepository : ITicketRepository
    {
        private readonly IMongoCollection<Ticket> _collection;

        public TicketRepository(IMongoClient mongoClient)
        {
            var database = mongoClient.GetDatabase("TicketServiceDb");
            _collection = database.GetCollection<Ticket>("Tickets");
        }

        public async Task<List<Ticket>> GetAllAsync()
            => await _collection.Find(new BsonDocument()).ToListAsync();

        public async Task<(List<Ticket> Items, long TotalCount)> GetPagedAsync(int page, int pageSize, string? ownerUsernameFilter = null)
        {
            FilterDefinition<Ticket> filter = ownerUsernameFilter is null
                ? Builders<Ticket>.Filter.Empty
                : Builders<Ticket>.Filter.Eq(t => t.OwnerUsername, ownerUsernameFilter);

            var total = await _collection.CountDocumentsAsync(filter);
            var items = await _collection.Find(filter)
                .SortByDescending(t => t.PurchaseDate)
                .Skip((page - 1) * pageSize)
                .Limit(pageSize)
                .ToListAsync();
            return (items, total);
        }

        public async Task<Ticket?> GetByIdAsync(string id)
        {
            var filter = Builders<Ticket>.Filter.Eq(t => t.Id, id);
            return await _collection.Find(filter).FirstOrDefaultAsync();
        }

        public async Task<Ticket> CreateAsync(Ticket ticket)
        {
            await _collection.InsertOneAsync(ticket);
            return ticket;
        }

        public async Task UpdateAsync(string id, Ticket ticket)
        {
            var filter = Builders<Ticket>.Filter.Eq(t => t.Id, id);
            var update = Builders<Ticket>.Update
                .Set(t => t.EventName, ticket.EventName)
                .Set(t => t.CustomerName, ticket.CustomerName)
                .Set(t => t.Seat, ticket.Seat)
                .Set(t => t.Status, ticket.Status)
                .Set(t => t.Price, ticket.Price);
            await _collection.UpdateOneAsync(filter, update);
        }

        public async Task DeleteAsync(string id)
        {
            var filter = Builders<Ticket>.Filter.Eq(t => t.Id, id);
            await _collection.DeleteOneAsync(filter);
        }
    }
}

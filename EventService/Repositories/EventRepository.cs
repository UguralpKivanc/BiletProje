using eventservice.Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace eventservice.Repositories
{
    public class EventRepository : IEventRepository
    {
        private readonly IMongoCollection<Event> _collection;

        public EventRepository(IMongoClient mongoClient)
        {
            var database = mongoClient.GetDatabase("BiletSistemiDb");
            _collection = database.GetCollection<Event>("Events");
        }

        public async Task<List<Event>> GetAllAsync()
            => await _collection.Find(new BsonDocument()).ToListAsync();

        public async Task<Event?> GetByIdAsync(string id)
        {
            var filter = Builders<Event>.Filter.Eq(e => e.Id, id);
            return await _collection.Find(filter).FirstOrDefaultAsync();
        }

        public async Task<Event> CreateAsync(Event evt)
        {
            await _collection.InsertOneAsync(evt);
            return evt;
        }

        public async Task UpdateAsync(string id, Event evt)
        {
            var filter = Builders<Event>.Filter.Eq(e => e.Id, id);
            var update = Builders<Event>.Update
                .Set(e => e.Name, evt.Name)
                .Set(e => e.Location, evt.Location)
                .Set(e => e.Date, evt.Date)
                .Set(e => e.Price, evt.Price);
            await _collection.UpdateOneAsync(filter, update);
        }

        public async Task DeleteAsync(string id)
        {
            var filter = Builders<Event>.Filter.Eq(e => e.Id, id);
            await _collection.DeleteOneAsync(filter);
        }
    }
}

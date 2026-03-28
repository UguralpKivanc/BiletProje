using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;

namespace eventservice.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EventsController : ControllerBase
    {
        private readonly IMongoCollection<BsonDocument> _eventsCollection;

        public EventsController(IConfiguration configuration)
        {
            // Ortam değişkenini kontrol et, yoksa Docker içindeki servis adını kullan
            var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__MongoDb")
                                   ?? "mongodb://mongodb:27017";

            var client = new MongoClient(connectionString);
            var database = client.GetDatabase("BiletSistemiDb");

            // BsonDocument kullanarak her türlü veri yapısına uyum sağlıyoruz
            _eventsCollection = database.GetCollection<BsonDocument>("Events");
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            // 1. Veritabanındaki tüm belgeleri çek
            var documents = await _eventsCollection.Find(new BsonDocument()).ToListAsync();

            // 2. BsonDocument'leri .NET'in anlayacağı bir 'Dictionary' yapısına çeviriyoruz.
            // Bu sayede o gıcık ters bölü (\") işaretleri kaybolur, tertemiz JSON gelir.
            var result = documents.Select(doc => BsonTypeMapper.MapToDotNetValue(doc));

            return Ok(result);
        }
    }
}
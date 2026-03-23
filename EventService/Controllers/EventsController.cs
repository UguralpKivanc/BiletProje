using Microsoft.AspNetCore.Mvc;
using eventservice.Models;
using MongoDB.Driver; // Bunu eklemeyi unutma (NuGet paketini kurduysan gelir)

namespace eventservice.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EventsController : ControllerBase
    {
        // Artık List yerine MongoDB Koleksiyonu kullanıyoruz
        private readonly IMongoCollection<Event> _eventsCollection;

        public EventsController()
        {
            // 1. MongoDB'ye bağlan (Varsayılan yerel adres)
            var client = new MongoClient("mongodb://localhost:27017");

            // 2. Veritabanını seç (Yoksa otomatik oluşturur)
            var database = client.GetDatabase("BiletSistemiDb");

            // 3. Tabloyu (Collection) seç
            _eventsCollection = database.GetCollection<Event>("Events");
        }

        // TÜM ETKİNLİKLERİ GETİR
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Event>>> Get()
        {
            // Veritabanındaki tüm belgeleri listele
            var events = await _eventsCollection.Find(_ => true).ToListAsync();
            return Ok(events);
        }

        // YENİ ETKİNLİK EKLE (Test etmek için lazım olacak)
        [HttpPost]
        public async Task<IActionResult> Create(Event newEvent)
        {
            await _eventsCollection.InsertOneAsync(newEvent);
            return Ok(newEvent);
        }
    }
}
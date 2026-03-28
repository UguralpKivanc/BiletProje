using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Text;

namespace TicketService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TicketsController : ControllerBase
    {
        private readonly IMongoCollection<BsonDocument> _ticketsCollection;

        public TicketsController(IConfiguration configuration)
        {
            var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__MongoDb")
                                   ?? "mongodb://mongodb:27017";

            var client = new MongoClient(connectionString);
            var database = client.GetDatabase("BiletSistemiDb");
            _ticketsCollection = database.GetCollection<BsonDocument>("Tickets");
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var documents = await _ticketsCollection.Find(new BsonDocument()).ToListAsync();
            var result = documents.Select(doc => BsonTypeMapper.MapToDotNetValue(doc));
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Post()
        {
            // Ağam, burada veriyi sınıfa uydurmaya çalışmıyoruz, 
            // Direkt gelen paketi (Body) ham metin olarak okuyoruz.
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var body = await reader.ReadToEndAsync();

            if (string.IsNullOrEmpty(body))
            {
                return BadRequest("Ağam veri boş geldi, Hadise biletini gönderemedik!");
            }

            try
            {
                // Okuduğumuz ham metni (JSON) BsonDocument'e elinle çeviriyoruz
                var document = BsonDocument.Parse(body);

                await _ticketsCollection.InsertOneAsync(document);

                // Başarıyla eklendiğini teyit etmek için eklenen veriyi geri döndür
                return Ok(BsonTypeMapper.MapToDotNetValue(document));
            }
            catch (Exception ex)
            {
                return BadRequest("Ağam gönderdiğin JSON bozuk: " + ex.Message);
            }
        }
    }
}
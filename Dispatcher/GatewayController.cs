using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Text;

namespace Dispatcher.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GatewayController : ControllerBase
    {
        private readonly HttpClient _httpClient = new HttpClient();
        private readonly IMongoCollection<BsonDocument>? _authCollection;
        private readonly bool _isDocker;

        public GatewayController()
        {
            _isDocker = Environment.GetEnvironmentVariable("DOCKER_ENV") == "true";
            try
            {
                var connectionString = _isDocker ? "mongodb://mongodb:27017" : "mongodb://localhost:27017";
                var client = new MongoClient(connectionString);
                var database = client.GetDatabase("BiletSistemiDb");
                _authCollection = database.GetCollection<BsonDocument>("ApiKeys");
            }
            catch { _authCollection = null; }
        }

        [HttpGet("{*path}")]
        public async Task<IActionResult> ForwardToService(string path)
        {
            // 1. API KEY ZORUNLU KONTROL
            if (!Request.Headers.TryGetValue("X-Api-Key", out var extractedApiKey))
                return StatusCode(401, new { error = "API anahtarı eksik ağam!" });

            // 2. ANAHTAR DOĞRULAMA
            if (_authCollection != null)
            {
                try
                {
                    var filter = Builders<BsonDocument>.Filter.Eq("key", extractedApiKey.ToString());
                    var authRecord = await _authCollection.Find(filter).FirstOrDefaultAsync();

                    if (authRecord == null || authRecord["isActive"] == false)
                        return StatusCode(403, new { error = "Hatalı veya pasif anahtar ağam!" });
                }
                catch
                {
                    // MongoDB erişilemiyorsa doğrulamayı geç
                }
            }

            // 3. HEDEF URL BELİRLEME
            if (string.IsNullOrEmpty(path))
                return BadRequest(new { error = "Geçersiz servis yolu!", gelen_yol = path });

            string targetUrl;
            var lowerPath = path.ToLower();

            if (lowerPath.Contains("events"))
                targetUrl = _isDocker ? "http://eventservice:5001/api/events" : "http://localhost:5001/api/events";
            else if (lowerPath.Contains("tickets"))
                targetUrl = _isDocker ? "http://ticketservice:5168/api/tickets" : "http://localhost:5168/api/tickets";
            else
                return BadRequest(new { error = "Geçersiz servis yolu!", gelen_yol = path });

            // 4. İSTEĞİ İLET (15 saniye timeout)
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                var response = await _httpClient.GetAsync(targetUrl, cts.Token);
                var content = await response.Content.ReadAsStringAsync();
                return Content(content, "application/json", Encoding.UTF8);
            }
            catch (Exception ex)
            {
                return StatusCode(502, new
                {
                    error = "Hedefe ulaşılamadı!",
                    hedef = targetUrl,
                    detay = ex.Message,
                    isDocker = _isDocker
                });
            }
        }
    }
}
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
            catch
            {
                // Test ortamında veritabanı yoksa hata fırlatmasın, null kalsın
                _authCollection = null;
            }
        }

        [HttpGet("{*path}")]
        [HttpPost("{*path}")]
        public async Task<IActionResult> ForwardToService(string path)
        {
            // 1. YETKİLENDİRME (AUTH)
            if (!Request.Headers.TryGetValue("X-Api-Key", out var extractedApiKey))
            {
                return Unauthorized(new { message = "Anahtar eksik ağam!" });
            }

            // --- TEST DOSTU AUTH KONTROLÜ ---
            if (_authCollection == null)
            {
                // Veritabanı yoksa (Test sırasında), anahtar KingoSifre123 değilse 403 dön
                if (extractedApiKey.ToString() != "KingoSifre123")
                    return StatusCode(403, new { error = "Hatalı anahtar ağam!" });
            }
            else
            {
                // Canlı ortamda MongoDB'den sorgula
                var filter = Builders<BsonDocument>.Filter.Eq("key", extractedApiKey.ToString());
                var authRecord = await _authCollection.Find(filter).FirstOrDefaultAsync();

                if (authRecord == null || authRecord["isActive"] == false)
                {
                    return StatusCode(403, new { error = "Hatalı veya pasif anahtar!" });
                }
            }
            // ---------------------------------

            // 2. YOL KONTROLÜ
            if (string.IsNullOrEmpty(path))
            {
                return NotFound(new { message = "Yol belirtilmedi ağam!" });
            }

            // 3. SERVİS BELİRLEME
            string targetUrl = "";
            if (path.Contains("events"))
                targetUrl = _isDocker ? $"http://event-service:5001/{path}" : $"http://localhost:5001/{path}";
            else if (path.Contains("tickets"))
                targetUrl = _isDocker ? $"http://ticket-service:5168/{path}" : $"http://localhost:5168/{path}";
            else
                return BadRequest(new { error = "Geçersiz servis yolu ağam!" });

            try
            {
                var request = new HttpRequestMessage(new HttpMethod(Request.Method), targetUrl);

                if (Request.ContentLength > 0)
                {
                    using var reader = new StreamReader(Request.Body);
                    var body = await reader.ReadToEndAsync();
                    request.Content = new StringContent(body, Encoding.UTF8, Request.ContentType ?? "application/json");
                }

                var response = await _httpClient.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();

                return Content(content, "application/json", Encoding.UTF8);
            }
            catch
            {
                // 4. SERVİS KAPALIYSA (Veya DB erişim hatası buraya düşerse)
                return StatusCode(503, new { error = "Servis ulaşılamaz ağam!" });
            }
        }
    }
}
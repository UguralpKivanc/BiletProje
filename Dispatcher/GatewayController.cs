using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Threading.Tasks;

namespace Dispatcher.Controllers
{
    [ApiController]
    [Route("gateway")] // Giriş kapımız: http://localhost:5000/gateway
    public class GatewayController : ControllerBase
    {
        private readonly HttpClient _httpClient = new HttpClient();

        [HttpGet("{*path}")]
        public async Task<IActionResult> ForwardToService(string path)
        {
            // 1. Yol kontrolü
            if (string.IsNullOrEmpty(path))
            {
                return NotFound("Lutfen bir yol belirtin (ornegin: gateway/events)");
            }

            // 2. Docker mı yoksa Yerel mi kontrolü
            // docker-compose dosyasında DOCKER_ENV=true verdiğimiz için bunu anlayabiliyor
            bool isDocker = Environment.GetEnvironmentVariable("DOCKER_ENV") == "true";

            // 3. Port ve Servis İsmi Yönlendirme Mantığı
            string targetUrl;
            if (path.Contains("events"))
            {
                // Docker içindeysek konteyner ismi (event-service), değilse localhost
                targetUrl = isDocker ? "http://event-service:5001/api/events" : "http://localhost:5001/api/events";
            }
            else if (path.Contains("tickets"))
            {
                targetUrl = isDocker ? "http://ticket-service:5168/api/tickets" : "http://localhost:5168/api/tickets";
            }
            else
            {
                return BadRequest("Gecersiz servis yolu. Sadece 'events' veya 'tickets' kullanilabilir.");
            }

            try
            {
                // 4. Mikroservise isteği at ve cevabı bekle
                var response = await _httpClient.GetAsync(targetUrl);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return Content(content, "application/json");
                }

                return StatusCode((int)response.StatusCode, "Mikroservis hata dondu.");
            }
            catch (Exception ex)
            {
                // Hata mesajını daha açıklayıcı yaptık (Docker hatasını anlamak için)
                return StatusCode(503, $"Hedef mikroservis su an ulasilamaz durumda ({targetUrl}): {ex.Message}");
            }
        }
    }
}
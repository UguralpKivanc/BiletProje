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
            // 1. Yol kontrolü (boşsa hata ver)
            if (string.IsNullOrEmpty(path))
            {
                return NotFound("Lutfen bir yol belirtin (ornegin: gateway/events)");
            }

            // 2. Port Yönlendirme Mantığı
            // Eğer adres 'events' içeriyorsa 5001'e, içermiyorsa 5168'e (TicketService) yönlendir.
            string targetUrl;
            if (path.Contains("events"))
            {
                targetUrl = "http://localhost:5001/api/events";
            }
            else if (path.Contains("tickets"))
            {
                targetUrl = "http://localhost:5168/api/tickets";
            }
            else
            {
                return BadRequest("Gecersiz servis yolu. Sadece 'events' veya 'tickets' kullanilabilir.");
            }

            try
            {
                // 3. Mikroservise isteği at ve cevabı bekle
                var response = await _httpClient.GetAsync(targetUrl);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    // Gelen veriyi JSON formatında kullanıcıya yansıt
                    return Content(content, "application/json");
                }

                return StatusCode((int)response.StatusCode, "Mikroservis hata dondu.");
            }
            catch (Exception ex)
            {
                // Eğer hedef servis (5001 veya 5168) kapalıysa buraya düşer
                return StatusCode(503, $"Hedef mikroservis su an ulasilamaz durumda: {ex.Message}");
            }
        }
    }
}
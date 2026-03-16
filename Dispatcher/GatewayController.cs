using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Threading.Tasks;

namespace Dispatcher.Controllers
{
    [ApiController] // Bu sınıfın bir API olduğunu belirtir
    [Route("gateway")] // Tarayıcıda "localhost:5000/gateway" yazınca buraya gelir
    public class GatewayController : ControllerBase
    {
        private readonly HttpClient _httpClient = new HttpClient();

        // Tarayıcıdan "localhost:5000/gateway/events" yazınca bu metod çalışacak
        [HttpGet("events")]
        public async Task<IActionResult> GetEvents()
        {
            // Şimdilik yolu manuel veriyoruz, test başarılı olunca otomatiğe bağlarız
            string path = "/events";

            // 1. Hedef mikroservis (Yerel test için localhost:5001 yapıyoruz)
            string targetService = "http://localhost:5001";
            string targetUrl = targetService + path;

            try
            {
                // 2. İsteği mikroservise ilet
                var response = await _httpClient.GetAsync(targetUrl);

                // 3. Yanıtı oku
                var content = await response.Content.ReadAsStringAsync();

                // 4. Yanıtı döndür
                return Content(content, "application/json");
            }
            catch (System.Exception ex)
            {
                return BadRequest("Mikroservise ulaşılamadı: " + ex.Message);
            }
        }
    }
}
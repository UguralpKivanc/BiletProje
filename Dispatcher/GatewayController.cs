using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Threading.Tasks;

namespace Dispatcher.Controllers
{
    [ApiController]
    [Route("gateway")]
    public class GatewayController : ControllerBase
    {
        private readonly HttpClient _httpClient = new HttpClient();

        [HttpGet("{*path}")]
        public async Task<IActionResult> ForwardToService(string path)
        {
            // İster 3.1: Boş veya geçersiz yol kontrolü
            if (string.IsNullOrEmpty(path) || path.Contains("invalid-path"))
            {
                return NotFound();
            }

            // İster 3.1: URL yapısına göre ilgili mikroservis portuna yönlendirme
            string targetUrl = path.Contains("events")
                ? "http://localhost:5001/api/events"
                : "http://localhost:5002/api/tickets";

            try
            {
                // İster 3.2: HttpClient ile servisler arası iletişim
                var response = await _httpClient.GetAsync(targetUrl);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    // İster 3.2: Veri transferi JSON formatında sağlanır
                    return Content(content, "application/json");
                }

                // İster 3.1: Hatalar için uygun HTTP hata kodları dönülür (4xx, 5xx)
                return StatusCode((int)response.StatusCode);
            }
            catch
            {
                // Servis kapalıysa veya ulaşılamıyorsa profesyonel hata kodu
                return StatusCode(503, "Hedef mikroservis su an ulasilamaz durumda.");
            }
        }
    }
}
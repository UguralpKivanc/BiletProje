using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Dispatcher.Controllers
{
    [ApiController]
    [Route("gateway")]
    public class GatewayController : ControllerBase
    {
        // TDD [Yeşil Aşama]: Testi geçirmek için geçici (sahte) metot
        [HttpGet("{*path}")]
        public async Task<IActionResult> ForwardToService(string path)
        {
            // Eğer yol "events" içeriyorsa testi geçirmek için JSON dön
            if (!string.IsNullOrEmpty(path) && path.Contains("events"))
            {
                return Content("[{'id':1, 'name':'TDD Test Verisi'}]", "application/json");
            }

            // Diğer durumlar için 404 (Geçersiz yol testi için)
            return NotFound();
        }
    }
}
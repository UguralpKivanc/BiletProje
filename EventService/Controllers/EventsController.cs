using Microsoft.AspNetCore.Mvc;

namespace EventService.Controllers
{
    [ApiController]
    [Route("events")]
    public class EventsController : ControllerBase
    {
        [HttpGet]
        public IActionResult GetAll()
        {
            var eventList = new[]
            {
                new { Id = 1, Name = "YazLab Sunumu", Location = "Kocaeli Üni" },
                new { Id = 2, Name = "Mikroservis Atölyesi", Location = "Laboratuvar A1" }
            };

            return Ok(eventList);
        }
    }
}
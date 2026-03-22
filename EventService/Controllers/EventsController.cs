using Microsoft.AspNetCore.Mvc;
using eventservice.Models; // Modelimizi buraya ekledik

namespace eventservice.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EventsController : ControllerBase
    {
        private static readonly List<Event> Etkinlikler = new List<Event>
        {
            new Event { Id = 1, Name = "KOÜ Bahar Şenliği", Location = "Umuttepe Kampüsü", Date = DateTime.Now.AddDays(10), Price = 0 },
            new Event { Id = 2, Name = "Yazılım Konferansı", Location = "Kocaeli Kongre Merkezi", Date = DateTime.Now.AddDays(20), Price = 150.50m }
        };

        [HttpGet]
        public IEnumerable<Event> Get()
        {
            return Etkinlikler;
        }
    }
}
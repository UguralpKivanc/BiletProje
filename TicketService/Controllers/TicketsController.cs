using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using TicketService.Models;

namespace TicketService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TicketsController : ControllerBase
    {
        private readonly IMongoCollection<Ticket> _tickets;

        public TicketsController()
        {
            var client = new MongoClient("mongodb://localhost:27017");
            var database = client.GetDatabase("BiletSistemiDb");
            _tickets = database.GetCollection<Ticket>("Tickets");
        }

        [HttpGet]
        public async Task<List<Ticket>> Get() =>
            await _tickets.Find(_ => true).ToListAsync();

        [HttpPost]
        public async Task<IActionResult> Post(Ticket ticket)
        {
            await _tickets.InsertOneAsync(ticket);
            return Ok(ticket);
        }
    }
}
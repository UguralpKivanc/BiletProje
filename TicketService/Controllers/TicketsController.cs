using Microsoft.AspNetCore.Mvc;
using TicketService.Models;
using TicketService.Repositories;

namespace TicketService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TicketsController : ControllerBase
    {
        private readonly ITicketRepository _repository;

        public TicketsController(ITicketRepository repository)
        {
            _repository = repository;
        }

        // GET /api/tickets
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var tickets = await _repository.GetAllAsync();
            return Ok(tickets);
        }

        // GET /api/tickets/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var ticket = await _repository.GetByIdAsync(id);
            if (ticket == null) return NotFound(new { error = "Bilet bulunamadı!" });
            return Ok(ticket);
        }

        // POST /api/tickets
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Ticket ticket)
        {
            if (ticket == null)
                return BadRequest("Ağam veri boş geldi, Hadise biletini gönderemedik!");
            var created = await _repository.CreateAsync(ticket);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        // PUT /api/tickets/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(string id, [FromBody] Ticket ticket)
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null) return NotFound(new { error = "Bilet bulunamadı!" });
            await _repository.UpdateAsync(id, ticket);
            return NoContent();
        }

        // DELETE /api/tickets/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null) return NotFound(new { error = "Bilet bulunamadı!" });
            await _repository.DeleteAsync(id);
            return NoContent();
        }
    }
}

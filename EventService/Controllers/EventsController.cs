using eventservice.Models;
using eventservice.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace eventservice.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EventsController : ControllerBase
    {
        private readonly IEventRepository _repository;

        public EventsController(IEventRepository repository)
        {
            _repository = repository;
        }

        // GET /api/events
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var events = await _repository.GetAllAsync();
            if (!events.Any())
                return Ok(new { message = "Bağlantı OK ama 'Events' içi boş görünüyor ağam!" });
            return Ok(events);
        }

        // GET /api/events/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var evt = await _repository.GetByIdAsync(id);
            if (evt == null) return NotFound(new { error = "Etkinlik bulunamadı!" });
            return Ok(evt);
        }

        // POST /api/events
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Event evt)
        {
            var created = await _repository.CreateAsync(evt);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        // PUT /api/events/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(string id, [FromBody] Event evt)
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null) return NotFound(new { error = "Etkinlik bulunamadı!" });
            await _repository.UpdateAsync(id, evt);
            return NoContent();
        }

        // DELETE /api/events/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null) return NotFound(new { error = "Etkinlik bulunamadı!" });
            await _repository.DeleteAsync(id);
            return NoContent();
        }
    }
}

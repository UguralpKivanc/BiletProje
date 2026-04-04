using System.Security.Claims;
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

        /// <summary>
        /// Tam liste: admin rolü veya X-Api-Key. Normal kullanıcı: yalnızca OwnerUsername kendi adı olan biletler.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Get(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 1;
            if (pageSize > 100) pageSize = 100;

            string? ownerFilter = null;
            if (!HasFullTicketAccess())
            {
                var name = User.FindFirstValue(ClaimTypes.Name);
                if (string.IsNullOrEmpty(name))
                    return Unauthorized(new { error = "Bilet listesi için giriş yapın (JWT) veya X-Api-Key kullanın." });
                ownerFilter = name;
            }

            var (tickets, total) = await _repository.GetPagedAsync(page, pageSize, ownerFilter);
            var totalPages = (int)Math.Ceiling(total / (double)pageSize);

            var items = tickets.Select(t => new
            {
                data = t,
                links = BuildLinks(t.Id!, t.EventName)
            });

            var links = new List<HateoasLink>
            {
                new HateoasLink("self", $"/api/tickets?page={page}&pageSize={pageSize}", "GET"),
                new HateoasLink("first", $"/api/tickets?page=1&pageSize={pageSize}", "GET"),
                new HateoasLink("last", $"/api/tickets?page={Math.Max(1, totalPages)}&pageSize={pageSize}", "GET"),
                new HateoasLink("create", "/api/tickets", "POST")
            };
            if (page > 1)
                links.Insert(2, new HateoasLink("prev", $"/api/tickets?page={page - 1}&pageSize={pageSize}", "GET"));
            if (page < totalPages)
                links.Add(new HateoasLink("next", $"/api/tickets?page={page + 1}&pageSize={pageSize}", "GET"));

            return Ok(new
            {
                data = items,
                pagination = new { page, pageSize, totalCount = total, totalPages },
                links
            });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var ticket = await _repository.GetByIdAsync(id);
            if (ticket == null) return NotFound(new { error = "Bilet bulunamadı!" });
            if (!CanAccessTicket(ticket)) return NotFound(new { error = "Bilet bulunamadı!" });

            return Ok(new
            {
                data = ticket,
                links = BuildLinks(id, ticket.EventName)
            });
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Ticket ticket)
        {
            if (ticket == null)
                return BadRequest(new { error = "Geçersiz istek gövdesi." });

            if (User?.Identity?.IsAuthenticated == true)
            {
                var name = User.FindFirstValue(ClaimTypes.Name);
                if (!string.IsNullOrEmpty(name))
                    ticket.OwnerUsername = name;
            }

            var created = await _repository.CreateAsync(ticket);

            var response = new
            {
                data = created,
                links = BuildLinks(created.Id!, created.EventName)
            };

            return CreatedAtAction(nameof(GetById), new { id = created.Id }, response);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Put(string id, [FromBody] Ticket ticket)
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null) return NotFound(new { error = "Bilet bulunamadı!" });
            if (!CanAccessTicket(existing)) return NotFound(new { error = "Bilet bulunamadı!" });

            ticket.Id = id;
            ticket.OwnerUsername = existing.OwnerUsername;

            await _repository.UpdateAsync(id, ticket);

            return Ok(new
            {
                data = ticket,
                links = BuildLinks(id, ticket.EventName)
            });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null) return NotFound(new { error = "Bilet bulunamadı!" });
            if (!CanAccessTicket(existing)) return NotFound(new { error = "Bilet bulunamadı!" });

            await _repository.DeleteAsync(id);
            return NoContent();
        }

        private bool HasFullTicketAccess()
        {
            if (Request.Headers.ContainsKey("X-Api-Key")) return true;
            return User?.Identity?.IsAuthenticated == true && User.IsInRole("admin");
        }

        private bool CanAccessTicket(Ticket? t)
        {
            if (t == null) return false;
            if (HasFullTicketAccess()) return true;
            var name = User.FindFirstValue(ClaimTypes.Name);
            if (string.IsNullOrEmpty(name)) return false;
            return t.OwnerUsername == name;
        }

        private static HateoasLink[] BuildLinks(string id, string eventName) =>
        [
            new HateoasLink("self", $"/api/tickets/{id}", "GET"),
            new HateoasLink("update", $"/api/tickets/{id}", "PUT"),
            new HateoasLink("delete", $"/api/tickets/{id}", "DELETE"),
            new HateoasLink("collection", "/api/tickets", "GET"),
            new HateoasLink("related-events", "/api/events", "GET")
        ];
    }
}

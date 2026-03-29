using TicketService.Models;

namespace TicketService.Repositories
{
    public interface ITicketRepository
    {
        Task<List<Ticket>> GetAllAsync();
        Task<Ticket?> GetByIdAsync(string id);
        Task<Ticket> CreateAsync(Ticket ticket);
        Task UpdateAsync(string id, Ticket ticket);
        Task DeleteAsync(string id);
    }
}

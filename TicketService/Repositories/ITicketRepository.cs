using TicketService.Models;

namespace TicketService.Repositories
{
    public interface ITicketRepository
    {
        Task<List<Ticket>> GetAllAsync();
        /// <param name="ownerUsernameFilter">null = tüm kayıtlar (yönetici); dolu = sadece bu kullanıcıya ait</param>
        Task<(List<Ticket> Items, long TotalCount)> GetPagedAsync(int page, int pageSize, string? ownerUsernameFilter = null);
        Task<Ticket?> GetByIdAsync(string id);
        Task<Ticket> CreateAsync(Ticket ticket);
        Task UpdateAsync(string id, Ticket ticket);
        Task DeleteAsync(string id);
    }
}

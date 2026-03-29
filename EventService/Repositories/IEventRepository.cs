using eventservice.Models;

namespace eventservice.Repositories
{
    public interface IEventRepository
    {
        Task<List<Event>> GetAllAsync();
        Task<Event?> GetByIdAsync(string id);
        Task<Event> CreateAsync(Event evt);
        Task UpdateAsync(string id, Event evt);
        Task DeleteAsync(string id);
    }
}

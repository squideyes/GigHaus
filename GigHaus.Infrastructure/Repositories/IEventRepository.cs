using GigHaus.Domain.Entities;

namespace GigHaus.Infrastructure.Repositories;

public interface IEventRepository
{
    Task<IEnumerable<Event>> GetAllAsync(CancellationToken ct = default);
    Task<Event?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Event> CreateAsync(Event ev, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}


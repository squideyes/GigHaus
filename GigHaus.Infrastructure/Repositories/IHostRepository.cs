using GigHaus.Domain.Entities;

namespace GigHaus.Infrastructure.Repositories;

public interface IHostRepository
{
    Task<IEnumerable<Host>> GetAllAsync(CancellationToken ct = default);
    Task<Host?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Host> CreateAsync(Host host, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}


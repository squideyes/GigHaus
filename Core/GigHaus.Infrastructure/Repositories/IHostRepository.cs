using GigHaus.Domain.Entities;
using GigHaus.Domain.Enums;

namespace GigHaus.Infrastructure.Repositories;

public interface IHostRepository
{
    Task<IEnumerable<Host>> GetAllAsync(CancellationToken ct = default);
    Task<Host?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Host?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<Host> CreateAsync(Host host, CancellationToken ct = default);
    Task<Host?> UpdateAsync(Host host, CancellationToken ct = default);
    Task<bool> SetStatusAsync(Guid id, HostStatus status, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}

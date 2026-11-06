using GigHaus.Domain.Entities;

namespace GigHaus.Infrastructure.Repositories;

public interface IUserRepository
{
    Task<ApplicationUser?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<ApplicationUser?> GetByNormalizedEmailAsync(string normalizedEmail, CancellationToken ct = default);
    Task<IReadOnlyList<ApplicationUser>> GetByRoleAsync(string role, CancellationToken ct = default);
    Task<int> CountByRoleAsync(string role, CancellationToken ct = default);
    Task CreateAsync(ApplicationUser user, CancellationToken ct = default);
    Task UpdateAsync(ApplicationUser user, CancellationToken ct = default);
    Task<bool> SoftDeleteAsync(Guid id, CancellationToken ct = default);
}

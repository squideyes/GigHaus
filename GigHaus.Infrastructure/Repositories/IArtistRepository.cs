using GigHaus.Domain.Entities;

namespace GigHaus.Infrastructure.Repositories;

public interface IArtistRepository
{
    Task<IEnumerable<Artist>> GetAllAsync(CancellationToken ct = default);
    Task<Artist?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Artist> CreateAsync(Artist artist, CancellationToken ct = default);
    Task<Artist?> UpdateAsync(Guid id, Artist artist, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}


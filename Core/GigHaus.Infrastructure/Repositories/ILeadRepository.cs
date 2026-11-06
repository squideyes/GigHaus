using GigHaus.Domain.Entities;

namespace GigHaus.Infrastructure.Repositories;

public interface ILeadRepository
{
    Task<IReadOnlyList<Lead>> GetAllAsync(CancellationToken ct = default);
    Task<Lead> CreateAsync(Lead lead, CancellationToken ct = default);
    Task UpdateAsync(Lead lead, CancellationToken ct = default);
    Task<bool> SoftDeleteAsync(Guid id, CancellationToken ct = default);

    /// <summary>Moves open leads with this email to SignedUp once the person creates an account.</summary>
    Task<int> MarkSignedUpAsync(string email, CancellationToken ct = default);
}

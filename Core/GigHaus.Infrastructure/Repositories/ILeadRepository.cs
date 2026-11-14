using GigHaus.Domain.Entities;

namespace GigHaus.Infrastructure.Repositories;

public interface ILeadRepository
{
    Task<IReadOnlyList<Lead>> GetAllAsync(CancellationToken ct = default);
    Task<Lead?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Finds the lead holding an invitation, whether or not it has expired.</summary>
    Task<Lead?> GetByInviteTokenHashAsync(string tokenHash, CancellationToken ct = default);

    /// <summary>The newest lead for this email that hasn't signed up, if any.</summary>
    Task<Lead?> GetOpenByEmailAsync(string email, CancellationToken ct = default);

    /// <summary>Stores a new invitation, replacing any earlier one, and moves the lead to Invited.</summary>
    Task<bool> SetInvitedAsync(Guid id, string tokenHash, DateTime expiresAt, CancellationToken ct = default);

    Task<Lead> CreateAsync(Lead lead, CancellationToken ct = default);
    Task UpdateAsync(Lead lead, CancellationToken ct = default);
    Task<bool> SoftDeleteAsync(Guid id, CancellationToken ct = default);

    /// <summary>Moves open leads with this email to SignedUp once the person creates an account, and spends their invitation.</summary>
    Task<int> MarkSignedUpAsync(string email, CancellationToken ct = default);
}

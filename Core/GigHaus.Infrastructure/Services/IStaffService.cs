using GigHaus.Domain.Entities;

namespace GigHaus.Infrastructure.Services;

/// <summary>
/// Manages the GigHaus staff accounts that can reach the admin area.
/// </summary>
public interface IStaffService
{
    Task<IReadOnlyList<ApplicationUser>> GetStaffAsync(CancellationToken ct = default);
    Task<Result<ApplicationUser>> AddAsync(string displayName, string email, string password, CancellationToken ct = default);

    /// <summary>Removes a staff account. You can't remove yourself, so there's always one left.</summary>
    Task<Result<ApplicationUser>> RemoveAsync(Guid staffId, Guid actingUserId, CancellationToken ct = default);
}

using GigHaus.Domain.Entities;
using GigHaus.Domain.Enums;

namespace GigHaus.Infrastructure.Services;

public sealed record HostSignup
{
    public required string Name { get; init; }
    public required string Email { get; init; }
    public required string Password { get; init; }
    public string? Phone { get; init; }
    public required string Address { get; init; }
    public required string City { get; init; }
    public required string State { get; init; }
    public required string ZipCode { get; init; }
    public required int MaxCapacity { get; init; }
    public string? VenueDescription { get; init; }
}

/// <summary>
/// Moves a host from signing up to being approved.
/// </summary>
public interface IHostOnboardingService
{
    Task<Result<ApplicationUser>> SignUpAsync(HostSignup signup, CancellationToken ct = default);

    /// <summary>Changes a host's review state and emails them when they're approved.</summary>
    Task<Result<Host>> SetStatusAsync(Guid hostId, HostStatus status, string portalUrl, CancellationToken ct = default);
}

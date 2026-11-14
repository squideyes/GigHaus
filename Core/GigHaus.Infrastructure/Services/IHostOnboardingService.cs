using GigHaus.Domain.Entities;
using GigHaus.Domain.Enums;

namespace GigHaus.Infrastructure.Services;

public sealed record HostSignup
{
    /// <summary>The token from the emailed sign-up link. It decides which email the account gets.</summary>
    public required string InviteToken { get; init; }
    public required string Name { get; init; }
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
/// Brings a host in by invitation. GigHaus is invite only: people ask for an invitation,
/// the admin emails a single-use sign-up link, and only that link can create an account.
/// </summary>
public interface IHostOnboardingService
{
    /// <summary>Emails a sign-up link to someone who asked for one. Inviting again replaces the earlier link.</summary>
    /// <param name="signupUrl">Absolute address of the sign-up page. The token is added to it.</param>
    Task<Result<Lead>> InviteAsync(Guid leadId, string signupUrl, CancellationToken ct = default);

    /// <summary>Invites someone who never asked, reusing their open request if they have one.</summary>
    Task<Result<Lead>> InviteNewAsync(string name, string email, string signupUrl, CancellationToken ct = default);

    /// <summary>Returns the invitation behind a sign-up link, or null when it's unknown, used or expired.</summary>
    Task<Lead?> FindInvitationAsync(string? inviteToken, CancellationToken ct = default);

    /// <summary>Creates the account and venue for an invited host. Invited hosts start out approved.</summary>
    Task<Result<ApplicationUser>> SignUpAsync(HostSignup signup, CancellationToken ct = default);

    /// <summary>Changes a host's review state and emails them when they're approved.</summary>
    Task<Result<Host>> SetStatusAsync(Guid hostId, HostStatus status, string portalUrl, CancellationToken ct = default);
}

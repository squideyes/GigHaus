namespace GigHaus.Domain.Enums;

/// <summary>
/// Where a host stands with GigHaus. Only approved hosts can publish shows.
/// </summary>
public enum HostStatus
{
    /// <summary>Signed up and waiting for review.</summary>
    Pending = 0,

    /// <summary>Reviewed and able to publish shows.</summary>
    Approved = 1,

    /// <summary>Blocked by the admin. Published shows stop accepting reservations.</summary>
    Suspended = 2
}

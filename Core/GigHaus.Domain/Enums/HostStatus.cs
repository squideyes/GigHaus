namespace GigHaus.Domain.Enums;

/// <summary>
/// Where a host stands with GigHaus staff. Only approved hosts can publish shows.
/// </summary>
public enum HostStatus
{
    /// <summary>Signed up and waiting for staff review.</summary>
    Pending = 0,

    /// <summary>Reviewed by staff and able to publish shows.</summary>
    Approved = 1,

    /// <summary>Blocked by staff. Published shows stop accepting reservations.</summary>
    Suspended = 2
}

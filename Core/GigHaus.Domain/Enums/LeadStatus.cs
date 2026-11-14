namespace GigHaus.Domain.Enums;

/// <summary>
/// Where a prospective host stands, from asking for an invitation to joining.
/// </summary>
public enum LeadStatus
{
    /// <summary>Asked for an invitation and hasn't been answered.</summary>
    New = 0,

    Contacted = 1,

    /// <summary>Used their invitation and has an account.</summary>
    SignedUp = 2,

    NotInterested = 3,

    /// <summary>Sent a sign-up link that hasn't been used yet.</summary>
    Invited = 4
}

using GigHaus.Domain.Enums;

namespace GigHaus.Domain.Entities;

/// <summary>
/// A prospective host: someone who asked for an invitation, or whom the admin invited directly.
/// </summary>
public class Lead : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public int? EstimatedCapacity { get; set; }
    public string? Message { get; set; }
    public LeadStatus Status { get; set; } = LeadStatus.New;

    /// <summary>Private notes for the GigHaus admin.</summary>
    public string? Notes { get; set; }

    /// <summary>SHA-256 of the sign-up link's token. The token itself is only ever in the email.</summary>
    public string? InviteTokenHash { get; set; }
    public DateTime? InvitedAt { get; set; }
    public DateTime? InviteExpiresAt { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}

namespace GigHaus.Domain.Entities;

/// <summary>
/// A person who can sign in: the GigHaus admin or a host.
/// </summary>
public class ApplicationUser : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public string NormalizedEmail { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? PasswordHash { get; set; }
    public string SecurityStamp { get; set; } = Guid.NewGuid().ToString("N");
    public bool EmailConfirmed { get; set; }
    public DateTime? LockoutEndUtc { get; set; }
    public bool LockoutEnabled { get; set; } = true;
    public int AccessFailedCount { get; set; }

    /// <summary>One of <see cref="UserRoles"/>.</summary>
    public string Role { get; set; } = UserRoles.Host;

    /// <summary>The host profile this user manages. Null for the admin.</summary>
    public Guid? HostId { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}

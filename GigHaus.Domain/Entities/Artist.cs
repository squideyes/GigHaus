namespace GigHaus.Domain.Entities;

public class Artist : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Website { get; set; }

    /// <summary>
    /// Stripe Connect account ID for platform payouts (Phase 2+).
    /// </summary>
    public string? StripeConnectAccountId { get; set; }

    // Navigation
    public ICollection<Event> Events { get; set; } = [];
}


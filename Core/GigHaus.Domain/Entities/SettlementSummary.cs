namespace GigHaus.Domain.Entities;

/// <summary>
/// Per-event settlement summary: totals for contributions collected,
/// fees, and net amounts attributable to the artist.
/// Follows the legal/pricing doc's recommended settlement model.
/// </summary>
public class SettlementSummary : BaseEntity
{
    public Guid EventId { get; set; }
    public Event Event { get; set; } = null!;

    // Totals
    public decimal TotalOnlineCollected { get; set; }
    public decimal TotalDoorCashCollected { get; set; }
    public decimal TotalOtherCollected { get; set; }
    public decimal TotalGrossCollected { get; set; }

    // Deductions
    public decimal TotalProcessorFees { get; set; }
    public decimal TotalPlatformFees { get; set; }
    public decimal TotalFees { get; set; }

    // Net
    public decimal NetAttributableToArtist { get; set; }

    // Counts
    public int TotalReservations { get; set; }
    public int TotalCheckedIn { get; set; }
    public int TotalGuests { get; set; }
    public int TotalComps { get; set; }

    /// <summary>Whether the settlement has been finalized (no more changes expected).</summary>
    public bool IsFinalized { get; set; }

    /// <summary>Payout status for the artist.</summary>
    public string PayoutStatus { get; set; } = "Pending";

    /// <summary>Stripe transfer/payout ID once funds are sent to artist.</summary>
    public string? StripePayoutId { get; set; }
}


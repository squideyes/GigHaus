using GigHaus.Domain.Enums;

namespace GigHaus.Domain.Entities;

/// <summary>
/// Represents a single payment / contribution tied to a reservation.
/// Follows the domain model from the legal/pricing notes.
/// </summary>
public class PaymentRecord : BaseEntity
{
    public Guid ReservationId { get; set; }
    public Reservation Reservation { get; set; } = null!;

    public decimal GrossAmount { get; set; }
    public decimal ProcessorFeeAmount { get; set; }
    public decimal PlatformFeeAmount { get; set; }
    public decimal NetAmount { get; set; }

    /// <summary>Amount ultimately attributable to the artist.</summary>
    public decimal AttributableToArtistAmount { get; set; }

    public PaymentMethodType PaymentMethodType { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    /// <summary>Payment provider name, e.g. "Stripe".</summary>
    public string? Provider { get; set; }

    /// <summary>External reference from the provider (charge ID, etc.).</summary>
    public string? ProviderReference { get; set; }

    /// <summary>Stripe PaymentIntent ID for tracking.</summary>
    public string? StripePaymentIntentId { get; set; }

    /// <summary>Stripe connected account that receives funds.</summary>
    public string? StripeConnectedAccountId { get; set; }
}


namespace GigHaus.Domain.Enums;

/// <summary>
/// Determines how contributions are handled for an event.
/// </summary>
public enum PricingMode
{
    /// <summary>Suggested donation only — attendee may pay any amount or nothing.</summary>
    SuggestedOnly = 0,

    /// <summary>A minimum contribution is required for entry.</summary>
    MinimumRequired = 1,

    /// <summary>Attendee reserves now and pays at the door.</summary>
    ReserveNowPayLater = 2,

    /// <summary>Payment accepted only at the door (no online payment).</summary>
    DoorOnly = 3
}


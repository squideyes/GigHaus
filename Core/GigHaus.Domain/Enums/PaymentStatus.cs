namespace GigHaus.Domain.Enums;

/// <summary>
/// Tracks the lifecycle of a payment/contribution.
/// </summary>
public enum PaymentStatus
{
    /// <summary>Payment intent created but not yet completed.</summary>
    Pending = 0,

    /// <summary>Payment successfully processed.</summary>
    Succeeded = 1,

    /// <summary>Payment failed during processing.</summary>
    Failed = 2,

    /// <summary>Payment was refunded.</summary>
    Refunded = 3,

    /// <summary>Payment is disputed / charge-back initiated.</summary>
    Disputed = 4
}


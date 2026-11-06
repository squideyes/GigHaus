using GigHaus.Domain.Entities;

namespace GigHaus.Infrastructure.Services;

public interface IPaymentService
{
    /// <summary>
    /// Creates a payment intent for an online contribution.
    /// Returns a client secret for the frontend to complete payment.
    /// </summary>
    Task<PaymentIntentResult> CreatePaymentIntentAsync(CreatePaymentRequest request, CancellationToken ct = default);

    /// <summary>
    /// Records a cash/door payment (no external processor).
    /// </summary>
    Task<PaymentResult> RecordDoorPaymentAsync(DoorPaymentRequest request, CancellationToken ct = default);

    /// <summary>
    /// Calculates fee breakdown for a given gross amount.
    /// </summary>
    FeeBreakdown CalculateFees(decimal grossAmount);
}

public record CreatePaymentRequest(
    Guid ReservationId,
    decimal Amount,
    string? StripeConnectedAccountId,
    string? Description = null
);

public record DoorPaymentRequest(
    Guid ReservationId,
    decimal Amount
);

public class PaymentIntentResult
{
    public bool Success { get; init; }
    public string? Error { get; init; }
    public string? ClientSecret { get; init; }
    public string? PaymentIntentId { get; init; }
    public PaymentRecord? PaymentRecord { get; init; }

    public static PaymentIntentResult Ok(string clientSecret, string intentId, PaymentRecord record) =>
        new() { Success = true, ClientSecret = clientSecret, PaymentIntentId = intentId, PaymentRecord = record };
    public static PaymentIntentResult Fail(string error) =>
        new() { Success = false, Error = error };
}

public class PaymentResult
{
    public bool Success { get; init; }
    public string? Error { get; init; }
    public PaymentRecord? PaymentRecord { get; init; }

    public static PaymentResult Ok(PaymentRecord record) => new() { Success = true, PaymentRecord = record };
    public static PaymentResult Fail(string error) => new() { Success = false, Error = error };
}

public record FeeBreakdown(
    decimal GrossAmount,
    decimal ProcessorFee,
    decimal PlatformFee,
    decimal NetAmount,
    decimal AttributableToArtist
);


using GigHaus.Domain.Entities;
using GigHaus.Domain.Enums;
using GigHaus.Infrastructure.Repositories;

namespace GigHaus.Infrastructure.Services;

/// <summary>
/// Stripe Connect payment service.
/// Uses Stripe.net SDK for real payment processing in production.
/// In dev mode (when no Stripe key is configured), operates as a stub
/// that creates payment records without calling Stripe.
/// </summary>
public class StripePaymentService : IPaymentService
{
    private readonly IPaymentRecordRepository _payments;
    private readonly IReservationRepository _reservations;
    private readonly StripeOptions _options;

    // Stripe Connect fee structure (configurable)
    private const decimal StripePercentageFee = 0.029m; // 2.9%
    private const decimal StripeFixedFee = 0.30m;       // $0.30
    private const decimal PlatformPercentageFee = 0.02m; // 2% platform fee

    public StripePaymentService(
        IPaymentRecordRepository payments,
        IReservationRepository reservations,
        StripeOptions options)
    {
        _payments = payments;
        _reservations = reservations;
        _options = options;
    }

    public FeeBreakdown CalculateFees(decimal grossAmount)
    {
        var processorFee = Math.Round(grossAmount * StripePercentageFee + StripeFixedFee, 2, MidpointRounding.AwayFromZero);
        var platformFee = Math.Round(grossAmount * PlatformPercentageFee, 2, MidpointRounding.AwayFromZero);
        var netAmount = grossAmount - processorFee - platformFee;
        var artistAmount = netAmount; // all net goes to artist

        return new FeeBreakdown(grossAmount, processorFee, platformFee, netAmount, artistAmount);
    }

    public async Task<PaymentIntentResult> CreatePaymentIntentAsync(CreatePaymentRequest request, CancellationToken ct = default)
    {
        var reservation = await _reservations.GetByIdAsync(request.ReservationId, ct);
        if (reservation is null)
            return PaymentIntentResult.Fail("Reservation not found.");

        if (reservation.Status == ReservationStatus.Cancelled)
            return PaymentIntentResult.Fail("Cannot pay for a cancelled reservation.");

        var fees = CalculateFees(request.Amount);

        // In dev/stub mode: create a fake payment intent
        var intentId = _options.IsDevMode
            ? $"pi_dev_{Guid.NewGuid():N}"
            : await CreateStripePaymentIntentAsync(request, fees, ct);

        var clientSecret = _options.IsDevMode
            ? $"{intentId}_secret_dev"
            : $"{intentId}_secret"; // Real Stripe would return this

        var record = new PaymentRecord
        {
            ReservationId = request.ReservationId,
            GrossAmount = fees.GrossAmount,
            ProcessorFeeAmount = fees.ProcessorFee,
            PlatformFeeAmount = fees.PlatformFee,
            NetAmount = fees.NetAmount,
            AttributableToArtistAmount = fees.AttributableToArtist,
            PaymentMethodType = PaymentMethodType.Online,
            Status = _options.IsDevMode ? PaymentStatus.Succeeded : PaymentStatus.Pending,
            Provider = "Stripe",
            StripePaymentIntentId = intentId,
            StripeConnectedAccountId = request.StripeConnectedAccountId
        };

        await _payments.CreateAsync(record, ct);

        // In dev mode, auto-confirm the reservation
        if (_options.IsDevMode && reservation.Status == ReservationStatus.Pending)
        {
            reservation.Status = ReservationStatus.Confirmed;
            reservation.ConfirmedAt = DateTime.UtcNow;
            await _reservations.UpdateAsync(reservation, ct);
        }

        return PaymentIntentResult.Ok(clientSecret, intentId, record);
    }

    public async Task<PaymentResult> RecordDoorPaymentAsync(DoorPaymentRequest request, CancellationToken ct = default)
    {
        var reservation = await _reservations.GetByIdAsync(request.ReservationId, ct);
        if (reservation is null)
            return PaymentResult.Fail("Reservation not found.");

        var record = new PaymentRecord
        {
            ReservationId = request.ReservationId,
            GrossAmount = request.Amount,
            ProcessorFeeAmount = 0,
            PlatformFeeAmount = 0,
            NetAmount = request.Amount,
            AttributableToArtistAmount = request.Amount,
            PaymentMethodType = PaymentMethodType.CashAtDoor,
            Status = PaymentStatus.Succeeded,
            Provider = "Cash"
        };

        await _payments.CreateAsync(record, ct);
        return PaymentResult.Ok(record);
    }

    /// <summary>
    /// Real Stripe API call — placeholder for Stripe.net SDK integration.
    /// </summary>
    private Task<string> CreateStripePaymentIntentAsync(
        CreatePaymentRequest request, FeeBreakdown fees, CancellationToken ct)
    {
        // TODO: Integrate Stripe.net SDK
        // var options = new PaymentIntentCreateOptions { ... };
        // var service = new PaymentIntentService();
        // var intent = await service.CreateAsync(options, cancellationToken: ct);
        // return intent.Id;
        throw new NotImplementedException(
            "Real Stripe integration requires Stripe.net SDK. Set IsDevMode=true for local development.");
    }
}

public class StripeOptions
{
    public string? SecretKey { get; set; }
    public string? PublishableKey { get; set; }
    public string? WebhookSecret { get; set; }
    public bool IsDevMode { get; set; } = true;
}


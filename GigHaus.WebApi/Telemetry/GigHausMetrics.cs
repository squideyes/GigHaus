using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace GigHaus.WebApi.Telemetry;

/// <summary>
/// Custom OpenTelemetry metrics for GigHaus business operations.
/// These metrics feed into the Aspire Dashboard and any configured OTLP exporter.
/// </summary>
public sealed class GigHausMetrics
{
    public const string MeterName = "GigHaus";

    private readonly Counter<long> _reservationsCreated;
    private readonly Counter<long> _reservationsConfirmed;
    private readonly Counter<long> _reservationsCancelled;
    private readonly Counter<long> _checkIns;
    private readonly Counter<long> _paymentsProcessed;
    private readonly Histogram<double> _paymentAmounts;
    private readonly Counter<long> _doorPayments;
    private readonly Counter<long> _onlinePayments;

    public static readonly ActivitySource ActivitySource = new(MeterName);

    public GigHausMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);

        _reservationsCreated = meter.CreateCounter<long>(
            "gighaus.reservations.created",
            unit: "{reservation}",
            description: "Total reservations created");

        _reservationsConfirmed = meter.CreateCounter<long>(
            "gighaus.reservations.confirmed",
            unit: "{reservation}",
            description: "Total reservations confirmed");

        _reservationsCancelled = meter.CreateCounter<long>(
            "gighaus.reservations.cancelled",
            unit: "{reservation}",
            description: "Total reservations cancelled");

        _checkIns = meter.CreateCounter<long>(
            "gighaus.checkins",
            unit: "{checkin}",
            description: "Total guest check-ins");

        _paymentsProcessed = meter.CreateCounter<long>(
            "gighaus.payments.processed",
            unit: "{payment}",
            description: "Total payments processed");

        _paymentAmounts = meter.CreateHistogram<double>(
            "gighaus.payments.amount",
            unit: "USD",
            description: "Distribution of payment amounts (gross)");

        _doorPayments = meter.CreateCounter<long>(
            "gighaus.payments.door",
            unit: "{payment}",
            description: "Total door/cash payments");

        _onlinePayments = meter.CreateCounter<long>(
            "gighaus.payments.online",
            unit: "{payment}",
            description: "Total online payments");
    }

    public void ReservationCreated(Guid eventId)
        => _reservationsCreated.Add(1, new KeyValuePair<string, object?>("event.id", eventId.ToString()));

    public void ReservationConfirmed(Guid eventId)
        => _reservationsConfirmed.Add(1, new KeyValuePair<string, object?>("event.id", eventId.ToString()));

    public void ReservationCancelled(Guid eventId)
        => _reservationsCancelled.Add(1, new KeyValuePair<string, object?>("event.id", eventId.ToString()));

    public void CheckIn(Guid eventId)
        => _checkIns.Add(1, new KeyValuePair<string, object?>("event.id", eventId.ToString()));

    public void PaymentProcessed(decimal grossAmount, string provider)
    {
        _paymentsProcessed.Add(1, new KeyValuePair<string, object?>("payment.provider", provider));
        _paymentAmounts.Record((double)grossAmount, new KeyValuePair<string, object?>("payment.provider", provider));

        if (provider.Equals("Cash", StringComparison.OrdinalIgnoreCase)
            || provider.Equals("Door", StringComparison.OrdinalIgnoreCase))
            _doorPayments.Add(1);
        else
            _onlinePayments.Add(1);
    }
}


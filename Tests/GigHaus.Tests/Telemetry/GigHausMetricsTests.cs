using System.Diagnostics.Metrics;
using GigHaus.WebApi.Telemetry;

namespace GigHaus.Tests.Telemetry;

public class GigHausMetricsTests : IDisposable
{
    private readonly IMeterFactory _meterFactory;
    private readonly GigHausMetrics _metrics;
    private readonly MeterListener _listener;
    private readonly List<(string Name, object Value)> _measurements = [];

    public GigHausMetricsTests()
    {
        _meterFactory = new TestMeterFactory();
        _metrics = new GigHausMetrics(_meterFactory);
        _listener = new MeterListener();
        _listener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == GigHausMetrics.MeterName)
                listener.EnableMeasurementEvents(instrument);
        };
        _listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, state) =>
            _measurements.Add((instrument.Name, measurement)));
        _listener.SetMeasurementEventCallback<double>((instrument, measurement, tags, state) =>
            _measurements.Add((instrument.Name, measurement)));
        _listener.Start();
    }

    [Fact]
    public void ReservationCreated_RecordsMetric()
    {
        _metrics.ReservationCreated(Guid.NewGuid());
        _listener.RecordObservableInstruments();
        Assert.Contains(_measurements, m => m.Name == "gighaus.reservations.created");
    }

    [Fact]
    public void ReservationConfirmed_RecordsMetric()
    {
        _metrics.ReservationConfirmed(Guid.NewGuid());
        _listener.RecordObservableInstruments();
        Assert.Contains(_measurements, m => m.Name == "gighaus.reservations.confirmed");
    }

    [Fact]
    public void ReservationCancelled_RecordsMetric()
    {
        _metrics.ReservationCancelled(Guid.NewGuid());
        _listener.RecordObservableInstruments();
        Assert.Contains(_measurements, m => m.Name == "gighaus.reservations.cancelled");
    }

    [Fact]
    public void CheckIn_RecordsMetric()
    {
        _metrics.CheckIn(Guid.NewGuid());
        _listener.RecordObservableInstruments();
        Assert.Contains(_measurements, m => m.Name == "gighaus.checkins");
    }

    [Fact]
    public void PaymentProcessed_Online_RecordsMetrics()
    {
        _metrics.PaymentProcessed(50m, "Stripe");
        _listener.RecordObservableInstruments();
        Assert.Contains(_measurements, m => m.Name == "gighaus.payments.processed");
        Assert.Contains(_measurements, m => m.Name == "gighaus.payments.amount");
        Assert.Contains(_measurements, m => m.Name == "gighaus.payments.online");
        Assert.DoesNotContain(_measurements, m => m.Name == "gighaus.payments.door");
    }

    [Fact]
    public void PaymentProcessed_Cash_RecordsDoorMetric()
    {
        _metrics.PaymentProcessed(30m, "Cash");
        _listener.RecordObservableInstruments();
        Assert.Contains(_measurements, m => m.Name == "gighaus.payments.door");
        Assert.DoesNotContain(_measurements, m => m.Name == "gighaus.payments.online");
    }

    [Fact]
    public void PaymentProcessed_Door_RecordsDoorMetric()
    {
        _metrics.PaymentProcessed(20m, "Door");
        _listener.RecordObservableInstruments();
        Assert.Contains(_measurements, m => m.Name == "gighaus.payments.door");
    }

    [Fact]
    public void PaymentProcessed_RecordsCorrectAmount()
    {
        _metrics.PaymentProcessed(99.99m, "Stripe");
        _listener.RecordObservableInstruments();
        var amount = _measurements.FirstOrDefault(m => m.Name == "gighaus.payments.amount");
        Assert.Equal(99.99d, (double)amount.Value, 2);
    }

    [Fact]
    public void MeterName_IsGigHaus()
    {
        Assert.Equal("GigHaus", GigHausMetrics.MeterName);
    }

    [Fact]
    public void ActivitySource_HasCorrectName()
    {
        Assert.Equal("GigHaus", GigHausMetrics.ActivitySource.Name);
    }

    public void Dispose()
    {
        _listener.Dispose();
        _meterFactory.Dispose();
    }
}

/// <summary>Simple IMeterFactory implementation for testing.</summary>
internal sealed class TestMeterFactory : IMeterFactory
{
    private readonly List<Meter> _meters = [];

    public Meter Create(MeterOptions options)
    {
        var meter = new Meter(options);
        _meters.Add(meter);
        return meter;
    }

    public void Dispose()
    {
        foreach (var m in _meters) m.Dispose();
    }
}


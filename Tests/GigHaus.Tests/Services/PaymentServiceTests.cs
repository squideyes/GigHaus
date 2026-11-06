using GigHaus.Domain.Entities;
using GigHaus.Domain.Enums;
using GigHaus.Infrastructure.Repositories;
using GigHaus.Infrastructure.Services;
using NSubstitute;

namespace GigHaus.Tests.Services;

public class PaymentServiceTests
{
    private readonly IPaymentRecordRepository _paymentRepo = Substitute.For<IPaymentRecordRepository>();
    private readonly IReservationRepository _reservationRepo = Substitute.For<IReservationRepository>();
    private readonly StripePaymentService _svc;

    public PaymentServiceTests()
    {
        _svc = new StripePaymentService(_paymentRepo, _reservationRepo, new StripeOptions { IsDevMode = true });
    }

    [Fact]
    public void CalculateFees_CorrectBreakdown()
    {
        // $50 gross: processor = 50*0.029 + 0.30 = $1.75, and GigHaus takes no platform fee
        var fees = _svc.CalculateFees(50m);

        Assert.Equal(50m, fees.GrossAmount);
        Assert.Equal(1.75m, fees.ProcessorFee);
        Assert.Equal(0m, fees.PlatformFee);
        Assert.Equal(48.25m, fees.NetAmount);
        Assert.Equal(48.25m, fees.AttributableToArtist);
    }

    [Fact]
    public void CalculateFees_ZeroAmount()
    {
        var fees = _svc.CalculateFees(0m);

        Assert.Equal(0m, fees.GrossAmount);
        Assert.Equal(0.30m, fees.ProcessorFee); // fixed fee still applies
        Assert.Equal(0m, fees.PlatformFee);
    }

    [Theory]
    [InlineData(10, 0.59, 0, 9.41)]
    [InlineData(25, 1.03, 0, 23.97)]
    [InlineData(100, 3.20, 0, 96.80)]
    public void CalculateFees_VariousAmounts(decimal gross, decimal expectedProcessor, decimal expectedPlatform, decimal expectedNet)
    {
        var fees = _svc.CalculateFees(gross);

        Assert.Equal(expectedProcessor, fees.ProcessorFee);
        Assert.Equal(expectedPlatform, fees.PlatformFee);
        Assert.Equal(expectedNet, fees.NetAmount);
    }

    [Fact]
    public async Task CreatePaymentIntent_DevMode_Succeeds()
    {
        var reservation = new Reservation
        {
            Status = ReservationStatus.Pending,
            AttendeeName = "Alice",
            AttendeeEmail = "a@t.com"
        };
        _reservationRepo.GetByIdAsync(reservation.Id).Returns(reservation);
        _paymentRepo.CreateAsync(Arg.Any<PaymentRecord>()).Returns(ci => ci.Arg<PaymentRecord>());
        _reservationRepo.UpdateAsync(Arg.Any<Reservation>()).Returns(ci => ci.Arg<Reservation>());

        var result = await _svc.CreatePaymentIntentAsync(
            new CreatePaymentRequest(reservation.Id, 50m, null));

        Assert.True(result.Success);
        Assert.NotNull(result.ClientSecret);
        Assert.NotNull(result.PaymentIntentId);
        Assert.StartsWith("pi_dev_", result.PaymentIntentId);
        Assert.Equal(PaymentStatus.Succeeded, result.PaymentRecord!.Status);
    }

    [Fact]
    public async Task CreatePaymentIntent_AutoConfirms_InDevMode()
    {
        var reservation = new Reservation { Status = ReservationStatus.Pending };
        _reservationRepo.GetByIdAsync(reservation.Id).Returns(reservation);
        _paymentRepo.CreateAsync(Arg.Any<PaymentRecord>()).Returns(ci => ci.Arg<PaymentRecord>());
        _reservationRepo.UpdateAsync(Arg.Any<Reservation>()).Returns(ci => ci.Arg<Reservation>());

        await _svc.CreatePaymentIntentAsync(new CreatePaymentRequest(reservation.Id, 25m, null));

        await _reservationRepo.Received(1).UpdateAsync(Arg.Is<Reservation>(r =>
            r.Status == ReservationStatus.Confirmed && r.ConfirmedAt != null));
    }

    [Fact]
    public async Task CreatePaymentIntent_Fails_WhenCancelled()
    {
        var reservation = new Reservation { Status = ReservationStatus.Cancelled };
        _reservationRepo.GetByIdAsync(reservation.Id).Returns(reservation);

        var result = await _svc.CreatePaymentIntentAsync(
            new CreatePaymentRequest(reservation.Id, 50m, null));

        Assert.False(result.Success);
        Assert.Contains("cancelled", result.Error!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RecordDoorPayment_NoFees()
    {
        var reservation = new Reservation { Status = ReservationStatus.Confirmed };
        _reservationRepo.GetByIdAsync(reservation.Id).Returns(reservation);
        _paymentRepo.CreateAsync(Arg.Any<PaymentRecord>()).Returns(ci => ci.Arg<PaymentRecord>());

        var result = await _svc.RecordDoorPaymentAsync(
            new DoorPaymentRequest(reservation.Id, 30m));

        Assert.True(result.Success);
        Assert.Equal(30m, result.PaymentRecord!.GrossAmount);
        Assert.Equal(0m, result.PaymentRecord.ProcessorFeeAmount);
        Assert.Equal(0m, result.PaymentRecord.PlatformFeeAmount);
        Assert.Equal(30m, result.PaymentRecord.NetAmount);
        Assert.Equal(30m, result.PaymentRecord.AttributableToArtistAmount);
        Assert.Equal(PaymentMethodType.CashAtDoor, result.PaymentRecord.PaymentMethodType);
    }

    [Fact]
    public async Task RecordDoorPayment_Fails_WhenReservationNotFound()
    {
        _reservationRepo.GetByIdAsync(Arg.Any<Guid>()).Returns((Reservation?)null);

        var result = await _svc.RecordDoorPaymentAsync(
            new DoorPaymentRequest(Guid.NewGuid(), 30m));

        Assert.False(result.Success);
        Assert.Contains("not found", result.Error!);
    }

    [Fact]
    public async Task CreatePaymentIntent_Fails_WhenReservationNotFound()
    {
        _reservationRepo.GetByIdAsync(Arg.Any<Guid>()).Returns((Reservation?)null);

        var result = await _svc.CreatePaymentIntentAsync(
            new CreatePaymentRequest(Guid.NewGuid(), 50m, null));

        Assert.False(result.Success);
        Assert.Contains("not found", result.Error!);
    }

    [Fact]
    public async Task CreatePaymentIntent_AlreadyConfirmed_DoesNotDoubleConfirm()
    {
        var reservation = new Reservation
        {
            Status = ReservationStatus.Confirmed,
            ConfirmedAt = DateTime.UtcNow.AddMinutes(-10)
        };
        _reservationRepo.GetByIdAsync(reservation.Id).Returns(reservation);
        _paymentRepo.CreateAsync(Arg.Any<PaymentRecord>()).Returns(ci => ci.Arg<PaymentRecord>());

        var result = await _svc.CreatePaymentIntentAsync(
            new CreatePaymentRequest(reservation.Id, 50m, null));

        Assert.True(result.Success);
        // Should NOT call UpdateAsync since reservation is already confirmed
        await _reservationRepo.DidNotReceive().UpdateAsync(Arg.Any<Reservation>());
    }

    [Fact]
    public void CalculateFees_LargeAmount_CorrectBreakdown()
    {
        var fees = _svc.CalculateFees(1000m);

        Assert.Equal(1000m, fees.GrossAmount);
        Assert.Equal(29.30m, fees.ProcessorFee); // 1000*0.029 + 0.30
        Assert.Equal(0m, fees.PlatformFee);
        Assert.Equal(970.70m, fees.NetAmount);
    }

    [Fact]
    public void CalculateFees_SmallAmount_FixedFeeDominates()
    {
        var fees = _svc.CalculateFees(1m);

        Assert.Equal(1m, fees.GrossAmount);
        Assert.Equal(0.33m, fees.ProcessorFee); // 1*0.029 + 0.30 = 0.329 → 0.33
        Assert.Equal(0m, fees.PlatformFee);
        Assert.Equal(0.67m, fees.NetAmount);
    }

    [Fact]
    public async Task CreatePaymentIntent_DevMode_GeneratesDevPrefixedId()
    {
        var reservation = new Reservation { Status = ReservationStatus.Pending };
        _reservationRepo.GetByIdAsync(reservation.Id).Returns(reservation);
        _paymentRepo.CreateAsync(Arg.Any<PaymentRecord>()).Returns(ci => ci.Arg<PaymentRecord>());
        _reservationRepo.UpdateAsync(Arg.Any<Reservation>()).Returns(ci => ci.Arg<Reservation>());

        var result = await _svc.CreatePaymentIntentAsync(
            new CreatePaymentRequest(reservation.Id, 25m, null));

        Assert.StartsWith("pi_dev_", result.PaymentIntentId);
        Assert.Contains("_secret_dev", result.ClientSecret!);
    }

    [Fact]
    public async Task RecordDoorPayment_SetsProviderToCash()
    {
        var reservation = new Reservation { Status = ReservationStatus.Confirmed };
        _reservationRepo.GetByIdAsync(reservation.Id).Returns(reservation);
        _paymentRepo.CreateAsync(Arg.Any<PaymentRecord>()).Returns(ci => ci.Arg<PaymentRecord>());

        var result = await _svc.RecordDoorPaymentAsync(
            new DoorPaymentRequest(reservation.Id, 0m));

        Assert.True(result.Success);
        Assert.Equal("Cash", result.PaymentRecord!.Provider);
        Assert.Equal(0m, result.PaymentRecord.GrossAmount);
    }

    [Fact]
    public async Task CreatePaymentIntent_SetsProviderToStripe()
    {
        var reservation = new Reservation { Status = ReservationStatus.Pending };
        _reservationRepo.GetByIdAsync(reservation.Id).Returns(reservation);
        _paymentRepo.CreateAsync(Arg.Any<PaymentRecord>()).Returns(ci => ci.Arg<PaymentRecord>());
        _reservationRepo.UpdateAsync(Arg.Any<Reservation>()).Returns(ci => ci.Arg<Reservation>());

        var result = await _svc.CreatePaymentIntentAsync(
            new CreatePaymentRequest(reservation.Id, 50m, "acct_123"));

        Assert.Equal("Stripe", result.PaymentRecord!.Provider);
        Assert.Equal("acct_123", result.PaymentRecord.StripeConnectedAccountId);
    }
}


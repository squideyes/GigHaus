using GigHaus.Domain.Entities;
using GigHaus.Domain.Enums;
using GigHaus.Infrastructure.Repositories;
using GigHaus.Infrastructure.Services;
using NSubstitute;

namespace GigHaus.Tests.Services;

public class ReservationServiceTests
{
    private readonly IReservationRepository _reservationRepo = Substitute.For<IReservationRepository>();
    private readonly IEventRepository _eventRepo = Substitute.For<IEventRepository>();
    private readonly ReservationService _svc;

    public ReservationServiceTests()
    {
        _svc = new ReservationService(_reservationRepo, _eventRepo);
    }

    private Event CreateTestEvent(int maxAttendees = 20, bool isPublished = true) => new()
    {
        Title = "Test Concert", MaxAttendees = maxAttendees, IsPublished = isPublished,
        ArtistId = Guid.NewGuid(), HostId = Guid.NewGuid(),
        EventDate = new DateOnly(2026, 7, 1), StartTime = new TimeOnly(19, 0)
    };

    [Fact]
    public async Task CreateReservation_Success_WhenCapacityAvailable()
    {
        var ev = CreateTestEvent();
        _eventRepo.GetByIdAsync(ev.Id).Returns(ev);
        _reservationRepo.GetConfirmedGuestCountAsync(ev.Id).Returns(5);
        _reservationRepo.CreateAsync(Arg.Any<Reservation>()).Returns(ci => ci.Arg<Reservation>());

        var res = new Reservation { EventId = ev.Id, AttendeeName = "Alice", AttendeeEmail = "a@t.com", NumberOfGuests = 2 };
        var result = await _svc.CreateReservationAsync(res);

        Assert.True(result.Success);
        Assert.Equal(ReservationStatus.Pending, result.Reservation!.Status);
    }

    [Fact]
    public async Task CreateReservation_Fails_WhenAtCapacity()
    {
        var ev = CreateTestEvent(maxAttendees: 10);
        _eventRepo.GetByIdAsync(ev.Id).Returns(ev);
        _reservationRepo.GetConfirmedGuestCountAsync(ev.Id).Returns(9);

        var res = new Reservation { EventId = ev.Id, AttendeeName = "Bob", AttendeeEmail = "b@t.com", NumberOfGuests = 3 };
        var result = await _svc.CreateReservationAsync(res);

        Assert.False(result.Success);
        Assert.Contains("capacity", result.Error!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateReservation_Fails_WhenEventNotPublished()
    {
        var ev = CreateTestEvent(isPublished: false);
        _eventRepo.GetByIdAsync(ev.Id).Returns(ev);

        var res = new Reservation { EventId = ev.Id, AttendeeName = "Charlie", AttendeeEmail = "c@t.com" };
        var result = await _svc.CreateReservationAsync(res);

        Assert.False(result.Success);
        Assert.Contains("not published", result.Error!);
    }

    [Fact]
    public async Task CreateReservation_Fails_WhenEventNotFound()
    {
        _eventRepo.GetByIdAsync(Arg.Any<Guid>()).Returns((Event?)null);

        var res = new Reservation { EventId = Guid.NewGuid(), AttendeeName = "Dan", AttendeeEmail = "d@t.com" };
        var result = await _svc.CreateReservationAsync(res);

        Assert.False(result.Success);
        Assert.Contains("not found", result.Error!);
    }

    [Fact]
    public async Task ConfirmReservation_Success_FromPending()
    {
        var reservation = new Reservation { Status = ReservationStatus.Pending, AttendeeName = "Eve", AttendeeEmail = "e@t.com" };
        _reservationRepo.GetByIdAsync(reservation.Id).Returns(reservation);
        _reservationRepo.UpdateAsync(Arg.Any<Reservation>()).Returns(ci => ci.Arg<Reservation>());

        var result = await _svc.ConfirmReservationAsync(reservation.Id);

        Assert.True(result.Success);
        Assert.Equal(ReservationStatus.Confirmed, result.Reservation!.Status);
        Assert.NotNull(result.Reservation.ConfirmedAt);
    }

    [Fact]
    public async Task ConfirmReservation_Fails_WhenAlreadyConfirmed()
    {
        var reservation = new Reservation { Status = ReservationStatus.Confirmed };
        _reservationRepo.GetByIdAsync(reservation.Id).Returns(reservation);

        var result = await _svc.ConfirmReservationAsync(reservation.Id);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task CancelReservation_Success_WithReason()
    {
        var reservation = new Reservation { Status = ReservationStatus.Confirmed };
        _reservationRepo.GetByIdAsync(reservation.Id).Returns(reservation);
        _reservationRepo.UpdateAsync(Arg.Any<Reservation>()).Returns(ci => ci.Arg<Reservation>());

        var result = await _svc.CancelReservationAsync(reservation.Id, "Schedule conflict");

        Assert.True(result.Success);
        Assert.Equal(ReservationStatus.Cancelled, result.Reservation!.Status);
        Assert.Equal("Schedule conflict", result.Reservation.CancellationReason);
        Assert.NotNull(result.Reservation.CancelledAt);
    }

    [Fact]
    public async Task CancelReservation_Fails_WhenCheckedIn()
    {
        var reservation = new Reservation { Status = ReservationStatus.CheckedIn };
        _reservationRepo.GetByIdAsync(reservation.Id).Returns(reservation);

        var result = await _svc.CancelReservationAsync(reservation.Id);

        Assert.False(result.Success);
        Assert.Contains("checked in", result.Error!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CheckIn_Success_AutoConfirms()
    {
        var reservation = new Reservation { Status = ReservationStatus.Pending };
        _reservationRepo.GetByIdAsync(reservation.Id).Returns(reservation);
        _reservationRepo.UpdateAsync(Arg.Any<Reservation>()).Returns(ci => ci.Arg<Reservation>());

        var result = await _svc.CheckInReservationAsync(reservation.Id);

        Assert.True(result.Success);
        Assert.Equal(ReservationStatus.CheckedIn, result.Reservation!.Status);
        Assert.NotNull(result.Reservation.ConfirmedAt); // auto-confirmed
        Assert.NotNull(result.Reservation.CheckedInAt);
    }

    [Fact]
    public async Task CheckIn_Fails_WhenCancelled()
    {
        var reservation = new Reservation { Status = ReservationStatus.Cancelled };
        _reservationRepo.GetByIdAsync(reservation.Id).Returns(reservation);

        var result = await _svc.CheckInReservationAsync(reservation.Id);

        Assert.False(result.Success);
        Assert.Contains("cancelled", result.Error!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CheckIn_Fails_WhenAlreadyCheckedIn()
    {
        var reservation = new Reservation { Status = ReservationStatus.CheckedIn };
        _reservationRepo.GetByIdAsync(reservation.Id).Returns(reservation);

        var result = await _svc.CheckInReservationAsync(reservation.Id);

        Assert.False(result.Success);
        Assert.Contains("already checked in", result.Error!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CheckIn_FromConfirmed_SetsTimestamp()
    {
        var reservation = new Reservation
        {
            Status = ReservationStatus.Confirmed,
            ConfirmedAt = DateTime.UtcNow.AddMinutes(-30)
        };
        _reservationRepo.GetByIdAsync(reservation.Id).Returns(reservation);
        _reservationRepo.UpdateAsync(Arg.Any<Reservation>()).Returns(ci => ci.Arg<Reservation>());

        var result = await _svc.CheckInReservationAsync(reservation.Id);

        Assert.True(result.Success);
        Assert.Equal(ReservationStatus.CheckedIn, result.Reservation!.Status);
        Assert.NotNull(result.Reservation.CheckedInAt);
        // ConfirmedAt should remain from original confirmation
        Assert.NotNull(result.Reservation.ConfirmedAt);
    }

    [Fact]
    public async Task CancelReservation_Fails_WhenAlreadyCancelled()
    {
        var reservation = new Reservation { Status = ReservationStatus.Cancelled };
        _reservationRepo.GetByIdAsync(reservation.Id).Returns(reservation);

        var result = await _svc.CancelReservationAsync(reservation.Id, "duplicate");

        Assert.False(result.Success);
        Assert.Contains("already cancelled", result.Error!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CancelReservation_FromPending_Succeeds()
    {
        var reservation = new Reservation { Status = ReservationStatus.Pending };
        _reservationRepo.GetByIdAsync(reservation.Id).Returns(reservation);
        _reservationRepo.UpdateAsync(Arg.Any<Reservation>()).Returns(ci => ci.Arg<Reservation>());

        var result = await _svc.CancelReservationAsync(reservation.Id);

        Assert.True(result.Success);
        Assert.Equal(ReservationStatus.Cancelled, result.Reservation!.Status);
        Assert.NotNull(result.Reservation.CancelledAt);
    }

    [Fact]
    public async Task ConfirmReservation_Fails_WhenNotFound()
    {
        _reservationRepo.GetByIdAsync(Arg.Any<Guid>()).Returns((Reservation?)null);

        var result = await _svc.ConfirmReservationAsync(Guid.NewGuid());

        Assert.False(result.Success);
        Assert.Contains("not found", result.Error!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CancelReservation_Fails_WhenNotFound()
    {
        _reservationRepo.GetByIdAsync(Arg.Any<Guid>()).Returns((Reservation?)null);

        var result = await _svc.CancelReservationAsync(Guid.NewGuid());

        Assert.False(result.Success);
        Assert.Contains("not found", result.Error!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CheckIn_Fails_WhenNotFound()
    {
        _reservationRepo.GetByIdAsync(Arg.Any<Guid>()).Returns((Reservation?)null);

        var result = await _svc.CheckInReservationAsync(Guid.NewGuid());

        Assert.False(result.Success);
        Assert.Contains("not found", result.Error!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateReservation_ExactCapacity_Succeeds()
    {
        var ev = CreateTestEvent(maxAttendees: 10);
        _eventRepo.GetByIdAsync(ev.Id).Returns(ev);
        _reservationRepo.GetConfirmedGuestCountAsync(ev.Id).Returns(8);
        _reservationRepo.CreateAsync(Arg.Any<Reservation>()).Returns(ci => ci.Arg<Reservation>());

        var res = new Reservation { EventId = ev.Id, AttendeeName = "Eve", AttendeeEmail = "e@t.com", NumberOfGuests = 2 };
        var result = await _svc.CreateReservationAsync(res);

        Assert.True(result.Success);
    }

    [Fact]
    public async Task CreateReservation_OneOverCapacity_Fails()
    {
        var ev = CreateTestEvent(maxAttendees: 10);
        _eventRepo.GetByIdAsync(ev.Id).Returns(ev);
        _reservationRepo.GetConfirmedGuestCountAsync(ev.Id).Returns(10);

        var res = new Reservation { EventId = ev.Id, AttendeeName = "F", AttendeeEmail = "f@t.com", NumberOfGuests = 1 };
        var result = await _svc.CreateReservationAsync(res);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task CreateReservation_DoorOnly_NoCashAtDoor_Fails()
    {
        var ev = CreateTestEvent();
        ev.PricingMode = PricingMode.DoorOnly;
        ev.AllowCashAtDoor = false;
        _eventRepo.GetByIdAsync(ev.Id).Returns(ev);
        _reservationRepo.GetConfirmedGuestCountAsync(ev.Id).Returns(0);

        var res = new Reservation { EventId = ev.Id, AttendeeName = "G", AttendeeEmail = "g@t.com" };
        var result = await _svc.CreateReservationAsync(res);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task ConfirmReservation_Fails_WhenCancelled()
    {
        var reservation = new Reservation { Status = ReservationStatus.Cancelled };
        _reservationRepo.GetByIdAsync(reservation.Id).Returns(reservation);

        var result = await _svc.ConfirmReservationAsync(reservation.Id);

        Assert.False(result.Success);
    }
}


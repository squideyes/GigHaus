using GigHaus.Domain.Entities;
using GigHaus.Domain.Enums;
using GigHaus.Infrastructure.Repositories;

namespace GigHaus.Infrastructure.Services;

/// <summary>
/// Orchestrates the reservation lifecycle: create → confirm → check-in → cancel.
/// Enforces capacity limits and PricingMode rules.
/// </summary>
public class ReservationService : IReservationService
{
    private readonly IReservationRepository _reservations;
    private readonly IEventRepository _events;

    public ReservationService(IReservationRepository reservations, IEventRepository events)
    {
        _reservations = reservations;
        _events = events;
    }

    public async Task<ReservationResult> CreateReservationAsync(Reservation reservation, CancellationToken ct = default)
    {
        var ev = await _events.GetByIdAsync(reservation.EventId, ct);
        if (ev is null)
            return ReservationResult.Fail("Event not found.");

        if (!ev.IsPublished)
            return ReservationResult.Fail("Event is not published yet.");

        // Capacity check
        var currentGuests = await _reservations.GetConfirmedGuestCountAsync(ev.Id, ct);
        if (currentGuests + reservation.NumberOfGuests > ev.MaxAttendees)
            return ReservationResult.Fail($"Not enough capacity. {ev.MaxAttendees - currentGuests} spot(s) remaining.");

        // PricingMode validation
        if (ev.PricingMode == PricingMode.DoorOnly && !ev.AllowCashAtDoor)
            return ReservationResult.Fail("This event only accepts door payments but cash at door is not enabled.");

        reservation.Status = ReservationStatus.Pending;
        var created = await _reservations.CreateAsync(reservation, ct);
        return ReservationResult.Ok(created);
    }

    public async Task<ReservationResult> ConfirmReservationAsync(Guid reservationId, CancellationToken ct = default)
    {
        var reservation = await _reservations.GetByIdAsync(reservationId, ct);
        if (reservation is null)
            return ReservationResult.Fail("Reservation not found.");

        if (reservation.Status != ReservationStatus.Pending)
            return ReservationResult.Fail($"Cannot confirm a reservation with status '{reservation.Status}'.");

        reservation.Status = ReservationStatus.Confirmed;
        reservation.ConfirmedAt = DateTime.UtcNow;
        await _reservations.UpdateAsync(reservation, ct);
        return ReservationResult.Ok(reservation);
    }

    public async Task<ReservationResult> CancelReservationAsync(Guid reservationId, string? reason = null, CancellationToken ct = default)
    {
        var reservation = await _reservations.GetByIdAsync(reservationId, ct);
        if (reservation is null)
            return ReservationResult.Fail("Reservation not found.");

        if (reservation.Status == ReservationStatus.Cancelled)
            return ReservationResult.Fail("Reservation is already cancelled.");

        if (reservation.Status == ReservationStatus.CheckedIn)
            return ReservationResult.Fail("Cannot cancel a reservation that has already been checked in.");

        reservation.Status = ReservationStatus.Cancelled;
        reservation.CancelledAt = DateTime.UtcNow;
        reservation.CancellationReason = reason;
        await _reservations.UpdateAsync(reservation, ct);
        return ReservationResult.Ok(reservation);
    }

    public async Task<ReservationResult> CheckInReservationAsync(Guid reservationId, CancellationToken ct = default)
    {
        var reservation = await _reservations.GetByIdAsync(reservationId, ct);
        if (reservation is null)
            return ReservationResult.Fail("Reservation not found.");

        if (reservation.Status == ReservationStatus.Cancelled)
            return ReservationResult.Fail("Cannot check in a cancelled reservation.");

        if (reservation.Status == ReservationStatus.CheckedIn)
            return ReservationResult.Fail("Reservation is already checked in.");

        reservation.Status = ReservationStatus.CheckedIn;
        reservation.CheckedInAt = DateTime.UtcNow;
        if (reservation.ConfirmedAt is null)
            reservation.ConfirmedAt = DateTime.UtcNow; // auto-confirm on check-in
        await _reservations.UpdateAsync(reservation, ct);
        return ReservationResult.Ok(reservation);
    }
}


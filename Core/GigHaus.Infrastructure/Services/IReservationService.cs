using GigHaus.Domain.Entities;

namespace GigHaus.Infrastructure.Services;

public interface IReservationService
{
    Task<ReservationResult> CreateReservationAsync(Reservation reservation, CancellationToken ct = default);
    Task<ReservationResult> ConfirmReservationAsync(Guid reservationId, CancellationToken ct = default);
    Task<ReservationResult> CancelReservationAsync(Guid reservationId, string? reason = null, CancellationToken ct = default);
    Task<ReservationResult> CheckInReservationAsync(Guid reservationId, CancellationToken ct = default);
}

/// <summary>
/// Result wrapper for reservation operations.
/// </summary>
public class ReservationResult
{
    public bool Success { get; init; }
    public string? Error { get; init; }
    public Reservation? Reservation { get; init; }

    public static ReservationResult Ok(Reservation reservation) => new() { Success = true, Reservation = reservation };
    public static ReservationResult Fail(string error) => new() { Success = false, Error = error };
}


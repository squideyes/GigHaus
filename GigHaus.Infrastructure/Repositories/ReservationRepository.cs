using Dapper;
using GigHaus.Domain.Entities;
using GigHaus.Infrastructure.Data;

namespace GigHaus.Infrastructure.Repositories;

public class ReservationRepository : IReservationRepository
{
    private readonly IDbConnectionFactory _db;

    public ReservationRepository(IDbConnectionFactory db) => _db = db;

    public async Task<IEnumerable<Reservation>> GetByEventIdAsync(Guid eventId, CancellationToken ct = default)
    {
        using var conn = await _db.CreateConnectionAsync(ct);
        return await conn.QueryAsync<Reservation>(
            @"SELECT * FROM reservations WHERE event_id = @EventId ORDER BY created_at",
            new { EventId = eventId });
    }

    public async Task<Reservation?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        using var conn = await _db.CreateConnectionAsync(ct);
        return await conn.QuerySingleOrDefaultAsync<Reservation>(
            @"SELECT * FROM reservations WHERE id = @Id", new { Id = id });
    }

    public async Task<int> GetConfirmedGuestCountAsync(Guid eventId, CancellationToken ct = default)
    {
        using var conn = await _db.CreateConnectionAsync(ct);
        return await conn.ExecuteScalarAsync<int>(
            @"SELECT COALESCE(SUM(number_of_guests), 0) FROM reservations
              WHERE event_id = @EventId AND status IN ('Pending', 'Confirmed', 'CheckedIn')",
            new { EventId = eventId });
    }

    public async Task<Reservation> CreateAsync(Reservation reservation, CancellationToken ct = default)
    {
        using var conn = await _db.CreateConnectionAsync(ct);
        await conn.ExecuteAsync(
            @"INSERT INTO reservations (id, event_id, attendee_name, attendee_email, attendee_phone,
              number_of_guests, status, note, confirmed_at, cancelled_at, checked_in_at,
              cancellation_reason, created_at, updated_at)
              VALUES (@Id, @EventId, @AttendeeName, @AttendeeEmail, @AttendeePhone,
              @NumberOfGuests, @Status, @Note, @ConfirmedAt, @CancelledAt, @CheckedInAt,
              @CancellationReason, @CreatedAt, @UpdatedAt)",
            new
            {
                reservation.Id, reservation.EventId, reservation.AttendeeName,
                reservation.AttendeeEmail, reservation.AttendeePhone,
                reservation.NumberOfGuests, Status = reservation.Status.ToString(),
                reservation.Note, reservation.ConfirmedAt, reservation.CancelledAt,
                reservation.CheckedInAt, reservation.CancellationReason,
                reservation.CreatedAt, reservation.UpdatedAt
            });
        return reservation;
    }

    public async Task<Reservation?> UpdateAsync(Reservation reservation, CancellationToken ct = default)
    {
        using var conn = await _db.CreateConnectionAsync(ct);
        reservation.UpdatedAt = DateTime.UtcNow;
        var rows = await conn.ExecuteAsync(
            @"UPDATE reservations SET status = @Status, note = @Note,
              confirmed_at = @ConfirmedAt, cancelled_at = @CancelledAt,
              checked_in_at = @CheckedInAt, cancellation_reason = @CancellationReason,
              updated_at = @UpdatedAt
              WHERE id = @Id",
            new
            {
                reservation.Id, Status = reservation.Status.ToString(),
                reservation.Note, reservation.ConfirmedAt, reservation.CancelledAt,
                reservation.CheckedInAt, reservation.CancellationReason,
                reservation.UpdatedAt
            });
        return rows > 0 ? reservation : null;
    }
}


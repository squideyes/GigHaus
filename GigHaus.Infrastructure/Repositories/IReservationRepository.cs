using GigHaus.Domain.Entities;

namespace GigHaus.Infrastructure.Repositories;

public interface IReservationRepository
{
    Task<IEnumerable<Reservation>> GetByEventIdAsync(Guid eventId, CancellationToken ct = default);
    Task<Reservation?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<int> GetConfirmedGuestCountAsync(Guid eventId, CancellationToken ct = default);
    Task<Reservation> CreateAsync(Reservation reservation, CancellationToken ct = default);
    Task<Reservation?> UpdateAsync(Reservation reservation, CancellationToken ct = default);
}


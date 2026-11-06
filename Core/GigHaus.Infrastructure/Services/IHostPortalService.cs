using GigHaus.Domain.Entities;

namespace GigHaus.Infrastructure.Services;

/// <summary>
/// Everything a signed-in host can do. Every method takes the host's ID and
/// refuses to touch shows or guests that belong to another host.
/// </summary>
public interface IHostPortalService
{
    Task<Host?> GetVenueAsync(Guid hostId, CancellationToken ct = default);
    Task<Result<Host>> UpdateVenueAsync(Guid hostId, Host changes, CancellationToken ct = default);

    Task<IReadOnlyList<Event>> GetEventsAsync(Guid hostId, CancellationToken ct = default);
    Task<Event?> GetEventAsync(Guid hostId, Guid eventId, CancellationToken ct = default);
    Task<Result<Event>> SaveEventAsync(Guid hostId, Event ev, CancellationToken ct = default);
    Task<Result<Event>> SetPublishedAsync(Guid hostId, Guid eventId, bool isPublished, CancellationToken ct = default);

    /// <summary>Finds the artist with this email, or adds them.</summary>
    Task<Result<Artist>> FindOrAddArtistAsync(string name, string email, string? website, CancellationToken ct = default);

    Task<IReadOnlyList<Reservation>> GetGuestsAsync(Guid hostId, Guid eventId, CancellationToken ct = default);
    Task<IReadOnlyList<Reservation>> GetAllGuestsAsync(Guid hostId, CancellationToken ct = default);
    Task<Result<Reservation>> ConfirmGuestAsync(Guid hostId, Guid reservationId, CancellationToken ct = default);
    Task<Result<Reservation>> CheckInGuestAsync(Guid hostId, Guid reservationId, CancellationToken ct = default);
    Task<Result<Reservation>> CancelGuestAsync(Guid hostId, Guid reservationId, string? reason, CancellationToken ct = default);
}

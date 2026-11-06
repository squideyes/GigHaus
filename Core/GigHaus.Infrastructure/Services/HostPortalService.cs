using System.Net;
using GigHaus.Domain.Entities;
using GigHaus.Domain.Enums;
using GigHaus.Infrastructure.Email;
using GigHaus.Infrastructure.Repositories;
using Microsoft.Extensions.Logging;

namespace GigHaus.Infrastructure.Services;

public sealed class HostPortalService(
    IHostRepository hosts,
    IEventRepository events,
    IArtistRepository artists,
    IReservationRepository reservations,
    IReservationService reservationService,
    IEmailService email,
    TimeProvider clock,
    ILogger<HostPortalService> logger) : IHostPortalService
{
    private const string ShowNotFound = "Show not found.";
    private const string GuestNotFound = "Guest not found.";

    public Task<Host?> GetVenueAsync(Guid hostId, CancellationToken ct = default) =>
        hosts.GetByIdAsync(hostId, ct);

    public async Task<Result<Host>> UpdateVenueAsync(Guid hostId, Host changes, CancellationToken ct = default)
    {
        var host = await hosts.GetByIdAsync(hostId, ct).ConfigureAwait(false);
        if (host is null)
        {
            return Result<Host>.Fail("Venue not found.");
        }

        if (changes.MaxCapacity < 1)
        {
            return Result<Host>.Fail("Capacity must be at least 1.");
        }

        host.Name = changes.Name;
        host.Phone = changes.Phone;
        host.Address = changes.Address;
        host.City = changes.City;
        host.State = changes.State;
        host.ZipCode = changes.ZipCode;
        host.MaxCapacity = changes.MaxCapacity;
        host.VenueDescription = changes.VenueDescription;

        await hosts.UpdateAsync(host, ct).ConfigureAwait(false);
        return Result<Host>.Ok(host);
    }

    public async Task<IReadOnlyList<Event>> GetEventsAsync(Guid hostId, CancellationToken ct = default) =>
        [.. await events.GetByHostIdAsync(hostId, ct).ConfigureAwait(false)];

    public async Task<Event?> GetEventAsync(Guid hostId, Guid eventId, CancellationToken ct = default)
    {
        var ev = await events.GetByIdAsync(eventId, ct).ConfigureAwait(false);
        return ev?.HostId == hostId ? ev : null;
    }

    public async Task<Result<Event>> SaveEventAsync(Guid hostId, Event ev, CancellationToken ct = default)
    {
        var host = await hosts.GetByIdAsync(hostId, ct).ConfigureAwait(false);
        if (host is null)
        {
            return Result<Event>.Fail("Venue not found.");
        }

        var stored = await events.GetByIdAsync(ev.Id, ct).ConfigureAwait(false);
        if (stored is not null && stored.HostId != hostId)
        {
            return Result<Event>.Fail(ShowNotFound);
        }

        var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
        if (stored is null && ev.EventDate < today)
        {
            return Result<Event>.Fail("Pick a date that hasn't passed yet.");
        }

        if (ev.MaxAttendees < 1 || ev.MaxAttendees > host.MaxCapacity)
        {
            return Result<Event>.Fail($"Guest limit must be between 1 and your venue's capacity of {host.MaxCapacity}.");
        }

        if (ev.EndTime is { } end && end <= ev.StartTime)
        {
            return Result<Event>.Fail("The end time must be after the start time.");
        }

        if (ev.SuggestedDonationAmount < 0)
        {
            return Result<Event>.Fail("The suggested donation can't be negative.");
        }

        if (ev.PricingMode == PricingMode.MinimumRequired)
        {
            if (ev.MinimumContributionAmount is not > 0)
            {
                return Result<Event>.Fail("Enter the minimum contribution guests must make to attend.");
            }

            if (ev.SuggestedDonationAmount < ev.MinimumContributionAmount)
            {
                return Result<Event>.Fail("The suggested donation can't be less than the minimum.");
            }
        }
        else
        {
            ev.MinimumContributionAmount = null;
        }

        ev.HostId = hostId;

        // Contributions are collected at the door until online payments launch.
        ev.AllowCashAtDoor = true;
        ev.AllowOnlinePayment = false;
        ev.AllowReserveNowPayLater = true;
        ev.AllowPayWhatYouCan = ev.PricingMode != PricingMode.MinimumRequired;

        if (stored is null)
        {
            ev.IsPublished = false;
            var created = await events.CreateAsync(ev, ct).ConfigureAwait(false);
            return Result<Event>.Ok(created);
        }

        var reserved = await reservations.GetConfirmedGuestCountAsync(ev.Id, ct).ConfigureAwait(false);
        if (ev.MaxAttendees < reserved)
        {
            return Result<Event>.Fail($"{reserved} guests have already reserved, so the limit can't go below that.");
        }

        ev.IsPublished = stored.IsPublished;
        ev.CreatedAt = stored.CreatedAt;
        ev.SuggestedDonationLabel = stored.SuggestedDonationLabel;
        ev.PricingDisclosureText = stored.PricingDisclosureText;

        var updated = await events.UpdateAsync(ev, ct).ConfigureAwait(false);
        return updated is null ? Result<Event>.Fail(ShowNotFound) : Result<Event>.Ok(updated);
    }

    public async Task<Result<Event>> SetPublishedAsync(Guid hostId, Guid eventId, bool isPublished, CancellationToken ct = default)
    {
        var ev = await GetEventAsync(hostId, eventId, ct).ConfigureAwait(false);
        if (ev is null)
        {
            return Result<Event>.Fail(ShowNotFound);
        }

        if (isPublished && ev.Host.Status != HostStatus.Approved)
        {
            return Result<Event>.Fail("You can publish shows once GigHaus staff have approved your venue.");
        }

        ev.IsPublished = isPublished;
        await events.UpdateAsync(ev, ct).ConfigureAwait(false);
        return Result<Event>.Ok(ev);
    }

    public async Task<Result<Artist>> FindOrAddArtistAsync(string name, string email, string? website, CancellationToken ct = default)
    {
        var existing = await artists.GetByEmailAsync(email, ct).ConfigureAwait(false);
        if (existing is not null)
        {
            return Result<Artist>.Ok(existing);
        }

        var artist = new Artist { Name = name.Trim(), Email = email.Trim(), Website = website?.Trim() };
        return Result<Artist>.Ok(await artists.CreateAsync(artist, ct).ConfigureAwait(false));
    }

    public async Task<IReadOnlyList<Reservation>> GetGuestsAsync(Guid hostId, Guid eventId, CancellationToken ct = default)
    {
        var ev = await GetEventAsync(hostId, eventId, ct).ConfigureAwait(false);
        return ev is null
            ? []
            : [.. await reservations.GetByEventIdAsync(eventId, ct).ConfigureAwait(false)];
    }

    public async Task<IReadOnlyList<Reservation>> GetAllGuestsAsync(Guid hostId, CancellationToken ct = default) =>
        [.. await reservations.GetByHostIdAsync(hostId, ct).ConfigureAwait(false)];

    public async Task<Result<Reservation>> ConfirmGuestAsync(Guid hostId, Guid reservationId, CancellationToken ct = default)
    {
        var ev = await GetEventForReservationAsync(hostId, reservationId, ct).ConfigureAwait(false);
        if (ev is null)
        {
            return Result<Reservation>.Fail(GuestNotFound);
        }

        var result = ToResult(await reservationService.ConfirmReservationAsync(reservationId, ct).ConfigureAwait(false));
        if (result.Success)
        {
            await SendConfirmationAsync(ev, result.Value!, ct).ConfigureAwait(false);
        }

        return result;
    }

    public async Task<Result<Reservation>> CheckInGuestAsync(Guid hostId, Guid reservationId, CancellationToken ct = default)
    {
        var ev = await GetEventForReservationAsync(hostId, reservationId, ct).ConfigureAwait(false);
        return ev is null
            ? Result<Reservation>.Fail(GuestNotFound)
            : ToResult(await reservationService.CheckInReservationAsync(reservationId, ct).ConfigureAwait(false));
    }

    public async Task<Result<Reservation>> CancelGuestAsync(Guid hostId, Guid reservationId, string? reason, CancellationToken ct = default)
    {
        var ev = await GetEventForReservationAsync(hostId, reservationId, ct).ConfigureAwait(false);
        return ev is null
            ? Result<Reservation>.Fail(GuestNotFound)
            : ToResult(await reservationService.CancelReservationAsync(reservationId, reason, ct).ConfigureAwait(false));
    }

    private async Task<Event?> GetEventForReservationAsync(Guid hostId, Guid reservationId, CancellationToken ct)
    {
        var reservation = await reservations.GetByIdAsync(reservationId, ct).ConfigureAwait(false);
        return reservation is null
            ? null
            : await GetEventAsync(hostId, reservation.EventId, ct).ConfigureAwait(false);
    }

    private static Result<Reservation> ToResult(ReservationResult result) =>
        result.Success
            ? Result<Reservation>.Ok(result.Reservation!)
            : Result<Reservation>.Fail(result.Error ?? "That didn't work. Please try again.");

    // The venue's street address is only ever sent to guests the host has confirmed.
    private async Task SendConfirmationAsync(Event ev, Reservation guest, CancellationToken ct)
    {
        var address = $"{ev.Host.Address}, {ev.Host.City}, {ev.Host.State} {ev.Host.ZipCode}";
        var html =
            $"""
            <p>Hi {Encode(guest.AttendeeName)},</p>
            <p>You're confirmed for <strong>{Encode(ev.Title)}</strong> with {Encode(ev.Artist.Name)}.</p>
            <p><strong>When:</strong> {ev.EventDate:dddd, MMMM d, yyyy} at {ev.StartTime:h:mm tt}<br>
            <strong>Where:</strong> {Encode(address)}<br>
            <strong>Guests:</strong> {guest.NumberOfGuests}</p>
            <p>This is a private home, so please keep the address to yourself.</p>
            <p>{Encode(ev.PricingDisclosureText)}</p>
            """;

        try
        {
            await email.SendEmailAsync(
                new EmailMessage(guest.AttendeeEmail, $"You're confirmed: {ev.Title}", html), ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Could not send confirmation email for reservation {ReservationId}", guest.Id);
        }
    }

    private static string Encode(string value) => WebUtility.HtmlEncode(value);
}

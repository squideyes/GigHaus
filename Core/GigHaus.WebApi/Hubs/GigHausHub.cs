using Microsoft.AspNetCore.SignalR;

namespace GigHaus.WebApi.Hubs;

/// <summary>
/// SignalR hub for real-time GigHaus updates.
/// Clients join an event-specific group to receive capacity, check-in, and reservation updates.
/// </summary>
public class GigHausHub : Hub<IGigHausClient>
{
    /// <summary>
    /// Client joins a group for a specific event to receive real-time updates.
    /// </summary>
    public Task JoinEventGroup(Guid eventId)
        => Groups.AddToGroupAsync(Context.ConnectionId, EventGroup(eventId));

    /// <summary>
    /// Client leaves an event group.
    /// </summary>
    public Task LeaveEventGroup(Guid eventId)
        => Groups.RemoveFromGroupAsync(Context.ConnectionId, EventGroup(eventId));

    public static string EventGroup(Guid eventId) => $"event-{eventId}";
}

/// <summary>
/// Strongly-typed client interface for type-safe hub invocations.
/// </summary>
public interface IGigHausClient
{
    /// <summary>Reservation created/confirmed/cancelled — capacity changed.</summary>
    Task CapacityUpdated(CapacityUpdate update);

    /// <summary>Attendee checked in.</summary>
    Task CheckInUpdated(CheckInUpdate update);

    /// <summary>Payment received for an event.</summary>
    Task PaymentReceived(PaymentUpdate update);
}

public record CapacityUpdate(Guid EventId, int CurrentGuests, int MaxAttendees, string Action);
public record CheckInUpdate(Guid EventId, Guid ReservationId, string AttendeeName, int TotalCheckedIn);
public record PaymentUpdate(Guid EventId, Guid PaymentId, decimal GrossAmount, string Provider);


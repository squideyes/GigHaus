namespace GigHaus.Infrastructure.Messaging;

public interface IEventBus
{
    Task PublishAsync<T>(string exchange, string routingKey, T message, CancellationToken ct = default) where T : class;
}

// ── Event messages ────────────────────────────────────────────

public record ReservationCreatedEvent(
    Guid ReservationId,
    Guid EventId,
    string AttendeeName,
    string AttendeeEmail,
    int NumberOfGuests,
    DateTime OccurredAt
);

public record ReservationConfirmedEvent(
    Guid ReservationId,
    Guid EventId,
    string AttendeeName,
    string AttendeeEmail,
    DateTime ConfirmedAt
);

public record ReservationCancelledEvent(
    Guid ReservationId,
    Guid EventId,
    string AttendeeName,
    string AttendeeEmail,
    string? Reason,
    DateTime CancelledAt
);

public record ReservationCheckedInEvent(
    Guid ReservationId,
    Guid EventId,
    string AttendeeName,
    string AttendeeEmail,
    DateTime CheckedInAt
);

public record PaymentReceivedEvent(
    Guid PaymentId,
    Guid ReservationId,
    decimal GrossAmount,
    decimal NetAmount,
    string Provider,
    DateTime OccurredAt
);


using GigHaus.Domain.Enums;

namespace GigHaus.Domain.Entities;

public class Reservation : BaseEntity
{
    public Guid EventId { get; set; }
    public Event Event { get; set; } = null!;

    public string AttendeeName { get; set; } = string.Empty;
    public string AttendeeEmail { get; set; } = string.Empty;
    public string? AttendeePhone { get; set; }
    public int NumberOfGuests { get; set; } = 1;

    public ReservationStatus Status { get; set; } = ReservationStatus.Pending;

    /// <summary>Optional note from the attendee.</summary>
    public string? Note { get; set; }

    // Workflow timestamps
    public DateTime? ConfirmedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public DateTime? CheckedInAt { get; set; }

    /// <summary>Reason for cancellation, if applicable.</summary>
    public string? CancellationReason { get; set; }

    // Navigation
    public ICollection<PaymentRecord> Payments { get; set; } = [];
}


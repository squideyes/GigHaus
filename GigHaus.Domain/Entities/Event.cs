using GigHaus.Domain.Enums;

namespace GigHaus.Domain.Entities;

public class Event : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }

    // Relationships
    public Guid ArtistId { get; set; }
    public Artist Artist { get; set; } = null!;
    public Guid HostId { get; set; }
    public Host Host { get; set; } = null!;

    // Schedule
    public DateOnly EventDate { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }

    // Capacity
    public int MaxAttendees { get; set; }

    // Pricing / Contribution policy  (from legal doc)
    public PricingMode PricingMode { get; set; } = PricingMode.SuggestedOnly;
    public decimal SuggestedDonationAmount { get; set; }
    public decimal? MinimumContributionAmount { get; set; }
    public bool AllowPayWhatYouCan { get; set; } = true;
    public bool AllowReserveNowPayLater { get; set; }
    public bool AllowCashAtDoor { get; set; } = true;
    public bool AllowOnlinePayment { get; set; } = true;

    /// <summary>Label shown to attendees, e.g. "Suggested donation to support the artist".</summary>
    public string SuggestedDonationLabel { get; set; } = "Suggested donation to support the artist";

    /// <summary>Legal disclosure shown near payment / reservation.</summary>
    public string PricingDisclosureText { get; set; } =
        "Payments support the artist and event hosting. These payments are not charitable donations and are not tax-deductible unless explicitly stated otherwise.";

    public bool IsPublished { get; set; }

    // Navigation
    public ICollection<Reservation> Reservations { get; set; } = [];
}


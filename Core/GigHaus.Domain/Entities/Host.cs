using GigHaus.Domain.Enums;

namespace GigHaus.Domain.Entities;

public class Host : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }

    // Venue details
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public int MaxCapacity { get; set; }

    /// <summary>
    /// Brief description of the venue / hosting space.
    /// </summary>
    public string? VenueDescription { get; set; }

    /// <summary>Review state. Only approved hosts can publish shows.</summary>
    public HostStatus Status { get; set; } = HostStatus.Pending;
    public DateTime? ApprovedAt { get; set; }

    // Navigation
    public ICollection<Event> Events { get; set; } = [];
}


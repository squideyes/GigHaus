using GigHaus.Domain.Enums;

namespace GigHaus.Domain.Entities;

/// <summary>
/// A prospective host who got in touch through the contact form.
/// </summary>
public class Lead : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public int? EstimatedCapacity { get; set; }
    public string? Message { get; set; }
    public LeadStatus Status { get; set; } = LeadStatus.New;

    /// <summary>Private notes for GigHaus staff.</summary>
    public string? StaffNotes { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}

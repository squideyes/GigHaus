using System.ComponentModel.DataAnnotations;

namespace GigHaus.Web.Models;

public sealed class ContactForm
{
    [Required(ErrorMessage = "Please tell us your name.")]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "We need an email so we can reply.")]
    [EmailAddress(ErrorMessage = "That doesn't look like an email address.")]
    [StringLength(200)]
    public string Email { get; set; } = string.Empty;

    [StringLength(50)]
    public string? Phone { get; set; }

    [StringLength(100)]
    public string? City { get; set; }

    [StringLength(50)]
    public string? State { get; set; }

    [Range(1, 1000, ErrorMessage = "Enter a number between 1 and 1000.")]
    public int? EstimatedCapacity { get; set; }

    [StringLength(4000)]
    public string? Message { get; set; }

    /// <summary>Hidden from people. Only bots fill it in.</summary>
    public string? Website { get; set; }
}

public sealed class ReservationForm
{
    [Required(ErrorMessage = "Please tell us your name.")]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "We need an email to send you the details.")]
    [EmailAddress(ErrorMessage = "That doesn't look like an email address.")]
    [StringLength(200)]
    public string Email { get; set; } = string.Empty;

    [StringLength(50)]
    public string? Phone { get; set; }

    [Range(1, 20, ErrorMessage = "Reserve between 1 and 20 spots.")]
    public int NumberOfGuests { get; set; } = 1;

    [StringLength(1000)]
    public string? Note { get; set; }

    /// <summary>Hidden from people. Only bots fill it in.</summary>
    public string? Website { get; set; }
}

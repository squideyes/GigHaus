using System.ComponentModel.DataAnnotations;
using GigHaus.Domain.Entities;
using GigHaus.Domain.Enums;

namespace GigHaus.Web.Models;

public sealed class EventForm
{
    [Required(ErrorMessage = "Give the show a title.")]
    [StringLength(300)]
    public string Title { get; set; } = string.Empty;

    [StringLength(4000)]
    public string? Description { get; set; }

    public Guid? ArtistId { get; set; }

    [StringLength(200)]
    public string? NewArtistName { get; set; }

    [EmailAddress(ErrorMessage = "That doesn't look like an email address.")]
    [StringLength(200)]
    public string? NewArtistEmail { get; set; }

    [Required(ErrorMessage = "Pick a date.")]
    public DateOnly? EventDate { get; set; }

    [Required(ErrorMessage = "Pick a start time.")]
    public TimeOnly? StartTime { get; set; } = new(19, 0);

    public TimeOnly? EndTime { get; set; }

    [Range(1, 1000, ErrorMessage = "Enter a guest limit from 1 to 1000.")]
    public int MaxAttendees { get; set; } = 20;

    public PricingMode PricingMode { get; set; } = PricingMode.SuggestedOnly;

    [Range(typeof(decimal), "0", "10000", ErrorMessage = "Enter an amount from $0 to $10,000.")]
    public decimal SuggestedDonationAmount { get; set; } = 20;

    [Range(typeof(decimal), "0", "10000", ErrorMessage = "Enter an amount from $0 to $10,000.")]
    public decimal? MinimumContributionAmount { get; set; }

    public static EventForm From(Event ev) => new()
    {
        Title = ev.Title,
        Description = ev.Description,
        ArtistId = ev.ArtistId,
        EventDate = ev.EventDate,
        StartTime = ev.StartTime,
        EndTime = ev.EndTime,
        MaxAttendees = ev.MaxAttendees,
        PricingMode = ev.PricingMode,
        SuggestedDonationAmount = ev.SuggestedDonationAmount,
        MinimumContributionAmount = ev.MinimumContributionAmount
    };

    public Event ToEvent(Guid id, Guid artistId) => new()
    {
        Id = id,
        Title = Title.Trim(),
        Description = Description?.Trim(),
        ArtistId = artistId,
        EventDate = EventDate ?? default,
        StartTime = StartTime ?? default,
        EndTime = EndTime,
        MaxAttendees = MaxAttendees,
        PricingMode = PricingMode,
        SuggestedDonationAmount = SuggestedDonationAmount,
        MinimumContributionAmount = MinimumContributionAmount
    };
}

using GigHaus.Domain.Entities;
using GigHaus.Domain.Enums;

namespace GigHaus.WebApi;

/// <summary>
/// What anyone can see about a show. Hosts are private homes, so this carries the
/// city but never the street address or anyone's contact details.
/// </summary>
public sealed record PublicEvent
{
    public required Guid Id { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public required string ArtistName { get; init; }
    public required string City { get; init; }
    public required string State { get; init; }
    public required DateOnly EventDate { get; init; }
    public required TimeOnly StartTime { get; init; }
    public TimeOnly? EndTime { get; init; }
    public required int MaxAttendees { get; init; }
    public required PricingMode PricingMode { get; init; }
    public required decimal SuggestedDonationAmount { get; init; }
    public decimal? MinimumContributionAmount { get; init; }
    public required string SuggestedDonationLabel { get; init; }
    public required string PricingDisclosureText { get; init; }

    public static bool IsVisible(Event ev) => ev.IsPublished && ev.Host.Status == HostStatus.Approved;

    public static PublicEvent From(Event ev) => new()
    {
        Id = ev.Id,
        Title = ev.Title,
        Description = ev.Description,
        ArtistName = ev.Artist.Name,
        City = ev.Host.City,
        State = ev.Host.State,
        EventDate = ev.EventDate,
        StartTime = ev.StartTime,
        EndTime = ev.EndTime,
        MaxAttendees = ev.MaxAttendees,
        PricingMode = ev.PricingMode,
        SuggestedDonationAmount = ev.SuggestedDonationAmount,
        MinimumContributionAmount = ev.MinimumContributionAmount,
        SuggestedDonationLabel = ev.SuggestedDonationLabel,
        PricingDisclosureText = ev.PricingDisclosureText
    };
}

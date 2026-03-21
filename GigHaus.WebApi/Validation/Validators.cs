using FluentValidation;
using GigHaus.Domain.Entities;
using GigHaus.Infrastructure.Services;
using HostEntity = GigHaus.Domain.Entities.Host;

namespace GigHaus.WebApi.Validation;

public class ArtistValidator : AbstractValidator<Artist>
{
    public ArtistValidator()
    {
        RuleFor(a => a.Name).NotEmpty().MaximumLength(200);
        RuleFor(a => a.Email).NotEmpty().EmailAddress().MaximumLength(200);
        RuleFor(a => a.Phone).MaximumLength(50);
        RuleFor(a => a.Website).MaximumLength(500);
    }
}

public class HostValidator : AbstractValidator<HostEntity>
{
    public HostValidator()
    {
        RuleFor(h => h.Name).NotEmpty().MaximumLength(200);
        RuleFor(h => h.Email).NotEmpty().EmailAddress().MaximumLength(200);
        RuleFor(h => h.Phone).MaximumLength(50);
        RuleFor(h => h.Address).NotEmpty().MaximumLength(500);
        RuleFor(h => h.City).NotEmpty().MaximumLength(100);
        RuleFor(h => h.State).NotEmpty().MaximumLength(50);
        RuleFor(h => h.ZipCode).NotEmpty().MaximumLength(20);
        RuleFor(h => h.MaxCapacity).GreaterThanOrEqualTo(0);
    }
}

public class EventValidator : AbstractValidator<Event>
{
    public EventValidator()
    {
        RuleFor(e => e.Title).NotEmpty().MaximumLength(300);
        RuleFor(e => e.ArtistId).NotEmpty();
        RuleFor(e => e.HostId).NotEmpty();
        RuleFor(e => e.EventDate).NotEmpty();
        RuleFor(e => e.StartTime).NotEmpty();
        RuleFor(e => e.MaxAttendees).GreaterThan(0);
        RuleFor(e => e.SuggestedDonationAmount).GreaterThanOrEqualTo(0);
        RuleFor(e => e.MinimumContributionAmount)
            .GreaterThan(0).When(e => e.MinimumContributionAmount.HasValue);
        RuleFor(e => e.SuggestedDonationLabel).NotEmpty().MaximumLength(500);
        RuleFor(e => e.PricingDisclosureText).NotEmpty().MaximumLength(2000);
    }
}

public class ReservationValidator : AbstractValidator<Reservation>
{
    public ReservationValidator()
    {
        RuleFor(r => r.EventId).NotEmpty();
        RuleFor(r => r.AttendeeName).NotEmpty().MaximumLength(200);
        RuleFor(r => r.AttendeeEmail).NotEmpty().EmailAddress().MaximumLength(200);
        RuleFor(r => r.AttendeePhone).MaximumLength(50);
        RuleFor(r => r.NumberOfGuests).GreaterThan(0).LessThanOrEqualTo(20);
    }
}

public class CreatePaymentRequestValidator : AbstractValidator<CreatePaymentRequest>
{
    public CreatePaymentRequestValidator()
    {
        RuleFor(p => p.ReservationId).NotEmpty();
        RuleFor(p => p.Amount).GreaterThan(0)
            .WithMessage("Contribution amount must be greater than zero.");
    }
}

public class DoorPaymentRequestValidator : AbstractValidator<DoorPaymentRequest>
{
    public DoorPaymentRequestValidator()
    {
        RuleFor(p => p.ReservationId).NotEmpty();
        RuleFor(p => p.Amount).GreaterThanOrEqualTo(0);
    }
}


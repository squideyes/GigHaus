using FluentValidation.TestHelper;
using GigHaus.Domain.Entities;
using GigHaus.Domain.Enums;
using GigHaus.Infrastructure.Services;
using GigHaus.WebApi.Validation;
using HostEntity = GigHaus.Domain.Entities.Host;

namespace GigHaus.Tests.Validation;

public class ArtistValidatorTests
{
    private readonly ArtistValidator _validator = new();

    [Fact]
    public void Valid_Artist_Passes()
    {
        var artist = new Artist { Name = "Alice", Email = "alice@test.com" };
        var result = _validator.TestValidate(artist);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("", "alice@test.com", "Name")]
    [InlineData("Alice", "", "Email")]
    [InlineData("Alice", "not-an-email", "Email")]
    public void Invalid_Artist_Fails(string name, string email, string expectedProperty)
    {
        var artist = new Artist { Name = name, Email = email };
        var result = _validator.TestValidate(artist);
        result.ShouldHaveValidationErrorFor(expectedProperty);
    }

    [Fact]
    public void Name_ExceedsMaxLength_Fails()
    {
        var artist = new Artist { Name = new string('A', 201), Email = "a@t.com" };
        _validator.TestValidate(artist).ShouldHaveValidationErrorFor(a => a.Name);
    }

    [Fact]
    public void Phone_ExceedsMaxLength_Fails()
    {
        var artist = new Artist { Name = "A", Email = "a@t.com", Phone = new string('1', 51) };
        _validator.TestValidate(artist).ShouldHaveValidationErrorFor(a => a.Phone);
    }

    [Fact]
    public void Website_ExceedsMaxLength_Fails()
    {
        var artist = new Artist { Name = "A", Email = "a@t.com", Website = new string('x', 501) };
        _validator.TestValidate(artist).ShouldHaveValidationErrorFor(a => a.Website);
    }
}

public class HostValidatorTests
{
    private readonly HostValidator _validator = new();

    [Fact]
    public void Valid_Host_Passes()
    {
        var host = new HostEntity
        {
            Name = "Jane", Email = "j@t.com", Address = "123 St",
            City = "Austin", State = "TX", ZipCode = "78701", MaxCapacity = 30
        };
        _validator.TestValidate(host).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Empty_Name_Fails() =>
        _validator.TestValidate(new HostEntity { Email = "j@t.com", Address = "x", City = "x", State = "x", ZipCode = "x" })
            .ShouldHaveValidationErrorFor(h => h.Name);

    [Fact]
    public void Empty_Address_Fails() =>
        _validator.TestValidate(new HostEntity { Name = "J", Email = "j@t.com", City = "x", State = "x", ZipCode = "x" })
            .ShouldHaveValidationErrorFor(h => h.Address);

    [Fact]
    public void Empty_City_Fails() =>
        _validator.TestValidate(new HostEntity { Name = "J", Email = "j@t.com", Address = "x", State = "x", ZipCode = "x" })
            .ShouldHaveValidationErrorFor(h => h.City);

    [Fact]
    public void Empty_State_Fails() =>
        _validator.TestValidate(new HostEntity { Name = "J", Email = "j@t.com", Address = "x", City = "x", ZipCode = "x" })
            .ShouldHaveValidationErrorFor(h => h.State);

    [Fact]
    public void Empty_ZipCode_Fails() =>
        _validator.TestValidate(new HostEntity { Name = "J", Email = "j@t.com", Address = "x", City = "x", State = "x" })
            .ShouldHaveValidationErrorFor(h => h.ZipCode);

    [Fact]
    public void Negative_MaxCapacity_Fails()
    {
        var host = new HostEntity
        {
            Name = "J", Email = "j@t.com", Address = "x", City = "x", State = "x", ZipCode = "x",
            MaxCapacity = -1
        };
        _validator.TestValidate(host).ShouldHaveValidationErrorFor(h => h.MaxCapacity);
    }

    [Fact]
    public void Invalid_Email_Fails() =>
        _validator.TestValidate(new HostEntity { Name = "J", Email = "bad", Address = "x", City = "x", State = "x", ZipCode = "x" })
            .ShouldHaveValidationErrorFor(h => h.Email);
}

public class EventValidatorTests
{
    private readonly EventValidator _validator = new();

    private Event ValidEvent() => new()
    {
        Title = "Concert", ArtistId = Guid.NewGuid(), HostId = Guid.NewGuid(),
        EventDate = new DateOnly(2026, 7, 1), StartTime = new TimeOnly(19, 0),
        MaxAttendees = 20, SuggestedDonationAmount = 25m
    };

    [Fact]
    public void Valid_Event_Passes() =>
        _validator.TestValidate(ValidEvent()).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Empty_Title_Fails() =>
        _validator.TestValidate(new Event { Title = "" }).ShouldHaveValidationErrorFor(e => e.Title);

    [Fact]
    public void Empty_ArtistId_Fails()
    {
        var ev = ValidEvent();
        ev.ArtistId = Guid.Empty;
        _validator.TestValidate(ev).ShouldHaveValidationErrorFor(e => e.ArtistId);
    }

    [Fact]
    public void Empty_HostId_Fails()
    {
        var ev = ValidEvent();
        ev.HostId = Guid.Empty;
        _validator.TestValidate(ev).ShouldHaveValidationErrorFor(e => e.HostId);
    }

    [Fact]
    public void Zero_MaxAttendees_Fails()
    {
        var ev = ValidEvent();
        ev.MaxAttendees = 0;
        _validator.TestValidate(ev).ShouldHaveValidationErrorFor(e => e.MaxAttendees);
    }

    [Fact]
    public void Negative_SuggestedDonation_Fails()
    {
        var ev = ValidEvent();
        ev.SuggestedDonationAmount = -5m;
        _validator.TestValidate(ev).ShouldHaveValidationErrorFor(e => e.SuggestedDonationAmount);
    }

    [Fact]
    public void MinimumContribution_Zero_Fails()
    {
        var ev = ValidEvent();
        ev.MinimumContributionAmount = 0m;
        _validator.TestValidate(ev).ShouldHaveValidationErrorFor(e => e.MinimumContributionAmount);
    }

    [Fact]
    public void MinimumContribution_Null_Passes()
    {
        var ev = ValidEvent();
        ev.MinimumContributionAmount = null;
        _validator.TestValidate(ev).ShouldNotHaveValidationErrorFor(e => e.MinimumContributionAmount);
    }

    [Fact]
    public void MinimumContribution_Positive_Passes()
    {
        var ev = ValidEvent();
        ev.MinimumContributionAmount = 10m;
        _validator.TestValidate(ev).ShouldNotHaveValidationErrorFor(e => e.MinimumContributionAmount);
    }

    [Fact]
    public void Title_ExceedsMaxLength_Fails()
    {
        var ev = ValidEvent();
        ev.Title = new string('X', 301);
        _validator.TestValidate(ev).ShouldHaveValidationErrorFor(e => e.Title);
    }
}

public class ReservationValidatorTests
{
    private readonly ReservationValidator _validator = new();

    [Fact]
    public void Valid_Reservation_Passes()
    {
        var res = new Reservation
        {
            EventId = Guid.NewGuid(), AttendeeName = "Bob",
            AttendeeEmail = "bob@test.com", NumberOfGuests = 2
        };
        _validator.TestValidate(res).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Empty_EventId_Fails() =>
        _validator.TestValidate(new Reservation { AttendeeName = "B", AttendeeEmail = "b@t.com" })
            .ShouldHaveValidationErrorFor(r => r.EventId);

    [Fact]
    public void Empty_AttendeeName_Fails() =>
        _validator.TestValidate(new Reservation { EventId = Guid.NewGuid(), AttendeeEmail = "b@t.com" })
            .ShouldHaveValidationErrorFor(r => r.AttendeeName);

    [Fact]
    public void Empty_AttendeeEmail_Fails() =>
        _validator.TestValidate(new Reservation { EventId = Guid.NewGuid(), AttendeeName = "B" })
            .ShouldHaveValidationErrorFor(r => r.AttendeeEmail);

    [Fact]
    public void Invalid_AttendeeEmail_Fails() =>
        _validator.TestValidate(new Reservation { EventId = Guid.NewGuid(), AttendeeName = "B", AttendeeEmail = "not-email" })
            .ShouldHaveValidationErrorFor(r => r.AttendeeEmail);

    [Fact]
    public void Zero_Guests_Fails()
    {
        var res = new Reservation { EventId = Guid.NewGuid(), AttendeeName = "B", AttendeeEmail = "b@t.com", NumberOfGuests = 0 };
        _validator.TestValidate(res).ShouldHaveValidationErrorFor(r => r.NumberOfGuests);
    }

    [Fact]
    public void TooMany_Guests_Fails()
    {
        var res = new Reservation { EventId = Guid.NewGuid(), AttendeeName = "B", AttendeeEmail = "b@t.com", NumberOfGuests = 21 };
        _validator.TestValidate(res).ShouldHaveValidationErrorFor(r => r.NumberOfGuests);
    }

    [Fact]
    public void MaxGuests_20_Passes()
    {
        var res = new Reservation { EventId = Guid.NewGuid(), AttendeeName = "B", AttendeeEmail = "b@t.com", NumberOfGuests = 20 };
        _validator.TestValidate(res).ShouldNotHaveValidationErrorFor(r => r.NumberOfGuests);
    }

    [Fact]
    public void Phone_ExceedsMaxLength_Fails()
    {
        var res = new Reservation { EventId = Guid.NewGuid(), AttendeeName = "B", AttendeeEmail = "b@t.com", AttendeePhone = new string('1', 51) };
        _validator.TestValidate(res).ShouldHaveValidationErrorFor(r => r.AttendeePhone);
    }
}

public class PaymentRequestValidatorTests
{
    private readonly CreatePaymentRequestValidator _createValidator = new();
    private readonly DoorPaymentRequestValidator _doorValidator = new();

    [Fact]
    public void Valid_CreatePaymentRequest_Passes()
    {
        var req = new CreatePaymentRequest(Guid.NewGuid(), 50m, null);
        _createValidator.TestValidate(req).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Zero_Amount_CreatePayment_Fails()
    {
        var req = new CreatePaymentRequest(Guid.NewGuid(), 0m, null);
        _createValidator.TestValidate(req).ShouldHaveValidationErrorFor(r => r.Amount);
    }

    [Fact]
    public void Negative_Amount_CreatePayment_Fails()
    {
        var req = new CreatePaymentRequest(Guid.NewGuid(), -10m, null);
        _createValidator.TestValidate(req).ShouldHaveValidationErrorFor(r => r.Amount);
    }

    [Fact]
    public void Empty_ReservationId_CreatePayment_Fails()
    {
        var req = new CreatePaymentRequest(Guid.Empty, 50m, null);
        _createValidator.TestValidate(req).ShouldHaveValidationErrorFor(r => r.ReservationId);
    }

    [Fact]
    public void Valid_DoorPaymentRequest_Passes()
    {
        var req = new DoorPaymentRequest(Guid.NewGuid(), 30m);
        _doorValidator.TestValidate(req).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Zero_Amount_DoorPayment_Passes()
    {
        var req = new DoorPaymentRequest(Guid.NewGuid(), 0m);
        _doorValidator.TestValidate(req).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Negative_Amount_DoorPayment_Fails()
    {
        var req = new DoorPaymentRequest(Guid.NewGuid(), -1m);
        _doorValidator.TestValidate(req).ShouldHaveValidationErrorFor(r => r.Amount);
    }

    [Fact]
    public void Empty_ReservationId_DoorPayment_Fails()
    {
        var req = new DoorPaymentRequest(Guid.Empty, 30m);
        _doorValidator.TestValidate(req).ShouldHaveValidationErrorFor(r => r.ReservationId);
    }
}

using GigHaus.Domain.Entities;
using GigHaus.Domain.Enums;
using GigHaus.Infrastructure.Email;
using GigHaus.Infrastructure.Repositories;
using GigHaus.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;

namespace GigHaus.Tests.Services;

public class HostPortalServiceTests
{
    private const int VenueCapacity = 30;
    private const string Title = "Evening Show";
    private const string ArtistName = "June Harper";
    private const string ArtistEmail = "june@example.test";

    private static readonly DateTimeOffset Now = new(2026, 11, 6, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly NextMonth = new(2026, 12, 6);
    private static readonly DateOnly LastMonth = new(2026, 10, 6);

    private readonly IHostRepository _hosts = Substitute.For<IHostRepository>();
    private readonly IEventRepository _events = Substitute.For<IEventRepository>();
    private readonly IArtistRepository _artists = Substitute.For<IArtistRepository>();
    private readonly IReservationRepository _reservations = Substitute.For<IReservationRepository>();
    private readonly IReservationService _reservationService = Substitute.For<IReservationService>();
    private readonly IEmailService _email = Substitute.For<IEmailService>();
    private readonly Host _host = new() { MaxCapacity = VenueCapacity, Status = HostStatus.Approved };
    private readonly HostPortalService _portal;

    public HostPortalServiceTests()
    {
        var clock = Substitute.For<TimeProvider>();
        clock.GetUtcNow().Returns(Now);

        _hosts.GetByIdAsync(_host.Id, Arg.Any<CancellationToken>()).Returns(_host);
        _events.CreateAsync(Arg.Any<Event>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<Event>());
        _events.UpdateAsync(Arg.Any<Event>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<Event>());

        _portal = new HostPortalService(
            _hosts, _events, _artists, _reservations, _reservationService, _email,
            clock, NullLogger<HostPortalService>.Instance);
    }

    [Fact]
    public async Task SaveEvent_NewShow_StartsAsADraftOnThisHost()
    {
        var ev = NewShow();
        ev.IsPublished = true;
        ev.HostId = Guid.NewGuid();

        var result = await _portal.SaveEventAsync(_host.Id, ev);

        result.Success.ShouldBeTrue();
        result.Value!.HostId.ShouldBe(_host.Id);
        result.Value.IsPublished.ShouldBeFalse();
    }

    [Fact]
    public async Task SaveEvent_ContributionsAreDoorOnly()
    {
        var result = await _portal.SaveEventAsync(_host.Id, NewShow());

        result.Value!.AllowCashAtDoor.ShouldBeTrue();
        result.Value.AllowOnlinePayment.ShouldBeFalse();
    }

    [Fact]
    public async Task SaveEvent_PastDate_Fails()
    {
        var ev = NewShow();
        ev.EventDate = LastMonth;

        var result = await _portal.SaveEventAsync(_host.Id, ev);

        result.Success.ShouldBeFalse();
        await _events.DidNotReceive().CreateAsync(Arg.Any<Event>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(VenueCapacity + 1)]
    public async Task SaveEvent_GuestLimitOutsideVenueCapacity_Fails(int limit)
    {
        var ev = NewShow();
        ev.MaxAttendees = limit;

        var result = await _portal.SaveEventAsync(_host.Id, ev);

        result.Success.ShouldBeFalse();
    }

    [Fact]
    public async Task SaveEvent_EndBeforeStart_Fails()
    {
        var ev = NewShow();
        ev.StartTime = new TimeOnly(20, 0);
        ev.EndTime = new TimeOnly(19, 0);

        var result = await _portal.SaveEventAsync(_host.Id, ev);

        result.Success.ShouldBeFalse();
    }

    [Fact]
    public async Task SaveEvent_MinimumRequiredWithoutAmount_Fails()
    {
        var ev = NewShow();
        ev.PricingMode = PricingMode.MinimumRequired;

        var result = await _portal.SaveEventAsync(_host.Id, ev);

        result.Success.ShouldBeFalse();
    }

    [Fact]
    public async Task SaveEvent_SuggestedBelowMinimum_Fails()
    {
        var ev = NewShow();
        ev.PricingMode = PricingMode.MinimumRequired;
        ev.MinimumContributionAmount = 30m;
        ev.SuggestedDonationAmount = 20m;

        var result = await _portal.SaveEventAsync(_host.Id, ev);

        result.Success.ShouldBeFalse();
    }

    [Fact]
    public async Task SaveEvent_MinimumRequired_TurnsOffPayWhatYouCan()
    {
        var ev = NewShow();
        ev.PricingMode = PricingMode.MinimumRequired;
        ev.MinimumContributionAmount = 15m;
        ev.SuggestedDonationAmount = 20m;

        var result = await _portal.SaveEventAsync(_host.Id, ev);

        result.Value!.AllowPayWhatYouCan.ShouldBeFalse();
    }

    [Fact]
    public async Task SaveEvent_SuggestedOnly_ClearsAnyMinimum()
    {
        var ev = NewShow();
        ev.MinimumContributionAmount = 15m;

        var result = await _portal.SaveEventAsync(_host.Id, ev);

        result.Value!.MinimumContributionAmount.ShouldBeNull();
    }

    [Fact]
    public async Task SaveEvent_AnotherHostsShow_FailsWithoutChangingIt()
    {
        var theirs = NewShow();
        theirs.HostId = Guid.NewGuid();
        _events.GetByIdAsync(theirs.Id, Arg.Any<CancellationToken>()).Returns(theirs);

        var result = await _portal.SaveEventAsync(_host.Id, NewShow(theirs.Id));

        result.Success.ShouldBeFalse();
        await _events.DidNotReceive().UpdateAsync(Arg.Any<Event>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveEvent_Existing_KeepsPublishedStateAndDisclosure()
    {
        var stored = StoredShow();
        stored.IsPublished = true;
        stored.PricingDisclosureText = Title;

        var result = await _portal.SaveEventAsync(_host.Id, NewShow(stored.Id));

        result.Value!.IsPublished.ShouldBeTrue();
        result.Value.PricingDisclosureText.ShouldBe(Title);
    }

    [Fact]
    public async Task SaveEvent_LimitBelowReservedGuests_Fails()
    {
        var stored = StoredShow();
        _reservations.GetConfirmedGuestCountAsync(stored.Id, Arg.Any<CancellationToken>()).Returns(12);
        var edit = NewShow(stored.Id);
        edit.MaxAttendees = 10;

        var result = await _portal.SaveEventAsync(_host.Id, edit);

        result.Success.ShouldBeFalse();
    }

    [Theory]
    [InlineData(HostStatus.Pending)]
    [InlineData(HostStatus.Suspended)]
    public async Task SetPublished_HostNotApproved_Fails(HostStatus status)
    {
        _host.Status = status;
        var stored = StoredShow();

        var result = await _portal.SetPublishedAsync(_host.Id, stored.Id, isPublished: true);

        result.Success.ShouldBeFalse();
        stored.IsPublished.ShouldBeFalse();
    }

    [Fact]
    public async Task SetPublished_HostNotApproved_CanStillUnpublish()
    {
        _host.Status = HostStatus.Suspended;
        var stored = StoredShow();
        stored.IsPublished = true;

        var result = await _portal.SetPublishedAsync(_host.Id, stored.Id, isPublished: false);

        result.Success.ShouldBeTrue();
        stored.IsPublished.ShouldBeFalse();
    }

    [Fact]
    public async Task GetEvent_AnotherHostsShow_ReturnsNull()
    {
        var theirs = NewShow();
        theirs.HostId = Guid.NewGuid();
        _events.GetByIdAsync(theirs.Id, Arg.Any<CancellationToken>()).Returns(theirs);

        var result = await _portal.GetEventAsync(_host.Id, theirs.Id);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task GuestActions_OnAnotherHostsGuest_AreRefused()
    {
        var theirs = NewShow();
        theirs.HostId = Guid.NewGuid();
        var guest = new Reservation { EventId = theirs.Id };
        _events.GetByIdAsync(theirs.Id, Arg.Any<CancellationToken>()).Returns(theirs);
        _reservations.GetByIdAsync(guest.Id, Arg.Any<CancellationToken>()).Returns(guest);

        var confirm = await _portal.ConfirmGuestAsync(_host.Id, guest.Id);
        var checkIn = await _portal.CheckInGuestAsync(_host.Id, guest.Id);
        var cancel = await _portal.CancelGuestAsync(_host.Id, guest.Id, null);

        confirm.Success.ShouldBeFalse();
        checkIn.Success.ShouldBeFalse();
        cancel.Success.ShouldBeFalse();
        await _reservationService.DidNotReceive().ConfirmReservationAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ConfirmGuest_StillSucceedsWhenEmailFails()
    {
        var stored = StoredShow();
        var guest = new Reservation { EventId = stored.Id, AttendeeEmail = ArtistEmail };
        _reservations.GetByIdAsync(guest.Id, Arg.Any<CancellationToken>()).Returns(guest);
        _reservationService.ConfirmReservationAsync(guest.Id, Arg.Any<CancellationToken>()).Returns(ReservationResult.Ok(guest));
        _email.SendEmailAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>()).ThrowsAsync(new InvalidOperationException());

        var result = await _portal.ConfirmGuestAsync(_host.Id, guest.Id);

        result.Success.ShouldBeTrue();
    }

    [Fact]
    public async Task ConfirmGuest_EscapesGuestEnteredTextInTheEmail()
    {
        var stored = StoredShow();
        var guest = new Reservation { EventId = stored.Id, AttendeeName = "<script>", AttendeeEmail = ArtistEmail };
        _reservations.GetByIdAsync(guest.Id, Arg.Any<CancellationToken>()).Returns(guest);
        _reservationService.ConfirmReservationAsync(guest.Id, Arg.Any<CancellationToken>()).Returns(ReservationResult.Ok(guest));

        await _portal.ConfirmGuestAsync(_host.Id, guest.Id);

        await _email.Received(1).SendEmailAsync(
            Arg.Is<EmailMessage>(m => !m.HtmlBody.Contains("<script>")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FindOrAddArtist_ReusesAnExistingArtist()
    {
        var existing = new Artist { Name = ArtistName, Email = ArtistEmail };
        _artists.GetByEmailAsync(ArtistEmail, Arg.Any<CancellationToken>()).Returns(existing);

        var result = await _portal.FindOrAddArtistAsync(ArtistName, ArtistEmail, website: null);

        result.Value.ShouldBe(existing);
        await _artists.DidNotReceive().CreateAsync(Arg.Any<Artist>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateVenue_ZeroCapacity_Fails()
    {
        var result = await _portal.UpdateVenueAsync(_host.Id, new Host { MaxCapacity = 0 });

        result.Success.ShouldBeFalse();
    }

    private static Event NewShow(Guid? id = null) => new()
    {
        Id = id ?? Guid.NewGuid(),
        Title = Title,
        EventDate = NextMonth,
        StartTime = new TimeOnly(19, 0),
        MaxAttendees = 20,
        SuggestedDonationAmount = 20m
    };

    private Event StoredShow()
    {
        var stored = NewShow();
        stored.HostId = _host.Id;
        stored.Host = _host;
        stored.Artist = new Artist { Name = ArtistName };
        _events.GetByIdAsync(stored.Id, Arg.Any<CancellationToken>()).Returns(stored);
        return stored;
    }
}

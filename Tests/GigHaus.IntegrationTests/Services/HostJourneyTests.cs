using GigHaus.Domain.Entities;
using GigHaus.Domain.Enums;
using GigHaus.Infrastructure.Email;
using GigHaus.Infrastructure.Repositories;
using GigHaus.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace GigHaus.IntegrationTests.Services;

/// <summary>
/// Runs the real services against the real database, from sign-up to the door.
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class HostJourneyTests(PostgresFixture fixture) : IDisposable
{
    private const string PortalUrl = "https://gighaus.test/portal";
    private const string SignupName = "Maple Street Sessions";

    private readonly IServiceScope _scope = fixture.Services.CreateScope();

    private IHostOnboardingService Onboarding => _scope.ServiceProvider.GetRequiredService<IHostOnboardingService>();
    private IHostPortalService Portal => _scope.ServiceProvider.GetRequiredService<IHostPortalService>();
    private IReservationService Reservations => _scope.ServiceProvider.GetRequiredService<IReservationService>();
    private IHostRepository Hosts => _scope.ServiceProvider.GetRequiredService<IHostRepository>();
    private IArtistRepository Artists => _scope.ServiceProvider.GetRequiredService<IArtistRepository>();

    [Fact]
    public async Task SignUp_CreatesPendingHostLinkedToUser()
    {
        var result = await Onboarding.SignUpAsync(NewSignup());

        result.Success.ShouldBeTrue();
        var host = await Hosts.GetByIdAsync(result.Value!.HostId!.Value);
        host!.Status.ShouldBe(HostStatus.Pending);
        host.Name.ShouldBe(SignupName);
    }

    [Fact]
    public async Task SignUp_TwiceWithOneEmail_FailsTheSecondTime()
    {
        var signup = NewSignup();
        await Onboarding.SignUpAsync(signup);

        var second = await Onboarding.SignUpAsync(signup with { Email = signup.Email.ToUpperInvariant() });

        second.Success.ShouldBeFalse();
    }

    [Fact]
    public async Task SignUp_WithWeakPassword_LeavesNoHostBehind()
    {
        var signup = NewSignup() with { Password = "short" };

        var result = await Onboarding.SignUpAsync(signup);

        result.Success.ShouldBeFalse();
        (await Hosts.GetByEmailAsync(signup.Email)).ShouldBeNull();
    }

    [Fact]
    public async Task SignUp_MovesAMatchingLeadToSignedUp()
    {
        var leads = _scope.ServiceProvider.GetRequiredService<ILeadRepository>();
        var signup = NewSignup();
        var lead = await leads.CreateAsync(new Lead { Name = SignupName, Email = signup.Email });

        await Onboarding.SignUpAsync(signup);

        (await leads.GetAllAsync()).Single(l => l.Id == lead.Id).Status.ShouldBe(LeadStatus.SignedUp);
    }

    [Fact]
    public async Task Publish_IsRefusedUntilStaffApprove()
    {
        var host = await Hosts.CreateAsync(TestData.Host(HostStatus.Pending));
        var ev = await SaveShowAsync(host.Id);

        var beforeApproval = await Portal.SetPublishedAsync(host.Id, ev.Id, isPublished: true);
        await Onboarding.SetStatusAsync(host.Id, HostStatus.Approved, PortalUrl);
        var afterApproval = await Portal.SetPublishedAsync(host.Id, ev.Id, isPublished: true);

        beforeApproval.Success.ShouldBeFalse();
        afterApproval.Success.ShouldBeTrue();
    }

    [Fact]
    public async Task Confirm_EmailsTheStreetAddressToTheGuest()
    {
        var (host, ev) = await PublishedShowAsync();
        var created = await Reservations.CreateReservationAsync(TestData.Reservation(ev.Id));

        var confirmed = await Portal.ConfirmGuestAsync(host.Id, created.Reservation!.Id);

        confirmed.Success.ShouldBeTrue();
        await fixture.Email.Received(1).SendEmailAsync(
            Arg.Is<EmailMessage>(m => m.To == created.Reservation.AttendeeEmail && m.HtmlBody.Contains(TestData.Street)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AnotherHost_CannotSeeOrActOnAGuest()
    {
        var (_, ev) = await PublishedShowAsync();
        var stranger = await Hosts.CreateAsync(TestData.Host());
        var created = await Reservations.CreateReservationAsync(TestData.Reservation(ev.Id));

        var guests = await Portal.GetGuestsAsync(stranger.Id, ev.Id);
        var confirm = await Portal.ConfirmGuestAsync(stranger.Id, created.Reservation!.Id);

        guests.ShouldBeEmpty();
        confirm.Success.ShouldBeFalse();
    }

    [Fact]
    public async Task Reservations_StopAtTheGuestLimit()
    {
        var (_, ev) = await PublishedShowAsync(maxAttendees: 3);

        var first = await Reservations.CreateReservationAsync(TestData.Reservation(ev.Id, guests: 2));
        var second = await Reservations.CreateReservationAsync(TestData.Reservation(ev.Id, guests: 2));

        first.Success.ShouldBeTrue();
        second.Success.ShouldBeFalse();
    }

    [Fact]
    public async Task CheckIn_AppearsInTheHostsGuestList()
    {
        var (host, ev) = await PublishedShowAsync();
        var created = await Reservations.CreateReservationAsync(TestData.Reservation(ev.Id));
        await Portal.ConfirmGuestAsync(host.Id, created.Reservation!.Id);

        await Portal.CheckInGuestAsync(host.Id, created.Reservation.Id);

        var all = await Portal.GetAllGuestsAsync(host.Id);
        all.Single().Status.ShouldBe(ReservationStatus.CheckedIn);
        all.Single().CheckedInAt.ShouldNotBeNull();
    }

    public void Dispose() => _scope.Dispose();

    private static HostSignup NewSignup() => new()
    {
        Name = SignupName,
        Email = TestData.UniqueEmail(),
        Password = TestData.Password,
        Address = TestData.Street,
        City = TestData.City,
        State = TestData.State,
        ZipCode = "19103",
        MaxCapacity = 25
    };

    private async Task<Event> SaveShowAsync(Guid hostId, int maxAttendees = 20)
    {
        var artist = await Artists.CreateAsync(TestData.Artist());
        var show = TestData.Event(hostId, artist.Id);
        show.MaxAttendees = maxAttendees;

        var saved = await Portal.SaveEventAsync(hostId, show);
        saved.Success.ShouldBeTrue(saved.Error);
        return saved.Value!;
    }

    private async Task<(Host Host, Event Event)> PublishedShowAsync(int maxAttendees = 20)
    {
        var host = await Hosts.CreateAsync(TestData.Host());
        var ev = await SaveShowAsync(host.Id, maxAttendees);

        var published = await Portal.SetPublishedAsync(host.Id, ev.Id, isPublished: true);
        published.Success.ShouldBeTrue(published.Error);
        return (host, ev);
    }
}

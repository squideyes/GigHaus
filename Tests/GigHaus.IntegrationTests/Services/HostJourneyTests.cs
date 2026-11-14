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
    private const string SignupUrl = "https://gighaus.test/signup";
    private const string SignupName = "Maple Street Sessions";

    private readonly IServiceScope _scope = fixture.Services.CreateScope();

    private IHostOnboardingService Onboarding => _scope.ServiceProvider.GetRequiredService<IHostOnboardingService>();
    private IHostPortalService Portal => _scope.ServiceProvider.GetRequiredService<IHostPortalService>();
    private IReservationService Reservations => _scope.ServiceProvider.GetRequiredService<IReservationService>();
    private IHostRepository Hosts => _scope.ServiceProvider.GetRequiredService<IHostRepository>();
    private IArtistRepository Artists => _scope.ServiceProvider.GetRequiredService<IArtistRepository>();

    [Fact]
    public async Task SignUp_WithAnInvitation_CreatesApprovedHostWithTheInvitedEmail()
    {
        var (lead, token) = await InviteAsync();

        var result = await Onboarding.SignUpAsync(NewSignup(token));

        result.Success.ShouldBeTrue(result.Error);
        result.Value!.Email.ShouldBe(lead.Email);
        var host = await Hosts.GetByIdAsync(result.Value.HostId!.Value);
        host!.Status.ShouldBe(HostStatus.Approved);
        host.Name.ShouldBe(SignupName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-real-token")]
    public async Task SignUp_WithoutAValidInvitation_IsRefused(string token)
    {
        var result = await Onboarding.SignUpAsync(NewSignup(token));

        result.Success.ShouldBeFalse();
    }

    [Fact]
    public async Task SignUp_UsesUpTheInvitation()
    {
        var (lead, token) = await InviteAsync();
        await Onboarding.SignUpAsync(NewSignup(token));

        var second = await Onboarding.SignUpAsync(NewSignup(token));

        second.Success.ShouldBeFalse();
        (await Onboarding.FindInvitationAsync(token)).ShouldBeNull();
        (await Leads.GetByIdAsync(lead.Id))!.Status.ShouldBe(LeadStatus.SignedUp);
    }

    [Fact]
    public async Task SignUp_WithWeakPassword_LeavesNoHostBehindAndKeepsTheInvitation()
    {
        var (lead, token) = await InviteAsync();

        var result = await Onboarding.SignUpAsync(NewSignup(token) with { Password = "short" });

        result.Success.ShouldBeFalse();
        (await Hosts.GetByEmailAsync(lead.Email)).ShouldBeNull();
        (await Onboarding.FindInvitationAsync(token)).ShouldNotBeNull();
    }

    [Fact]
    public async Task Invite_Again_ReplacesTheEarlierLink()
    {
        var (lead, first) = await InviteAsync();

        var second = await InviteAsync(lead);

        (await Onboarding.FindInvitationAsync(first)).ShouldBeNull();
        (await Onboarding.FindInvitationAsync(second))!.Id.ShouldBe(lead.Id);
    }

    [Fact]
    public async Task Invitation_StopsWorkingOnceItExpires()
    {
        var (lead, token) = await InviteAsync();
        await Leads.SetInvitedAsync(lead.Id, new string('a', 64), DateTime.UtcNow.AddMinutes(-1));

        (await Onboarding.FindInvitationAsync(token)).ShouldBeNull();
        (await Leads.GetByInviteTokenHashAsync(new string('a', 64)))!.Status.ShouldBe(LeadStatus.Invited);
        (await Onboarding.SignUpAsync(NewSignup(token))).Success.ShouldBeFalse();
    }

    [Fact]
    public async Task InviteNew_ReusesAnOpenRequestForTheSameEmail()
    {
        var request = await Leads.CreateAsync(new Lead { Name = SignupName, Email = TestData.UniqueEmail() });

        var invited = await Onboarding.InviteNewAsync("Someone Else", request.Email.ToUpperInvariant(), SignupUrl);

        invited.Success.ShouldBeTrue(invited.Error);
        invited.Value!.Id.ShouldBe(request.Id);
        invited.Value.Status.ShouldBe(LeadStatus.Invited);
    }

    [Fact]
    public async Task Invite_IsRefusedForAnEmailThatAlreadyHasAnAccount()
    {
        var (lead, token) = await InviteAsync();
        await Onboarding.SignUpAsync(NewSignup(token));

        var again = await Onboarding.InviteNewAsync(SignupName, lead.Email, SignupUrl);

        again.Success.ShouldBeFalse();
    }

    [Fact]
    public async Task Publish_IsRefusedUntilHostIsApproved()
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

    private ILeadRepository Leads => _scope.ServiceProvider.GetRequiredService<ILeadRepository>();

    /// <summary>Invites a new (or the given) lead and returns the token from the emailed link.</summary>
    private async Task<(Lead Lead, string Token)> InviteAsync()
    {
        var lead = await Leads.CreateAsync(new Lead { Name = SignupName, Email = TestData.UniqueEmail() });
        return (lead, await InviteAsync(lead));
    }

    private async Task<string> InviteAsync(Lead lead)
    {
        string? token = null;
        fixture.Email
            .When(e => e.SendEmailAsync(Arg.Is<EmailMessage>(m => m.To == lead.Email), Arg.Any<CancellationToken>()))
            .Do(call =>
            {
                var body = call.Arg<EmailMessage>().HtmlBody;
                var start = body.IndexOf("invite=", StringComparison.Ordinal) + "invite=".Length;
                token = body[start..body.IndexOf('"', start)];
            });

        var invited = await Onboarding.InviteAsync(lead.Id, SignupUrl);
        invited.Success.ShouldBeTrue(invited.Error);
        return token.ShouldNotBeNull();
    }

    private static HostSignup NewSignup(string inviteToken) => new()
    {
        InviteToken = inviteToken,
        Name = SignupName,
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

using GigHaus.Domain.Enums;
using GigHaus.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace GigHaus.IntegrationTests.Repositories;

[Collection(PostgresCollection.Name)]
public sealed class EventRepositoryTests(PostgresFixture fixture)
{
    private readonly IEventRepository _events = fixture.Services.GetRequiredService<IEventRepository>();
    private readonly IHostRepository _hosts = fixture.Services.GetRequiredService<IHostRepository>();
    private readonly IArtistRepository _artists = fixture.Services.GetRequiredService<IArtistRepository>();

    [Fact]
    public async Task Create_ThenGetById_RoundTripsDatesTimesEnumsAndMoney()
    {
        var ev = await CreateEventAsync();

        var stored = await _events.GetByIdAsync(ev.Id);

        stored.ShouldNotBeNull();
        stored.EventDate.ShouldBe(TestData.ShowDate);
        stored.StartTime.ShouldBe(TestData.ShowStart);
        stored.EndTime.ShouldBe(TestData.ShowEnd);
        stored.PricingMode.ShouldBe(PricingMode.SuggestedOnly);
        stored.SuggestedDonationAmount.ShouldBe(25m);
    }

    [Fact]
    public async Task GetById_LoadsArtistAndHostWithStatus()
    {
        var ev = await CreateEventAsync();

        var stored = await _events.GetByIdAsync(ev.Id);

        stored!.Artist.Name.ShouldNotBeNullOrEmpty();
        stored.Host.City.ShouldBe(TestData.City);
        stored.Host.Status.ShouldBe(HostStatus.Approved);
    }

    [Fact]
    public async Task GetByHostId_OnlyReturnsThatHostsShows()
    {
        var mine = await CreateEventAsync();
        await CreateEventAsync();

        var shows = await _events.GetByHostIdAsync(mine.HostId);

        shows.Single().Id.ShouldBe(mine.Id);
    }

    [Fact]
    public async Task GetAll_ReturnsShowsWithRelatedRows()
    {
        var ev = await CreateEventAsync();

        var shows = await _events.GetAllAsync();

        shows.Single(s => s.Id == ev.Id).Host.ShouldNotBeNull();
    }

    [Fact]
    public async Task Update_ChangesThePolicyAndPublishedState()
    {
        var ev = await CreateEventAsync();
        ev.PricingMode = PricingMode.MinimumRequired;
        ev.MinimumContributionAmount = 15m;
        ev.EndTime = null;
        ev.IsPublished = true;

        await _events.UpdateAsync(ev);

        var stored = await _events.GetByIdAsync(ev.Id);
        stored!.PricingMode.ShouldBe(PricingMode.MinimumRequired);
        stored.MinimumContributionAmount.ShouldBe(15m);
        stored.EndTime.ShouldBeNull();
        stored.IsPublished.ShouldBeTrue();
    }

    private async Task<Domain.Entities.Event> CreateEventAsync()
    {
        var host = await _hosts.CreateAsync(TestData.Host());
        var artist = await _artists.CreateAsync(TestData.Artist());
        return await _events.CreateAsync(TestData.Event(host.Id, artist.Id));
    }
}

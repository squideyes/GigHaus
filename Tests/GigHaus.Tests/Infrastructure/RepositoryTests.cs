using GigHaus.Domain.Entities;
using GigHaus.Domain.Enums;
using GigHaus.Infrastructure.Repositories;
using NSubstitute;

namespace GigHaus.Tests.Infrastructure;

public class RepositoryTests
{
    [Fact]
    public async Task ArtistRepository_GetAll_ReturnsFromRepository()
    {
        var repo = Substitute.For<IArtistRepository>();
        var expected = new List<Artist>
        {
            new() { Name = "Alice", Email = "alice@test.com" },
            new() { Name = "Bob", Email = "bob@test.com" }
        };
        repo.GetAllAsync().Returns(expected);

        var result = (await repo.GetAllAsync()).ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal("Alice", result[0].Name);
    }

    [Fact]
    public async Task ArtistRepository_GetById_ReturnsArtist()
    {
        var repo = Substitute.For<IArtistRepository>();
        var artist = new Artist { Name = "Test", Email = "t@t.com" };
        repo.GetByIdAsync(artist.Id).Returns(artist);

        var result = await repo.GetByIdAsync(artist.Id);

        Assert.NotNull(result);
        Assert.Equal("Test", result.Name);
    }

    [Fact]
    public async Task ArtistRepository_GetById_ReturnsNullForMissing()
    {
        var repo = Substitute.For<IArtistRepository>();
        repo.GetByIdAsync(Arg.Any<Guid>()).Returns((Artist?)null);

        var result = await repo.GetByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task ArtistRepository_Create_ReturnsCreatedArtist()
    {
        var repo = Substitute.For<IArtistRepository>();
        var artist = new Artist { Name = "New Artist", Email = "new@test.com" };
        repo.CreateAsync(artist).Returns(artist);

        var result = await repo.CreateAsync(artist);

        Assert.Equal("New Artist", result.Name);
        Assert.NotEqual(Guid.Empty, result.Id);
    }

    [Fact]
    public async Task ArtistRepository_Delete_ReturnsTrueWhenFound()
    {
        var repo = Substitute.For<IArtistRepository>();
        var id = Guid.NewGuid();
        repo.DeleteAsync(id).Returns(true);

        Assert.True(await repo.DeleteAsync(id));
    }

    [Fact]
    public async Task ArtistRepository_Delete_ReturnsFalseWhenNotFound()
    {
        var repo = Substitute.For<IArtistRepository>();
        repo.DeleteAsync(Arg.Any<Guid>()).Returns(false);

        Assert.False(await repo.DeleteAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task HostRepository_CreateAndRetrieve()
    {
        var repo = Substitute.For<IHostRepository>();
        var host = new Host
        {
            Name = "Jane", Email = "j@t.com", Address = "123 St",
            City = "Austin", State = "TX", ZipCode = "78701", MaxCapacity = 30
        };
        repo.CreateAsync(host).Returns(host);
        repo.GetByIdAsync(host.Id).Returns(host);

        await repo.CreateAsync(host);
        var loaded = await repo.GetByIdAsync(host.Id);

        Assert.NotNull(loaded);
        Assert.Equal(30, loaded.MaxCapacity);
    }

    [Fact]
    public async Task EventRepository_Create_WithRelationships()
    {
        var repo = Substitute.For<IEventRepository>();
        var ev = new Event
        {
            Title = "House Concert",
            ArtistId = Guid.NewGuid(),
            HostId = Guid.NewGuid(),
            EventDate = new DateOnly(2026, 6, 15),
            StartTime = new TimeOnly(19, 0),
            MaxAttendees = 20,
            SuggestedDonationAmount = 25m,
            PricingMode = PricingMode.SuggestedOnly
        };
        repo.CreateAsync(ev).Returns(ev);

        var result = await repo.CreateAsync(ev);

        Assert.Equal("House Concert", result.Title);
        Assert.Equal(25m, result.SuggestedDonationAmount);
    }

    [Fact]
    public async Task ReservationRepository_GetByEventId()
    {
        var repo = Substitute.For<IReservationRepository>();
        var eventId = Guid.NewGuid();
        var reservations = new List<Reservation>
        {
            new() { EventId = eventId, AttendeeName = "Bob", AttendeeEmail = "bob@t.com", NumberOfGuests = 2 }
        };
        repo.GetByEventIdAsync(eventId).Returns(reservations);

        var result = (await repo.GetByEventIdAsync(eventId)).ToList();

        Assert.Single(result);
        Assert.Equal(2, result[0].NumberOfGuests);
        Assert.Equal(ReservationStatus.Pending, result[0].Status);
    }
}


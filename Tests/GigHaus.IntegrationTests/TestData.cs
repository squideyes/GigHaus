using GigHaus.Domain.Entities;
using GigHaus.Domain.Enums;

namespace GigHaus.IntegrationTests;

/// <summary>
/// Builds valid rows. Emails are unique so tests can share one database.
/// </summary>
internal static class TestData
{
    public const string Password = "correct-horse-battery";
    public const string City = "Philadelphia";
    public const string State = "PA";
    public const string Street = "12 Elm Street";

    public static readonly DateOnly ShowDate = new(2099, 6, 15);
    public static readonly TimeOnly ShowStart = new(19, 30);
    public static readonly TimeOnly ShowEnd = new(21, 45);

    public static string UniqueEmail() => $"{Guid.NewGuid():N}@example.test";

    public static Host Host(HostStatus status = HostStatus.Approved, int capacity = 30) => new()
    {
        Name = "The Front Room",
        Email = UniqueEmail(),
        Address = Street,
        City = City,
        State = State,
        ZipCode = "19103",
        MaxCapacity = capacity,
        Status = status
    };

    public static Artist Artist() => new()
    {
        Name = "June Harper",
        Email = UniqueEmail()
    };

    public static Event Event(Guid hostId, Guid artistId) => new()
    {
        Title = "An Evening with June Harper",
        HostId = hostId,
        ArtistId = artistId,
        EventDate = ShowDate,
        StartTime = ShowStart,
        EndTime = ShowEnd,
        MaxAttendees = 20,
        SuggestedDonationAmount = 25m
    };

    public static Reservation Reservation(Guid eventId, int guests = 2) => new()
    {
        EventId = eventId,
        AttendeeName = "Sam Rivera",
        AttendeeEmail = UniqueEmail(),
        NumberOfGuests = guests
    };
}

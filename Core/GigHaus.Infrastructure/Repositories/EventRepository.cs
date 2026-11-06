using Dapper;
using GigHaus.Domain.Entities;
using GigHaus.Infrastructure.Data;

namespace GigHaus.Infrastructure.Repositories;

public class EventRepository : IEventRepository
{
    private readonly IDbConnectionFactory _db;

    public EventRepository(IDbConnectionFactory db) => _db = db;

    public async Task<IEnumerable<Event>> GetAllAsync(CancellationToken ct = default)
    {
        using var conn = await _db.CreateConnectionAsync(ct);
        const string sql = @"
            SELECT e.*, a.id, a.name, a.bio, a.email, a.phone, a.website, a.stripe_connect_account_id, a.created_at, a.updated_at,
                   h.id, h.name, h.email, h.phone, h.address, h.city, h.state, h.zip_code, h.max_capacity, h.venue_description, h.created_at, h.updated_at
            FROM events e
            JOIN artists a ON e.artist_id = a.id
            JOIN hosts h ON e.host_id = h.id
            ORDER BY e.event_date";
        return await conn.QueryAsync<Event, Artist, Domain.Entities.Host, Event>(sql, (ev, artist, host) =>
        {
            ev.Artist = artist;
            ev.Host = host;
            return ev;
        }, splitOn: "id,id");
    }

    public async Task<Event?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        using var conn = await _db.CreateConnectionAsync(ct);
        const string sql = @"
            SELECT e.*, a.id, a.name, a.bio, a.email, a.phone, a.website, a.stripe_connect_account_id, a.created_at, a.updated_at,
                   h.id, h.name, h.email, h.phone, h.address, h.city, h.state, h.zip_code, h.max_capacity, h.venue_description, h.created_at, h.updated_at
            FROM events e
            JOIN artists a ON e.artist_id = a.id
            JOIN hosts h ON e.host_id = h.id
            WHERE e.id = @Id";
        var results = await conn.QueryAsync<Event, Artist, Domain.Entities.Host, Event>(sql, (ev, artist, host) =>
        {
            ev.Artist = artist;
            ev.Host = host;
            return ev;
        }, new { Id = id }, splitOn: "id,id");
        return results.SingleOrDefault();
    }

    public async Task<Event> CreateAsync(Event ev, CancellationToken ct = default)
    {
        using var conn = await _db.CreateConnectionAsync(ct);
        await conn.ExecuteAsync(
            @"INSERT INTO events (id, title, description, artist_id, host_id, event_date, start_time, end_time,
              max_attendees, pricing_mode, suggested_donation_amount, minimum_contribution_amount,
              allow_pay_what_you_can, allow_reserve_now_pay_later, allow_cash_at_door, allow_online_payment,
              suggested_donation_label, pricing_disclosure_text, is_published, created_at, updated_at)
              VALUES (@Id, @Title, @Description, @ArtistId, @HostId, @EventDate, @StartTime, @EndTime,
              @MaxAttendees, @PricingMode, @SuggestedDonationAmount, @MinimumContributionAmount,
              @AllowPayWhatYouCan, @AllowReserveNowPayLater, @AllowCashAtDoor, @AllowOnlinePayment,
              @SuggestedDonationLabel, @PricingDisclosureText, @IsPublished, @CreatedAt, @UpdatedAt)",
            new
            {
                ev.Id, ev.Title, ev.Description, ev.ArtistId, ev.HostId,
                ev.EventDate, ev.StartTime, ev.EndTime, ev.MaxAttendees,
                PricingMode = ev.PricingMode.ToString(),
                ev.SuggestedDonationAmount, ev.MinimumContributionAmount,
                ev.AllowPayWhatYouCan, ev.AllowReserveNowPayLater,
                ev.AllowCashAtDoor, ev.AllowOnlinePayment,
                ev.SuggestedDonationLabel, ev.PricingDisclosureText,
                ev.IsPublished, ev.CreatedAt, ev.UpdatedAt
            });
        return ev;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        using var conn = await _db.CreateConnectionAsync(ct);
        var rows = await conn.ExecuteAsync("DELETE FROM events WHERE id = @Id", new { Id = id });
        return rows > 0;
    }
}


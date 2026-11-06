using Dapper;
using GigHaus.Domain.Entities;
using GigHaus.Infrastructure.Data;

namespace GigHaus.Infrastructure.Repositories;

public class SettlementSummaryRepository : ISettlementSummaryRepository
{
    private readonly IDbConnectionFactory _db;

    public SettlementSummaryRepository(IDbConnectionFactory db) => _db = db;

    public async Task<SettlementSummary?> GetByEventIdAsync(Guid eventId, CancellationToken ct = default)
    {
        using var conn = await _db.CreateConnectionAsync(ct);
        return await conn.QuerySingleOrDefaultAsync<SettlementSummary>(
            @"SELECT * FROM settlement_summaries WHERE event_id = @EventId",
            new { EventId = eventId });
    }

    public async Task<SettlementSummary> CreateOrUpdateAsync(SettlementSummary summary, CancellationToken ct = default)
    {
        using var conn = await _db.CreateConnectionAsync(ct);
        summary.UpdatedAt = DateTime.UtcNow;

        var rows = await conn.ExecuteAsync(
            @"INSERT INTO settlement_summaries (id, event_id, total_online_collected, total_door_cash_collected,
              total_other_collected, total_gross_collected, total_processor_fees, total_platform_fees,
              total_fees, net_attributable_to_artist, total_reservations, total_checked_in,
              total_guests, total_comps, is_finalized, payout_status, stripe_payout_id,
              created_at, updated_at)
              VALUES (@Id, @EventId, @TotalOnlineCollected, @TotalDoorCashCollected,
              @TotalOtherCollected, @TotalGrossCollected, @TotalProcessorFees, @TotalPlatformFees,
              @TotalFees, @NetAttributableToArtist, @TotalReservations, @TotalCheckedIn,
              @TotalGuests, @TotalComps, @IsFinalized, @PayoutStatus, @StripePayoutId,
              @CreatedAt, @UpdatedAt)
              ON CONFLICT (event_id) DO UPDATE SET
              total_online_collected = @TotalOnlineCollected,
              total_door_cash_collected = @TotalDoorCashCollected,
              total_other_collected = @TotalOtherCollected,
              total_gross_collected = @TotalGrossCollected,
              total_processor_fees = @TotalProcessorFees,
              total_platform_fees = @TotalPlatformFees,
              total_fees = @TotalFees,
              net_attributable_to_artist = @NetAttributableToArtist,
              total_reservations = @TotalReservations,
              total_checked_in = @TotalCheckedIn,
              total_guests = @TotalGuests,
              total_comps = @TotalComps,
              is_finalized = @IsFinalized,
              payout_status = @PayoutStatus,
              stripe_payout_id = @StripePayoutId,
              updated_at = @UpdatedAt",
            new
            {
                summary.Id, summary.EventId, summary.TotalOnlineCollected,
                summary.TotalDoorCashCollected, summary.TotalOtherCollected,
                summary.TotalGrossCollected, summary.TotalProcessorFees,
                summary.TotalPlatformFees, summary.TotalFees,
                summary.NetAttributableToArtist, summary.TotalReservations,
                summary.TotalCheckedIn, summary.TotalGuests, summary.TotalComps,
                summary.IsFinalized, summary.PayoutStatus, summary.StripePayoutId,
                summary.CreatedAt, summary.UpdatedAt
            });

        return summary;
    }
}


using Dapper;
using GigHaus.Domain.Entities;
using GigHaus.Infrastructure.Data;

namespace GigHaus.Infrastructure.Repositories;

public class PaymentRecordRepository : IPaymentRecordRepository
{
    private readonly IDbConnectionFactory _db;

    public PaymentRecordRepository(IDbConnectionFactory db) => _db = db;

    public async Task<IEnumerable<PaymentRecord>> GetByReservationIdAsync(Guid reservationId, CancellationToken ct = default)
    {
        using var conn = await _db.CreateConnectionAsync(ct);
        return await conn.QueryAsync<PaymentRecord>(
            @"SELECT * FROM payment_records WHERE reservation_id = @ReservationId ORDER BY created_at",
            new { ReservationId = reservationId });
    }

    public async Task<PaymentRecord?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        using var conn = await _db.CreateConnectionAsync(ct);
        return await conn.QuerySingleOrDefaultAsync<PaymentRecord>(
            @"SELECT * FROM payment_records WHERE id = @Id", new { Id = id });
    }

    public async Task<PaymentRecord?> GetByStripePaymentIntentIdAsync(string paymentIntentId, CancellationToken ct = default)
    {
        using var conn = await _db.CreateConnectionAsync(ct);
        return await conn.QuerySingleOrDefaultAsync<PaymentRecord>(
            @"SELECT * FROM payment_records WHERE stripe_payment_intent_id = @PaymentIntentId",
            new { PaymentIntentId = paymentIntentId });
    }

    public async Task<PaymentRecord> CreateAsync(PaymentRecord record, CancellationToken ct = default)
    {
        using var conn = await _db.CreateConnectionAsync(ct);
        await conn.ExecuteAsync(
            @"INSERT INTO payment_records (id, reservation_id, gross_amount, processor_fee_amount,
              platform_fee_amount, net_amount, attributable_to_artist_amount, payment_method_type,
              status, provider, provider_reference, stripe_payment_intent_id,
              stripe_connected_account_id, created_at, updated_at)
              VALUES (@Id, @ReservationId, @GrossAmount, @ProcessorFeeAmount,
              @PlatformFeeAmount, @NetAmount, @AttributableToArtistAmount, @PaymentMethodType,
              @Status, @Provider, @ProviderReference, @StripePaymentIntentId,
              @StripeConnectedAccountId, @CreatedAt, @UpdatedAt)",
            new
            {
                record.Id, record.ReservationId, record.GrossAmount, record.ProcessorFeeAmount,
                record.PlatformFeeAmount, record.NetAmount, record.AttributableToArtistAmount,
                PaymentMethodType = record.PaymentMethodType.ToString(),
                Status = record.Status.ToString(),
                record.Provider, record.ProviderReference, record.StripePaymentIntentId,
                record.StripeConnectedAccountId, record.CreatedAt, record.UpdatedAt
            });
        return record;
    }

    public async Task<PaymentRecord?> UpdateAsync(PaymentRecord record, CancellationToken ct = default)
    {
        using var conn = await _db.CreateConnectionAsync(ct);
        record.UpdatedAt = DateTime.UtcNow;
        var rows = await conn.ExecuteAsync(
            @"UPDATE payment_records SET gross_amount = @GrossAmount, processor_fee_amount = @ProcessorFeeAmount,
              platform_fee_amount = @PlatformFeeAmount, net_amount = @NetAmount,
              attributable_to_artist_amount = @AttributableToArtistAmount,
              payment_method_type = @PaymentMethodType, status = @Status,
              provider = @Provider, provider_reference = @ProviderReference,
              stripe_payment_intent_id = @StripePaymentIntentId,
              stripe_connected_account_id = @StripeConnectedAccountId,
              updated_at = @UpdatedAt
              WHERE id = @Id",
            new
            {
                record.Id, record.GrossAmount, record.ProcessorFeeAmount,
                record.PlatformFeeAmount, record.NetAmount, record.AttributableToArtistAmount,
                PaymentMethodType = record.PaymentMethodType.ToString(),
                Status = record.Status.ToString(),
                record.Provider, record.ProviderReference, record.StripePaymentIntentId,
                record.StripeConnectedAccountId, record.UpdatedAt
            });
        return rows > 0 ? record : null;
    }
}


using GigHaus.Domain.Entities;

namespace GigHaus.Infrastructure.Repositories;

public interface IPaymentRecordRepository
{
    Task<IEnumerable<PaymentRecord>> GetByReservationIdAsync(Guid reservationId, CancellationToken ct = default);
    Task<PaymentRecord?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PaymentRecord?> GetByStripePaymentIntentIdAsync(string paymentIntentId, CancellationToken ct = default);
    Task<PaymentRecord> CreateAsync(PaymentRecord record, CancellationToken ct = default);
    Task<PaymentRecord?> UpdateAsync(PaymentRecord record, CancellationToken ct = default);
}


using GigHaus.Domain.Entities;

namespace GigHaus.Infrastructure.Repositories;

public interface ISettlementSummaryRepository
{
    Task<SettlementSummary?> GetByEventIdAsync(Guid eventId, CancellationToken ct = default);
    Task<SettlementSummary> CreateOrUpdateAsync(SettlementSummary summary, CancellationToken ct = default);
}


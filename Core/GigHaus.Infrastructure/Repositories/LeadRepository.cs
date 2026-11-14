using Dapper;
using GigHaus.Domain.Entities;
using GigHaus.Domain.Enums;
using GigHaus.Infrastructure.Data;

namespace GigHaus.Infrastructure.Repositories;

public sealed class LeadRepository(IDbConnectionFactory db) : ILeadRepository
{
    public async Task<IReadOnlyList<Lead>> GetAllAsync(CancellationToken ct = default)
    {
        using var conn = await db.CreateConnectionAsync(ct).ConfigureAwait(false);
        var leads = await conn.QueryAsync<Lead>(new CommandDefinition(
            "SELECT * FROM leads WHERE NOT is_deleted ORDER BY created_at DESC",
            cancellationToken: ct)).ConfigureAwait(false);
        return leads.AsList();
    }

    public async Task<Lead?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        using var conn = await db.CreateConnectionAsync(ct).ConfigureAwait(false);
        return await conn.QuerySingleOrDefaultAsync<Lead>(new CommandDefinition(
            "SELECT * FROM leads WHERE id = @Id AND NOT is_deleted",
            new { Id = id }, cancellationToken: ct)).ConfigureAwait(false);
    }

    public async Task<Lead?> GetByInviteTokenHashAsync(string tokenHash, CancellationToken ct = default)
    {
        using var conn = await db.CreateConnectionAsync(ct).ConfigureAwait(false);
        return await conn.QuerySingleOrDefaultAsync<Lead>(new CommandDefinition(
            "SELECT * FROM leads WHERE invite_token_hash = @TokenHash AND NOT is_deleted",
            new { TokenHash = tokenHash }, cancellationToken: ct)).ConfigureAwait(false);
    }

    public async Task<Lead?> GetOpenByEmailAsync(string email, CancellationToken ct = default)
    {
        using var conn = await db.CreateConnectionAsync(ct).ConfigureAwait(false);
        return await conn.QueryFirstOrDefaultAsync<Lead>(new CommandDefinition(
            @"SELECT * FROM leads
              WHERE LOWER(email) = LOWER(@Email) AND status <> @SignedUp AND NOT is_deleted
              ORDER BY created_at DESC",
            new { Email = email, SignedUp = nameof(LeadStatus.SignedUp) },
            cancellationToken: ct)).ConfigureAwait(false);
    }

    public async Task<bool> SetInvitedAsync(Guid id, string tokenHash, DateTime expiresAt, CancellationToken ct = default)
    {
        using var conn = await db.CreateConnectionAsync(ct).ConfigureAwait(false);
        var rows = await conn.ExecuteAsync(new CommandDefinition(
            @"UPDATE leads SET status = @Invited, invite_token_hash = @TokenHash, invited_at = NOW(),
                invite_expires_at = @ExpiresAt, updated_at = NOW()
              WHERE id = @Id AND status <> @SignedUp AND NOT is_deleted",
            new
            {
                Id = id, TokenHash = tokenHash, ExpiresAt = expiresAt,
                Invited = nameof(LeadStatus.Invited), SignedUp = nameof(LeadStatus.SignedUp)
            }, cancellationToken: ct)).ConfigureAwait(false);
        return rows > 0;
    }

    public async Task<Lead> CreateAsync(Lead lead, CancellationToken ct = default)
    {
        using var conn = await db.CreateConnectionAsync(ct).ConfigureAwait(false);
        await conn.ExecuteAsync(new CommandDefinition(
            @"INSERT INTO leads (id, name, email, phone, city, state, estimated_capacity, message, status,
                notes, created_at, updated_at)
              VALUES (@Id, @Name, @Email, @Phone, @City, @State, @EstimatedCapacity, @Message, @Status,
                @Notes, @CreatedAt, @UpdatedAt)",
            new
            {
                lead.Id, lead.Name, lead.Email, lead.Phone, lead.City, lead.State,
                lead.EstimatedCapacity, lead.Message, Status = lead.Status.ToString(),
                lead.Notes, lead.CreatedAt, lead.UpdatedAt
            }, cancellationToken: ct)).ConfigureAwait(false);
        return lead;
    }

    public async Task UpdateAsync(Lead lead, CancellationToken ct = default)
    {
        lead.UpdatedAt = DateTime.UtcNow;
        using var conn = await db.CreateConnectionAsync(ct).ConfigureAwait(false);
        await conn.ExecuteAsync(new CommandDefinition(
            "UPDATE leads SET status = @Status, notes = @Notes, updated_at = @UpdatedAt WHERE id = @Id AND NOT is_deleted",
            new { lead.Id, Status = lead.Status.ToString(), lead.Notes, lead.UpdatedAt },
            cancellationToken: ct)).ConfigureAwait(false);
    }

    public async Task<bool> SoftDeleteAsync(Guid id, CancellationToken ct = default)
    {
        using var conn = await db.CreateConnectionAsync(ct).ConfigureAwait(false);
        var rows = await conn.ExecuteAsync(new CommandDefinition(
            "UPDATE leads SET is_deleted = TRUE, deleted_at = NOW(), updated_at = NOW() WHERE id = @Id AND NOT is_deleted",
            new { Id = id }, cancellationToken: ct)).ConfigureAwait(false);
        return rows > 0;
    }

    public async Task<int> MarkSignedUpAsync(string email, CancellationToken ct = default)
    {
        using var conn = await db.CreateConnectionAsync(ct).ConfigureAwait(false);
        return await conn.ExecuteAsync(new CommandDefinition(
            @"UPDATE leads SET status = @SignedUp, invite_token_hash = NULL, updated_at = NOW()
              WHERE LOWER(email) = LOWER(@Email) AND status <> @SignedUp AND NOT is_deleted",
            new { Email = email, SignedUp = nameof(LeadStatus.SignedUp) },
            cancellationToken: ct)).ConfigureAwait(false);
    }
}

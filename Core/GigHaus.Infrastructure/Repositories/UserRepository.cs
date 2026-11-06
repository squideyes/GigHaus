using Dapper;
using GigHaus.Domain.Entities;
using GigHaus.Infrastructure.Data;

namespace GigHaus.Infrastructure.Repositories;

public sealed class UserRepository(IDbConnectionFactory db) : IUserRepository
{
    public async Task<ApplicationUser?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        using var conn = await db.CreateConnectionAsync(ct).ConfigureAwait(false);
        return await conn.QuerySingleOrDefaultAsync<ApplicationUser>(new CommandDefinition(
            "SELECT * FROM users WHERE id = @Id AND NOT is_deleted",
            new { Id = id }, cancellationToken: ct)).ConfigureAwait(false);
    }

    public async Task<ApplicationUser?> GetByNormalizedEmailAsync(string normalizedEmail, CancellationToken ct = default)
    {
        using var conn = await db.CreateConnectionAsync(ct).ConfigureAwait(false);
        return await conn.QuerySingleOrDefaultAsync<ApplicationUser>(new CommandDefinition(
            "SELECT * FROM users WHERE normalized_email = @NormalizedEmail AND NOT is_deleted",
            new { NormalizedEmail = normalizedEmail }, cancellationToken: ct)).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<ApplicationUser>> GetByRoleAsync(string role, CancellationToken ct = default)
    {
        using var conn = await db.CreateConnectionAsync(ct).ConfigureAwait(false);
        var users = await conn.QueryAsync<ApplicationUser>(new CommandDefinition(
            "SELECT * FROM users WHERE role = @Role AND NOT is_deleted ORDER BY display_name",
            new { Role = role }, cancellationToken: ct)).ConfigureAwait(false);
        return users.AsList();
    }

    public async Task<int> CountByRoleAsync(string role, CancellationToken ct = default)
    {
        using var conn = await db.CreateConnectionAsync(ct).ConfigureAwait(false);
        return await conn.ExecuteScalarAsync<int>(new CommandDefinition(
            "SELECT COUNT(*) FROM users WHERE role = @Role AND NOT is_deleted",
            new { Role = role }, cancellationToken: ct)).ConfigureAwait(false);
    }

    public async Task CreateAsync(ApplicationUser user, CancellationToken ct = default)
    {
        using var conn = await db.CreateConnectionAsync(ct).ConfigureAwait(false);
        await conn.ExecuteAsync(new CommandDefinition(
            @"INSERT INTO users (id, email, normalized_email, display_name, password_hash, security_stamp,
                email_confirmed, lockout_end_utc, lockout_enabled, access_failed_count, role, host_id,
                created_at, updated_at)
              VALUES (@Id, @Email, @NormalizedEmail, @DisplayName, @PasswordHash, @SecurityStamp,
                @EmailConfirmed, @LockoutEndUtc, @LockoutEnabled, @AccessFailedCount, @Role, @HostId,
                @CreatedAt, @UpdatedAt)",
            user, cancellationToken: ct)).ConfigureAwait(false);
    }

    public async Task UpdateAsync(ApplicationUser user, CancellationToken ct = default)
    {
        user.UpdatedAt = DateTime.UtcNow;
        using var conn = await db.CreateConnectionAsync(ct).ConfigureAwait(false);
        await conn.ExecuteAsync(new CommandDefinition(
            @"UPDATE users SET email = @Email, normalized_email = @NormalizedEmail, display_name = @DisplayName,
                password_hash = @PasswordHash, security_stamp = @SecurityStamp, email_confirmed = @EmailConfirmed,
                lockout_end_utc = @LockoutEndUtc, lockout_enabled = @LockoutEnabled,
                access_failed_count = @AccessFailedCount, role = @Role, host_id = @HostId, updated_at = @UpdatedAt
              WHERE id = @Id AND NOT is_deleted",
            user, cancellationToken: ct)).ConfigureAwait(false);
    }

    public async Task<bool> SoftDeleteAsync(Guid id, CancellationToken ct = default)
    {
        using var conn = await db.CreateConnectionAsync(ct).ConfigureAwait(false);
        var rows = await conn.ExecuteAsync(new CommandDefinition(
            "UPDATE users SET is_deleted = TRUE, deleted_at = NOW(), updated_at = NOW() WHERE id = @Id AND NOT is_deleted",
            new { Id = id }, cancellationToken: ct)).ConfigureAwait(false);
        return rows > 0;
    }
}

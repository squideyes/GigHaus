using Dapper;
using GigHaus.Domain.Entities;
using GigHaus.Infrastructure.Data;

namespace GigHaus.Infrastructure.Repositories;

public class ArtistRepository : IArtistRepository
{
    private readonly IDbConnectionFactory _db;

    public ArtistRepository(IDbConnectionFactory db) => _db = db;

    public async Task<IEnumerable<Artist>> GetAllAsync(CancellationToken ct = default)
    {
        using var conn = await _db.CreateConnectionAsync(ct);
        return await conn.QueryAsync<Artist>(
            "SELECT id, name, bio, email, phone, website, stripe_connect_account_id, created_at, updated_at FROM artists ORDER BY name");
    }

    public async Task<Artist?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        using var conn = await _db.CreateConnectionAsync(ct);
        return await conn.QuerySingleOrDefaultAsync<Artist>(
            "SELECT id, name, bio, email, phone, website, stripe_connect_account_id, created_at, updated_at FROM artists WHERE id = @Id",
            new { Id = id });
    }

    public async Task<Artist> CreateAsync(Artist artist, CancellationToken ct = default)
    {
        using var conn = await _db.CreateConnectionAsync(ct);
        await conn.ExecuteAsync(
            @"INSERT INTO artists (id, name, bio, email, phone, website, stripe_connect_account_id, created_at, updated_at)
              VALUES (@Id, @Name, @Bio, @Email, @Phone, @Website, @StripeConnectAccountId, @CreatedAt, @UpdatedAt)",
            artist);
        return artist;
    }

    public async Task<Artist?> UpdateAsync(Guid id, Artist input, CancellationToken ct = default)
    {
        using var conn = await _db.CreateConnectionAsync(ct);
        var rows = await conn.ExecuteAsync(
            @"UPDATE artists SET name = @Name, bio = @Bio, email = @Email, phone = @Phone,
              website = @Website, updated_at = @UpdatedAt WHERE id = @Id",
            new { input.Name, input.Bio, input.Email, input.Phone, input.Website, UpdatedAt = DateTime.UtcNow, Id = id });
        if (rows == 0) return null;
        return await GetByIdAsync(id, ct);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        using var conn = await _db.CreateConnectionAsync(ct);
        var rows = await conn.ExecuteAsync("DELETE FROM artists WHERE id = @Id", new { Id = id });
        return rows > 0;
    }
}


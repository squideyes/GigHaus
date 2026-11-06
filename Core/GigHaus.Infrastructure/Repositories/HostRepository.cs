using Dapper;
using GigHaus.Domain.Entities;
using GigHaus.Infrastructure.Data;

namespace GigHaus.Infrastructure.Repositories;

public class HostRepository : IHostRepository
{
    private readonly IDbConnectionFactory _db;

    public HostRepository(IDbConnectionFactory db) => _db = db;

    public async Task<IEnumerable<Host>> GetAllAsync(CancellationToken ct = default)
    {
        using var conn = await _db.CreateConnectionAsync(ct);
        return await conn.QueryAsync<Host>(
            "SELECT id, name, email, phone, address, city, state, zip_code, max_capacity, venue_description, created_at, updated_at FROM hosts ORDER BY name");
    }

    public async Task<Host?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        using var conn = await _db.CreateConnectionAsync(ct);
        return await conn.QuerySingleOrDefaultAsync<Host>(
            "SELECT id, name, email, phone, address, city, state, zip_code, max_capacity, venue_description, created_at, updated_at FROM hosts WHERE id = @Id",
            new { Id = id });
    }

    public async Task<Host> CreateAsync(Host host, CancellationToken ct = default)
    {
        using var conn = await _db.CreateConnectionAsync(ct);
        await conn.ExecuteAsync(
            @"INSERT INTO hosts (id, name, email, phone, address, city, state, zip_code, max_capacity, venue_description, created_at, updated_at)
              VALUES (@Id, @Name, @Email, @Phone, @Address, @City, @State, @ZipCode, @MaxCapacity, @VenueDescription, @CreatedAt, @UpdatedAt)",
            host);
        return host;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        using var conn = await _db.CreateConnectionAsync(ct);
        var rows = await conn.ExecuteAsync("DELETE FROM hosts WHERE id = @Id", new { Id = id });
        return rows > 0;
    }
}


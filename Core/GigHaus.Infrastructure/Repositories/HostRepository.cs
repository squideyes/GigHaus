using Dapper;
using GigHaus.Domain.Entities;
using GigHaus.Domain.Enums;
using GigHaus.Infrastructure.Data;

namespace GigHaus.Infrastructure.Repositories;

public class HostRepository : IHostRepository
{
    private const string Columns =
        "id, name, email, phone, address, city, state, zip_code, max_capacity, venue_description, status, approved_at, created_at, updated_at";

    private readonly IDbConnectionFactory _db;

    public HostRepository(IDbConnectionFactory db) => _db = db;

    public async Task<IEnumerable<Host>> GetAllAsync(CancellationToken ct = default)
    {
        using var conn = await _db.CreateConnectionAsync(ct);
        return await conn.QueryAsync<Host>($"SELECT {Columns} FROM hosts ORDER BY name");
    }

    public async Task<Host?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        using var conn = await _db.CreateConnectionAsync(ct);
        return await conn.QuerySingleOrDefaultAsync<Host>(
            $"SELECT {Columns} FROM hosts WHERE id = @Id",
            new { Id = id });
    }

    public async Task<Host?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        using var conn = await _db.CreateConnectionAsync(ct);
        return await conn.QueryFirstOrDefaultAsync<Host>(
            $"SELECT {Columns} FROM hosts WHERE LOWER(email) = LOWER(@Email)",
            new { Email = email });
    }

    public async Task<Host> CreateAsync(Host host, CancellationToken ct = default)
    {
        using var conn = await _db.CreateConnectionAsync(ct);
        await conn.ExecuteAsync(
            @"INSERT INTO hosts (id, name, email, phone, address, city, state, zip_code, max_capacity, venue_description, status, approved_at, created_at, updated_at)
              VALUES (@Id, @Name, @Email, @Phone, @Address, @City, @State, @ZipCode, @MaxCapacity, @VenueDescription, @Status, @ApprovedAt, @CreatedAt, @UpdatedAt)",
            new
            {
                host.Id, host.Name, host.Email, host.Phone, host.Address, host.City, host.State,
                host.ZipCode, host.MaxCapacity, host.VenueDescription,
                Status = host.Status.ToString(), host.ApprovedAt, host.CreatedAt, host.UpdatedAt
            });
        return host;
    }

    public async Task<Host?> UpdateAsync(Host host, CancellationToken ct = default)
    {
        host.UpdatedAt = DateTime.UtcNow;
        using var conn = await _db.CreateConnectionAsync(ct);
        var rows = await conn.ExecuteAsync(
            @"UPDATE hosts SET name = @Name, phone = @Phone, address = @Address, city = @City, state = @State,
                zip_code = @ZipCode, max_capacity = @MaxCapacity, venue_description = @VenueDescription,
                updated_at = @UpdatedAt
              WHERE id = @Id",
            new
            {
                host.Id, host.Name, host.Phone, host.Address, host.City, host.State,
                host.ZipCode, host.MaxCapacity, host.VenueDescription, host.UpdatedAt
            });
        return rows > 0 ? host : null;
    }

    public async Task<bool> SetStatusAsync(Guid id, HostStatus status, CancellationToken ct = default)
    {
        using var conn = await _db.CreateConnectionAsync(ct);
        var rows = await conn.ExecuteAsync(
            @"UPDATE hosts SET status = @Status, updated_at = NOW(),
                approved_at = CASE WHEN @Status = 'Approved' THEN COALESCE(approved_at, NOW()) ELSE approved_at END
              WHERE id = @Id",
            new { Id = id, Status = status.ToString() });
        return rows > 0;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        using var conn = await _db.CreateConnectionAsync(ct);
        var rows = await conn.ExecuteAsync("DELETE FROM hosts WHERE id = @Id", new { Id = id });
        return rows > 0;
    }
}

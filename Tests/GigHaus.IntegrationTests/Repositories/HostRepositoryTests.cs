using GigHaus.Domain.Enums;
using GigHaus.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace GigHaus.IntegrationTests.Repositories;

[Collection(PostgresCollection.Name)]
public sealed class HostRepositoryTests(PostgresFixture fixture)
{
    private readonly IHostRepository _hosts = fixture.Services.GetRequiredService<IHostRepository>();

    [Fact]
    public async Task Create_StoresStatus()
    {
        var host = await _hosts.CreateAsync(TestData.Host(HostStatus.Pending));

        var stored = await _hosts.GetByIdAsync(host.Id);

        stored!.Status.ShouldBe(HostStatus.Pending);
        stored.ApprovedAt.ShouldBeNull();
    }

    [Fact]
    public async Task SetStatus_Approved_StampsApprovedAt()
    {
        var host = await _hosts.CreateAsync(TestData.Host(HostStatus.Pending));

        await _hosts.SetStatusAsync(host.Id, HostStatus.Approved);

        var stored = await _hosts.GetByIdAsync(host.Id);
        stored!.Status.ShouldBe(HostStatus.Approved);
        stored.ApprovedAt.ShouldNotBeNull();
    }

    [Fact]
    public async Task SetStatus_Suspended_KeepsApprovedAt()
    {
        var host = await _hosts.CreateAsync(TestData.Host(HostStatus.Pending));
        await _hosts.SetStatusAsync(host.Id, HostStatus.Approved);

        await _hosts.SetStatusAsync(host.Id, HostStatus.Suspended);

        var stored = await _hosts.GetByIdAsync(host.Id);
        stored!.Status.ShouldBe(HostStatus.Suspended);
        stored.ApprovedAt.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetByEmail_IgnoresCase()
    {
        var host = await _hosts.CreateAsync(TestData.Host());

        var stored = await _hosts.GetByEmailAsync(host.Email.ToUpperInvariant());

        stored!.Id.ShouldBe(host.Id);
    }

    [Fact]
    public async Task Update_ChangesVenueButNotStatus()
    {
        var host = await _hosts.CreateAsync(TestData.Host(HostStatus.Pending));
        host.MaxCapacity = 45;
        host.Status = HostStatus.Approved;

        await _hosts.UpdateAsync(host);

        var stored = await _hosts.GetByIdAsync(host.Id);
        stored!.MaxCapacity.ShouldBe(45);
        stored.Status.ShouldBe(HostStatus.Pending);
    }
}

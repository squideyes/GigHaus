using GigHaus.Domain.Entities;
using GigHaus.Domain.Enums;
using GigHaus.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace GigHaus.IntegrationTests.Repositories;

[Collection(PostgresCollection.Name)]
public sealed class LeadRepositoryTests(PostgresFixture fixture)
{
    private const string LeadName = "Dana Fox";
    private const string Notes = "Called on Tuesday";

    private readonly ILeadRepository _leads = fixture.Services.GetRequiredService<ILeadRepository>();

    [Fact]
    public async Task Create_ThenGetAll_ReturnsTheLead()
    {
        var lead = await _leads.CreateAsync(NewLead());

        var all = await _leads.GetAllAsync();

        var stored = all.Single(l => l.Id == lead.Id);
        stored.Status.ShouldBe(LeadStatus.New);
        stored.EstimatedCapacity.ShouldBe(25);
    }

    [Fact]
    public async Task Update_SavesStatusAndNotes()
    {
        var lead = await _leads.CreateAsync(NewLead());
        lead.Status = LeadStatus.Contacted;
        lead.Notes = Notes;

        await _leads.UpdateAsync(lead);

        var stored = (await _leads.GetAllAsync()).Single(l => l.Id == lead.Id);
        stored.Status.ShouldBe(LeadStatus.Contacted);
        stored.Notes.ShouldBe(Notes);
    }

    [Fact]
    public async Task SoftDelete_HidesTheLead()
    {
        var lead = await _leads.CreateAsync(NewLead());

        await _leads.SoftDeleteAsync(lead.Id);

        (await _leads.GetAllAsync()).ShouldNotContain(l => l.Id == lead.Id);
    }

    [Fact]
    public async Task MarkSignedUp_MatchesEmailIgnoringCase()
    {
        var lead = await _leads.CreateAsync(NewLead());

        var updated = await _leads.MarkSignedUpAsync(lead.Email.ToUpperInvariant());

        updated.ShouldBe(1);
        (await _leads.GetAllAsync()).Single(l => l.Id == lead.Id).Status.ShouldBe(LeadStatus.SignedUp);
    }

    private static Lead NewLead() => new()
    {
        Name = LeadName,
        Email = TestData.UniqueEmail(),
        EstimatedCapacity = 25
    };
}

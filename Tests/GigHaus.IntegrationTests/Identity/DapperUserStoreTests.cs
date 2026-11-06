using System.Security.Claims;
using GigHaus.Domain;
using GigHaus.Domain.Entities;
using GigHaus.Infrastructure.Identity;
using GigHaus.Infrastructure.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace GigHaus.IntegrationTests.Identity;

[Collection(PostgresCollection.Name)]
public sealed class DapperUserStoreTests(PostgresFixture fixture) : IDisposable
{
    private const string DisplayName = "Alex Chen";
    private const string NewPassword = "a-different-long-password";

    private readonly IServiceScope _scope = fixture.Services.CreateScope();

    private UserManager<ApplicationUser> Users => _scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    [Fact]
    public async Task Create_ThenFindByEmail_IgnoresCase()
    {
        var user = await CreateUserAsync();

        var found = await Users.FindByEmailAsync(user.Email.ToUpperInvariant());

        found!.Id.ShouldBe(user.Id);
        found.DisplayName.ShouldBe(DisplayName);
    }

    [Fact]
    public async Task CheckPassword_OnlyAcceptsTheRightPassword()
    {
        var user = await CreateUserAsync();

        (await Users.CheckPasswordAsync(user, TestData.Password)).ShouldBeTrue();
        (await Users.CheckPasswordAsync(user, NewPassword)).ShouldBeFalse();
    }

    [Fact]
    public async Task Create_RejectsDuplicateEmail()
    {
        var user = await CreateUserAsync();

        var duplicate = new ApplicationUser { Email = user.Email, DisplayName = DisplayName };
        var result = await Users.CreateAsync(duplicate, TestData.Password);

        result.Succeeded.ShouldBeFalse();
    }

    [Fact]
    public async Task ChangePassword_PersistsAndChangesSecurityStamp()
    {
        var user = await CreateUserAsync();
        var stampBefore = user.SecurityStamp;

        var result = await Users.ChangePasswordAsync(user, TestData.Password, NewPassword);

        result.Succeeded.ShouldBeTrue();
        var stored = await Users.FindByIdAsync(user.Id.ToString());
        (await Users.CheckPasswordAsync(stored!, NewPassword)).ShouldBeTrue();
        stored!.SecurityStamp.ShouldNotBe(stampBefore);
    }

    [Fact]
    public async Task Lockout_RoundTripsThroughTheDatabase()
    {
        var user = await CreateUserAsync();
        var until = DateTimeOffset.UtcNow.AddMinutes(15);

        await Users.SetLockoutEndDateAsync(user, until);

        var stored = await Users.FindByIdAsync(user.Id.ToString());
        (await Users.IsLockedOutAsync(stored!)).ShouldBeTrue();
    }

    [Fact]
    public async Task IsInRole_MatchesTheUsersOneRole()
    {
        var user = await CreateUserAsync(UserRoles.Admin);

        (await Users.IsInRoleAsync(user, UserRoles.Admin)).ShouldBeTrue();
        (await Users.IsInRoleAsync(user, UserRoles.Host)).ShouldBeFalse();
    }

    [Fact]
    public async Task Claims_IncludeRoleAndHostProfile()
    {
        var hostId = (await _scope.ServiceProvider.GetRequiredService<IHostRepository>().CreateAsync(TestData.Host())).Id;
        var user = await CreateUserAsync(UserRoles.Host, hostId);
        var factory = _scope.ServiceProvider.GetRequiredService<IUserClaimsPrincipalFactory<ApplicationUser>>();

        var principal = await factory.CreateAsync(user);

        principal.IsInRole(UserRoles.Host).ShouldBeTrue();
        principal.FindFirstValue(GigHausClaims.HostId).ShouldBe(hostId.ToString());
        principal.FindFirstValue(GigHausClaims.DisplayName).ShouldBe(DisplayName);
    }

    [Fact]
    public async Task SoftDeletedUser_CanNoLongerBeFound_AndEmailCanBeReused()
    {
        var user = await CreateUserAsync();
        var repository = _scope.ServiceProvider.GetRequiredService<IUserRepository>();

        await repository.SoftDeleteAsync(user.Id);

        (await Users.FindByEmailAsync(user.Email)).ShouldBeNull();
        var replacement = new ApplicationUser { Email = user.Email, DisplayName = DisplayName };
        (await Users.CreateAsync(replacement, TestData.Password)).Succeeded.ShouldBeTrue();
    }

    public void Dispose() => _scope.Dispose();

    private async Task<ApplicationUser> CreateUserAsync(string role = UserRoles.Host, Guid? hostId = null)
    {
        var user = new ApplicationUser
        {
            Email = TestData.UniqueEmail(),
            DisplayName = DisplayName,
            Role = role,
            HostId = hostId
        };

        var result = await Users.CreateAsync(user, TestData.Password);
        result.Succeeded.ShouldBeTrue();
        return user;
    }
}

using GigHaus.Domain;
using GigHaus.Domain.Entities;
using GigHaus.Infrastructure.Repositories;
using Microsoft.AspNetCore.Identity;

namespace GigHaus.Web.Identity;

/// <summary>
/// Creates the admin account from Admin:Email and Admin:Password when none
/// exists yet.
/// </summary>
public sealed class AdminSeeder(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<AdminSeeder> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var email = configuration["Admin:Email"];
        var password = configuration["Admin:Password"];

        await using var scope = scopeFactory.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IUserRepository>();

        if (await repository.CountByRoleAsync(UserRoles.Admin, cancellationToken) > 0)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning("No admin account exists. Set Admin:Email and Admin:Password to create it");
            return;
        }

        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var created = await users.CreateAsync(
            new ApplicationUser
            {
                Email = email,
                DisplayName = "GigHaus Admin",
                Role = UserRoles.Admin,
                EmailConfirmed = true
            },
            password);

        if (created.Succeeded)
        {
            logger.LogInformation("Created the admin account");
        }
        else
        {
            logger.LogError("Could not create the admin account: {Errors}",
                string.Join(" ", created.Errors.Select(e => e.Description)));
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

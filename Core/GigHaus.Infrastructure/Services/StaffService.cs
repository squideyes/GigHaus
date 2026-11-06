using GigHaus.Domain;
using GigHaus.Domain.Entities;
using GigHaus.Infrastructure.Repositories;
using Microsoft.AspNetCore.Identity;

namespace GigHaus.Infrastructure.Services;

public sealed class StaffService(UserManager<ApplicationUser> users, IUserRepository repository) : IStaffService
{
    public Task<IReadOnlyList<ApplicationUser>> GetStaffAsync(CancellationToken ct = default) =>
        repository.GetByRoleAsync(UserRoles.Admin, ct);

    public async Task<Result<ApplicationUser>> AddAsync(string displayName, string email, string password, CancellationToken ct = default)
    {
        var address = email.Trim();
        if (await users.FindByEmailAsync(address).ConfigureAwait(false) is not null)
        {
            return Result<ApplicationUser>.Fail("An account with that email already exists.");
        }

        var user = new ApplicationUser
        {
            Email = address,
            DisplayName = displayName.Trim(),
            Role = UserRoles.Admin,
            EmailConfirmed = true
        };

        var created = await users.CreateAsync(user, password).ConfigureAwait(false);
        return created.Succeeded
            ? Result<ApplicationUser>.Ok(user)
            : Result<ApplicationUser>.Fail(string.Join(" ", created.Errors.Select(e => e.Description)));
    }

    public async Task<Result<ApplicationUser>> RemoveAsync(Guid staffId, Guid actingUserId, CancellationToken ct = default)
    {
        if (staffId == actingUserId)
        {
            return Result<ApplicationUser>.Fail("You can't remove your own account.");
        }

        var user = await repository.GetByIdAsync(staffId, ct).ConfigureAwait(false);
        if (user is null || user.Role != UserRoles.Admin)
        {
            return Result<ApplicationUser>.Fail("Staff member not found.");
        }

        await repository.SoftDeleteAsync(staffId, ct).ConfigureAwait(false);
        return Result<ApplicationUser>.Ok(user);
    }
}

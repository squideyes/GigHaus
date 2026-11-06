using System.Security.Claims;
using GigHaus.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace GigHaus.Infrastructure.Identity;

public static class GigHausClaims
{
    public const string HostId = "gighaus:host_id";
    public const string DisplayName = "gighaus:display_name";
}

/// <summary>
/// Adds the user's role, display name and host profile to the sign-in cookie.
/// </summary>
public sealed class GigHausClaimsFactory(
    UserManager<ApplicationUser> userManager,
    IOptions<IdentityOptions> options)
    : UserClaimsPrincipalFactory<ApplicationUser>(userManager, options)
{
    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user).ConfigureAwait(false);

        identity.AddClaim(new Claim(Options.ClaimsIdentity.RoleClaimType, user.Role));
        identity.AddClaim(new Claim(GigHausClaims.DisplayName, user.DisplayName));

        if (user.HostId is { } hostId)
        {
            identity.AddClaim(new Claim(GigHausClaims.HostId, hostId.ToString()));
        }

        return identity;
    }
}

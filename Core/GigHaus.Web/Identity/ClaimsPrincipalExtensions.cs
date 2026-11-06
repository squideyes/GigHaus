using System.Security.Claims;
using GigHaus.Domain;
using GigHaus.Infrastructure.Identity;

namespace GigHaus.Web.Identity;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user) =>
        Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : Guid.Empty;

    public static Guid GetHostId(this ClaimsPrincipal user) =>
        Guid.TryParse(user.FindFirstValue(GigHausClaims.HostId), out var id) ? id : Guid.Empty;

    public static string GetDisplayName(this ClaimsPrincipal user) =>
        user.FindFirstValue(GigHausClaims.DisplayName) ?? string.Empty;

    /// <summary>Where this person lands after signing in.</summary>
    public static string GetHomePath(this ClaimsPrincipal user) =>
        user.IsInRole(UserRoles.Admin) ? "/admin" : "/portal";
}

using GigHaus.Web.Identity;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace GigHaus.Web.Components.Pages.Portal;

/// <summary>
/// Base for host portal pages. The host always comes from the signed-in user, never
/// from the URL, so one host can't reach another's data by editing a link.
/// </summary>
public abstract class PortalPage : ComponentBase
{
    [CascadingParameter] private Task<AuthenticationState> AuthenticationState { get; set; } = default!;

    protected async Task<Guid> GetHostIdAsync() => (await AuthenticationState).User.GetHostId();
}

using System.Net;
using GigHaus.Domain;
using GigHaus.Domain.Entities;
using GigHaus.Domain.Enums;
using GigHaus.Infrastructure.Email;
using GigHaus.Infrastructure.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace GigHaus.Infrastructure.Services;

public sealed class HostOnboardingService(
    UserManager<ApplicationUser> users,
    IHostRepository hosts,
    ILeadRepository leads,
    IEmailService email,
    ILogger<HostOnboardingService> logger) : IHostOnboardingService
{
    private const string DuplicateEmail = "An account with that email already exists. Try signing in instead.";

    public async Task<Result<ApplicationUser>> SignUpAsync(HostSignup signup, CancellationToken ct = default)
    {
        var emailAddress = signup.Email.Trim();

        if (await users.FindByEmailAsync(emailAddress).ConfigureAwait(false) is not null
            || await hosts.GetByEmailAsync(emailAddress, ct).ConfigureAwait(false) is not null)
        {
            return Result<ApplicationUser>.Fail(DuplicateEmail);
        }

        var host = new Host
        {
            Name = signup.Name.Trim(),
            Email = emailAddress,
            Phone = signup.Phone?.Trim(),
            Address = signup.Address.Trim(),
            City = signup.City.Trim(),
            State = signup.State.Trim(),
            ZipCode = signup.ZipCode.Trim(),
            MaxCapacity = signup.MaxCapacity,
            VenueDescription = signup.VenueDescription?.Trim(),
            Status = HostStatus.Pending
        };

        await hosts.CreateAsync(host, ct).ConfigureAwait(false);

        var user = new ApplicationUser
        {
            Email = emailAddress,
            DisplayName = host.Name,
            Role = UserRoles.Host,
            HostId = host.Id
        };

        var created = await users.CreateAsync(user, signup.Password).ConfigureAwait(false);
        if (!created.Succeeded)
        {
            await hosts.DeleteAsync(host.Id, ct).ConfigureAwait(false);
            return Result<ApplicationUser>.Fail(string.Join(" ", created.Errors.Select(e => e.Description)));
        }

        await leads.MarkSignedUpAsync(emailAddress, ct).ConfigureAwait(false);
        return Result<ApplicationUser>.Ok(user);
    }

    public async Task<Result<Host>> SetStatusAsync(Guid hostId, HostStatus status, string portalUrl, CancellationToken ct = default)
    {
        var host = await hosts.GetByIdAsync(hostId, ct).ConfigureAwait(false);
        if (host is null)
        {
            return Result<Host>.Fail("Host not found.");
        }

        var wasApproved = host.Status == HostStatus.Approved;
        await hosts.SetStatusAsync(hostId, status, ct).ConfigureAwait(false);
        host.Status = status;

        if (status == HostStatus.Approved && !wasApproved)
        {
            await SendApprovedAsync(host, portalUrl, ct).ConfigureAwait(false);
        }

        return Result<Host>.Ok(host);
    }

    private async Task SendApprovedAsync(Host host, string portalUrl, CancellationToken ct)
    {
        var html =
            $"""
            <p>Hi {WebUtility.HtmlEncode(host.Name)},</p>
            <p>Your venue is approved, so you can publish your first show on GigHaus.</p>
            <p><a href="{WebUtility.HtmlEncode(portalUrl)}">Go to your host portal</a></p>
            """;

        try
        {
            await email.SendEmailAsync(new EmailMessage(host.Email, "Your GigHaus venue is approved", html), ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Could not send approval email to host {HostId}", host.Id);
        }
    }
}

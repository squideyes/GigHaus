using System.Net;
using System.Security.Cryptography;
using System.Text;
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
    private const string InvalidInvite = "This invitation link isn't valid any more. Ask us for a new one.";
    private const string LeadGone = "That request no longer exists.";

    /// <summary>How long a sign-up link works.</summary>
    public static readonly TimeSpan InviteLifetime = TimeSpan.FromDays(14);

    public async Task<Result<Lead>> InviteAsync(Guid leadId, string signupUrl, CancellationToken ct = default)
    {
        var lead = await leads.GetByIdAsync(leadId, ct).ConfigureAwait(false);
        if (lead is null)
        {
            return Result<Lead>.Fail(LeadGone);
        }

        if (lead.Status == LeadStatus.SignedUp
            || await users.FindByEmailAsync(lead.Email).ConfigureAwait(false) is not null)
        {
            return Result<Lead>.Fail("That email already has an account.");
        }

        var token = Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(32));
        var expiresAt = DateTime.UtcNow.Add(InviteLifetime);
        if (!await leads.SetInvitedAsync(lead.Id, HashToken(token), expiresAt, ct).ConfigureAwait(false))
        {
            return Result<Lead>.Fail(LeadGone);
        }

        lead.Status = LeadStatus.Invited;
        lead.InvitedAt = DateTime.UtcNow;
        lead.InviteExpiresAt = expiresAt;

        var link = $"{signupUrl}{(signupUrl.Contains('?') ? '&' : '?')}invite={token}";
        var html =
            $"""
            <p>Hi {WebUtility.HtmlEncode(lead.Name)},</p>
            <p>You're invited to host house concerts with GigHaus. It's free.</p>
            <p><a href="{WebUtility.HtmlEncode(link)}">Set up your account</a></p>
            <p>This link is just for you and works until {expiresAt:MMMM d, yyyy}.</p>
            """;

        try
        {
            await email.SendEmailAsync(new EmailMessage(lead.Email, "Your invitation to GigHaus", html), ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Could not send invitation email for lead {LeadId}", lead.Id);
            return Result<Lead>.Fail("The invitation was saved, but the email couldn't be sent. Try inviting again.");
        }

        return Result<Lead>.Ok(lead);
    }

    public async Task<Result<Lead>> InviteNewAsync(string name, string email, string signupUrl, CancellationToken ct = default)
    {
        var emailAddress = email.Trim();
        var lead = await leads.GetOpenByEmailAsync(emailAddress, ct).ConfigureAwait(false)
            ?? await leads.CreateAsync(new Lead { Name = name.Trim(), Email = emailAddress }, ct).ConfigureAwait(false);

        return await InviteAsync(lead.Id, signupUrl, ct).ConfigureAwait(false);
    }

    public async Task<Lead?> FindInvitationAsync(string? inviteToken, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(inviteToken))
        {
            return null;
        }

        var lead = await leads.GetByInviteTokenHashAsync(HashToken(inviteToken), ct).ConfigureAwait(false);
        return lead is { Status: LeadStatus.Invited } && lead.InviteExpiresAt > DateTime.UtcNow ? lead : null;
    }

    public async Task<Result<ApplicationUser>> SignUpAsync(HostSignup signup, CancellationToken ct = default)
    {
        var invitation = await FindInvitationAsync(signup.InviteToken, ct).ConfigureAwait(false);
        if (invitation is null)
        {
            return Result<ApplicationUser>.Fail(InvalidInvite);
        }

        // The account gets the invited email, so a link can't be used to sign up someone else.
        var emailAddress = invitation.Email.Trim();

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
            // The invitation was the review.
            Status = HostStatus.Approved,
            ApprovedAt = DateTime.UtcNow
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

    private static string HashToken(string token) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(token.Trim())));

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

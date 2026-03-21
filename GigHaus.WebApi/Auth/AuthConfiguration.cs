using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace GigHaus.WebApi.Auth;

/// <summary>
/// Configures JWT Bearer authentication with role-based authorization policies.
/// In development, a symmetric key is used with a local token endpoint.
/// In production, replace with a real identity provider (e.g., Entra ID, Auth0).
/// </summary>
public static class AuthConfiguration
{
    public const string PolicyAdmin = "Admin";
    public const string PolicyHost = "HostOrAdmin";
    public const string PolicyArtist = "ArtistOrAdmin";
    public const string PolicyAuthenticated = "Authenticated";

    public const string RoleAdmin = "Admin";
    public const string RoleHost = "Host";
    public const string RoleArtist = "Artist";
    public const string RoleAttendee = "Attendee";

    /// <summary>
    /// Dev-mode signing key. In production, use a real secret from configuration / key vault.
    /// </summary>
    internal static string GetDevSigningKey(IConfiguration config)
        => config["Jwt:Key"] ?? "GigHaus-Dev-Signing-Key-Replace-In-Production-Min-32-Chars!!";

    internal static string GetIssuer(IConfiguration config)
        => config["Jwt:Issuer"] ?? "GigHaus";

    internal static string GetAudience(IConfiguration config)
        => config["Jwt:Audience"] ?? "GigHaus-API";

    public static IServiceCollection AddGigHausAuth(this IServiceCollection services, IConfiguration config)
    {
        var key = Encoding.UTF8.GetBytes(GetDevSigningKey(config));

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = GetIssuer(config),
                    ValidAudience = GetAudience(config),
                    IssuerSigningKey = new SymmetricSecurityKey(key)
                };

                // Allow SignalR to receive tokens via query string
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;
                        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                        {
                            context.Token = accessToken;
                        }
                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorizationBuilder()
            .AddPolicy(PolicyAdmin, p => p.RequireRole(RoleAdmin))
            .AddPolicy(PolicyHost, p => p.RequireRole(RoleHost, RoleAdmin))
            .AddPolicy(PolicyArtist, p => p.RequireRole(RoleArtist, RoleAdmin))
            .AddPolicy(PolicyAuthenticated, p => p.RequireAuthenticatedUser());

        return services;
    }

    /// <summary>
    /// Generates a JWT for development/testing. NOT for production use.
    /// </summary>
    public static string GenerateDevToken(IConfiguration config, string userId, string email, string name, string[] roles)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(GetDevSigningKey(config)));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId),
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtRegisteredClaimNames.Name, name),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        var token = new JwtSecurityToken(
            issuer: GetIssuer(config),
            audience: GetAudience(config),
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

/// <summary>Request body for the dev token endpoint.</summary>
public record DevTokenRequest(string Email, string Name, string[] Roles);


using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GigHaus.WebApi.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace GigHaus.Tests.Auth;

public class AuthConfigurationTests
{
    private static IConfiguration CreateConfig(string? key = null, string? issuer = null, string? audience = null)
    {
        var data = new Dictionary<string, string?>();
        if (key is not null) data["Jwt:Key"] = key;
        if (issuer is not null) data["Jwt:Issuer"] = issuer;
        if (audience is not null) data["Jwt:Audience"] = audience;
        return new ConfigurationBuilder().AddInMemoryCollection(data).Build();
    }

    [Fact]
    public void GenerateDevToken_ReturnsValidJwt()
    {
        var config = CreateConfig();
        var token = AuthConfiguration.GenerateDevToken(config, "user-1", "test@t.com", "Test User", ["Host"]);

        Assert.NotEmpty(token);
        var handler = new JwtSecurityTokenHandler();
        Assert.True(handler.CanReadToken(token));
    }

    [Fact]
    public void GenerateDevToken_ContainsExpectedClaims()
    {
        var config = CreateConfig();
        var tokenStr = AuthConfiguration.GenerateDevToken(config, "user-42", "alice@t.com", "Alice", ["Artist", "Attendee"]);

        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(tokenStr);

        Assert.Equal("user-42", token.Subject);
        Assert.Contains(token.Claims, c => c.Type == JwtRegisteredClaimNames.Email && c.Value == "alice@t.com");
        Assert.Contains(token.Claims, c => c.Type == JwtRegisteredClaimNames.Name && c.Value == "Alice");
        Assert.Contains(token.Claims, c => c.Type == ClaimTypes.Role && c.Value == "Artist");
        Assert.Contains(token.Claims, c => c.Type == ClaimTypes.Role && c.Value == "Attendee");
    }

    [Fact]
    public void GenerateDevToken_HasJtiClaim()
    {
        var config = CreateConfig();
        var tokenStr = AuthConfiguration.GenerateDevToken(config, "u", "e@t.com", "N", ["Admin"]);

        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(tokenStr);

        Assert.Contains(token.Claims, c => c.Type == JwtRegisteredClaimNames.Jti && !string.IsNullOrEmpty(c.Value));
    }

    [Fact]
    public void GenerateDevToken_ExpiresInFuture()
    {
        var config = CreateConfig();
        var tokenStr = AuthConfiguration.GenerateDevToken(config, "u", "e@t.com", "N", ["Admin"]);

        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(tokenStr);

        Assert.True(token.ValidTo > DateTime.UtcNow);
        Assert.True(token.ValidTo <= DateTime.UtcNow.AddHours(9)); // 8h + some buffer
    }

    [Fact]
    public void GenerateDevToken_CanBeValidated()
    {
        var config = CreateConfig();
        var tokenStr = AuthConfiguration.GenerateDevToken(config, "u", "e@t.com", "N", ["Host"]);

        var handler = new JwtSecurityTokenHandler();
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            "GigHaus-Dev-Signing-Key-Replace-In-Production-Min-32-Chars!!"));

        var principal = handler.ValidateToken(tokenStr, new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = "GigHaus",
            ValidAudience = "GigHaus-API",
            IssuerSigningKey = key
        }, out var validatedToken);

        Assert.NotNull(principal);
        Assert.NotNull(validatedToken);
    }

    [Fact]
    public void GenerateDevToken_UsesCustomConfig()
    {
        var config = CreateConfig(
            key: "CustomKeyThatIsAtLeast32CharactersLong!!!!",
            issuer: "MyIssuer",
            audience: "MyAudience");
        var tokenStr = AuthConfiguration.GenerateDevToken(config, "u", "e@t.com", "N", ["Admin"]);

        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(tokenStr);

        Assert.Equal("MyIssuer", token.Issuer);
        Assert.Contains("MyAudience", token.Audiences);
    }

    [Fact]
    public void PolicyConstants_AreCorrect()
    {
        Assert.Equal("Admin", AuthConfiguration.PolicyAdmin);
        Assert.Equal("HostOrAdmin", AuthConfiguration.PolicyHost);
        Assert.Equal("ArtistOrAdmin", AuthConfiguration.PolicyArtist);
        Assert.Equal("Authenticated", AuthConfiguration.PolicyAuthenticated);
    }

    [Fact]
    public void RoleConstants_AreCorrect()
    {
        Assert.Equal("Admin", AuthConfiguration.RoleAdmin);
        Assert.Equal("Host", AuthConfiguration.RoleHost);
        Assert.Equal("Artist", AuthConfiguration.RoleArtist);
        Assert.Equal("Attendee", AuthConfiguration.RoleAttendee);
    }

    [Fact]
    public void GenerateDevToken_MultipleRoles_AllPresent()
    {
        var config = CreateConfig();
        var roles = new[] { "Admin", "Host", "Artist", "Attendee" };
        var tokenStr = AuthConfiguration.GenerateDevToken(config, "u", "e@t.com", "N", roles);

        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(tokenStr);
        var roleClaims = token.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();

        Assert.Equal(4, roleClaims.Count);
        foreach (var role in roles)
            Assert.Contains(role, roleClaims);
    }
}


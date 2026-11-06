using System.Threading.RateLimiting;
using GigHaus.Domain.Entities;
using GigHaus.Infrastructure;
using GigHaus.Infrastructure.Data;
using GigHaus.Infrastructure.Identity;
using GigHaus.Web.Components;
using GigHaus.Web.Identity;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;

DapperConfig.Configure();

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Npgsql data source from Aspire
builder.AddNpgsqlDataSource("gighaus");

// Dapper repositories, email and portal services
builder.Services.AddGigHausInfrastructure();
builder.Services.AddGigHausEmail(options =>
{
    builder.Configuration.GetSection("Smtp").Bind(options);

    // Aspire hands over the MailDev endpoint in development.
    if (Uri.TryCreate(builder.Configuration["services:maildev:smtp:0"], UriKind.Absolute, out var smtp))
    {
        options.Host = smtp.Host;
        options.Port = smtp.Port;
    }
});
builder.Services.AddGigHausPortal();

// Sign-in: ASP.NET Core Identity over the Dapper user store
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();
builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();
builder.Services.AddAuthorization();
builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 10;
        options.Password.RequireDigit = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    })
    .AddUserStore<DapperUserStore>()
    .AddClaimsPrincipalFactory<GigHausClaimsFactory>()
    .AddSignInManager()
    .AddDefaultTokenProviders();
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/login";
    options.AccessDeniedPath = "/access-denied";
    options.ExpireTimeSpan = TimeSpan.FromDays(14);
    options.SlidingExpiration = true;
});
builder.Services.AddHostedService<AdminSeeder>();

// Public forms can be reached by anyone, so limit how often one address can post.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        HttpMethods.IsPost(context.Request.Method) && !context.Request.Path.StartsWithSegments("/_blazor")
            ? RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = 30, Window = TimeSpan.FromMinutes(1) })
            : RateLimitPartition.GetNoLimiter("not-limited"));
});

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

app.MapDefaultEndpoints();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapPost("/logout", async (SignInManager<ApplicationUser> signIn) =>
    {
        await signIn.SignOutAsync();
        return TypedResults.LocalRedirect("~/");
    })
    .WithMetadata(new RequireAntiforgeryTokenAttribute());
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

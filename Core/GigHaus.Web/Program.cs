using GigHaus.Infrastructure;
using GigHaus.Infrastructure.Data;
using GigHaus.Web.Components;

DapperConfig.Configure();

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Npgsql data source from Aspire
builder.AddNpgsqlDataSource("gighaus");

// Dapper repositories
builder.Services.AddGigHausInfrastructure();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

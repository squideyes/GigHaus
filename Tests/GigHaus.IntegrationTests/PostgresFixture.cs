using System.Reflection;
using DbUp;
using GigHaus.Domain.Entities;
using GigHaus.Infrastructure;
using GigHaus.Infrastructure.Data;
using GigHaus.Infrastructure.Email;
using GigHaus.Infrastructure.Identity;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using NSubstitute;
using Testcontainers.PostgreSql;

namespace GigHaus.IntegrationTests;

/// <summary>
/// One real Postgres for the whole test run, built by the same migrations production uses.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private const string Image = "postgres:18.3";
    private const string MigrationsAssembly = "GigHaus.Migrations";

    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder(Image).Build();
    private NpgsqlDataSource? _dataSource;

    public ServiceProvider Services { get; private set; } = default!;

    public IEmailService Email { get; } = Substitute.For<IEmailService>();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        var connectionString = _container.GetConnectionString();
        var upgrade = DeployChanges.To
            .PostgresqlDatabase(connectionString)
            .WithScriptsEmbeddedInAssembly(Assembly.Load(MigrationsAssembly))
            .LogToNowhere()
            .Build()
            .PerformUpgrade();

        upgrade.Successful.ShouldBeTrue(upgrade.Error?.Message);

        DapperConfig.Configure();
        _dataSource = NpgsqlDataSource.Create(connectionString);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(_dataSource);
        services.AddSingleton(Email);
        services.AddGigHausInfrastructure();
        services.AddGigHausPortal();
        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 10;
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
            })
            .AddUserStore<DapperUserStore>()
            .AddClaimsPrincipalFactory<GigHausClaimsFactory>();

        Services = services.BuildServiceProvider();
    }

    public async Task DisposeAsync()
    {
        await Services.DisposeAsync();

        if (_dataSource is not null)
        {
            await _dataSource.DisposeAsync();
        }

        await _container.DisposeAsync();
    }
}

[CollectionDefinition(Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "Postgres";
}

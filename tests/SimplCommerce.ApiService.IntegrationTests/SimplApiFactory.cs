using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimplCommerce.Infrastructure.Modules;
using SimplCommerce.Module.Core.Data;
using Testcontainers.MsSql;
using Xunit;

namespace SimplCommerce.ApiService.IntegrationTests;

/// <summary>
/// Spins a disposable SQL Server 2022 container via Testcontainers and boots the
/// ApiService against it. Shared across all <see cref="IClassFixture{T}"/> consumers
/// in the collection so container start (~15s) amortizes over every test.
///
/// Requires Docker on the host running the test (CI with Docker-in-Docker, local dev
/// box, or a remote Docker endpoint via DOCKER_HOST). If Docker is missing the fixture
/// will throw during InitializeAsync — tests marked with the <c>RequiresDocker</c>
/// trait should be filtered out via <c>--filter "Category!=RequiresDocker"</c>.
/// </summary>
public class SimplApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MsSqlContainer _sql = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    public string ConnectionString => _sql.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _sql.StartAsync();

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SimplDbContext>();
        await db.Database.MigrateAsync();
    }

    public new async Task DisposeAsync()
    {
        await _sql.DisposeAsync();
        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:SimplCommerce"] = _sql.GetConnectionString(),
                ["ConnectionStrings:redis"] = string.Empty,
                ["ConnectionStrings:blobs"] = string.Empty,
            });
        });

        // The ApiService registers SimplDbContext through Aspire's pooled
        // AddSqlServerDbContext("SimplCommerce"), which reads
        // ConnectionStrings:SimplCommerce — already pointed at the Testcontainer
        // above. Re-registering the DbContext here would collide with the pool, so
        // we only seed the module manifest (needed by SimplDbContext.OnModelCreating
        // for entity discovery; idempotent — Program.cs also calls it).
        builder.ConfigureServices(_ => ModuleManifestLoader.LoadAllBundled());
    }
}

[CollectionDefinition("ApiServiceDb")]
public class ApiServiceDbCollection : ICollectionFixture<SimplApiFactory> { }

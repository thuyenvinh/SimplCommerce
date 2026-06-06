using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
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

        // Aspire's AddSqlServerDbContext("SimplCommerce") reads the connection string
        // at registration time inside Program.cs — BEFORE WebApplicationFactory's
        // ConfigureAppConfiguration callbacks run — so an in-memory config source is
        // applied too late and Aspire captures a null connection string. Environment
        // variables ARE read by WebApplication.CreateBuilder immediately, so set them
        // before the host is built (first .Services access below). Empty redis/blobs
        // tell Aspire to disable those components.
        Environment.SetEnvironmentVariable("ConnectionStrings__SimplCommerce", _sql.GetConnectionString());
        Environment.SetEnvironmentVariable("ConnectionStrings__redis", string.Empty);
        Environment.SetEnvironmentVariable("ConnectionStrings__blobs", string.Empty);

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SimplDbContext>();
        await db.Database.MigrateAsync();
    }

    public new async Task DisposeAsync()
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__SimplCommerce", null);
        Environment.SetEnvironmentVariable("ConnectionStrings__redis", null);
        Environment.SetEnvironmentVariable("ConnectionStrings__blobs", null);
        await _sql.DisposeAsync();
        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Keep a production-like environment. NOT Development: that flips on the DI
        // container's ValidateOnBuild, which eagerly validates every registration in
        // this large modular app (many conditional/IEnumerable handler registrations
        // aren't resolvable as roots) — stricter than production and out of scope to
        // satisfy here. Health endpoints are dev-only by design, so the smoke test
        // hits an always-mapped anonymous endpoint instead.
        builder.UseEnvironment("Testing");

        // SimplDbContext.OnModelCreating walks GlobalConfiguration.Modules for entity
        // discovery; seed the manifest (idempotent — Program.cs also calls it).
        builder.ConfigureServices(_ => ModuleManifestLoader.LoadAllBundled());
    }
}

[CollectionDefinition("ApiServiceDb")]
public class ApiServiceDbCollection : ICollectionFixture<SimplApiFactory> { }

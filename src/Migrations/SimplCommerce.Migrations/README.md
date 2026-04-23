# SimplCommerce.Migrations

Consolidated EF Core migrations project. Home for every EF migration owned by SimplCommerce from Aspire cutover onward.

## Status

- `Migrations/20260423123830_Initial_AspireBaseline.cs` — consolidated schema snapshot (85 tables) across every bundled module in `ModuleConfigurationManager`. Generated from the current `SimplDbContext` model via a design-time factory that seeds `GlobalConfiguration.Modules` from the static manifest before EF walks `OnModelCreating`. No live SQL Server was needed.
- ApiService (`src/Apps/SimplCommerce.ApiService/Program.cs`) already uses `MigrationsAssembly("SimplCommerce.Migrations")`.
- Legacy WebHost (`src/SimplCommerce.WebHost/`) keeps its own migration chain under `Migrations/` unchanged — needed until Phase 8 destructive removes WebHost.

## How the design-time factory works

`DesignTimeDbContextFactory` (this project) implements `IDesignTimeDbContextFactory<SimplDbContext>`:

1. Calls `ModuleManifestLoader.LoadAllBundled()` which walks `ModuleConfigurationManager.GetModules()` and `Assembly.Load`s each.
2. Builds `DbContextOptions<SimplDbContext>` with SqlServer provider, placeholder connection string, and `MigrationsAssembly` pointed at itself.

That is enough for `dotnet ef migrations add` / `dotnet ef migrations script` to run without any database or Aspire host.

## Runbook — regenerate the baseline (only if the model changes meaningfully)

```bash
dotnet build SimplCommerce.sln
dotnet ef migrations add <MigrationName> \
  --project src/Migrations/SimplCommerce.Migrations \
  --context SimplDbContext \
  --output-dir Migrations \
  --no-build
```

No SQL Server required.

## Runbook — apply to a fresh database

```bash
dotnet ef database update \
  --project src/Migrations/SimplCommerce.Migrations \
  --context SimplDbContext \
  --connection "Server=<host>;Database=SimplCommerce;User Id=sa;Password=<pw>;TrustServerCertificate=True"
```

Or let Aspire apply it on first boot by adding `dbContext.Database.Migrate()` in `ApiService/Program.cs` (not done by default — explicit migrations are safer in prod).

## Runbook — upgrade from an existing legacy WebHost DB (prod migration)

An existing prod database already has `SimplCommerce.WebHost/Migrations/20240311113057_AddedProductBackInStockSubscription` as the last applied row in `__EFMigrationsHistory`. The tables match the new baseline **exactly** (Phase 2 did not change any column). So the upgrade is a one-row swap in the history table, not a schema migration:

```sql
-- 1. Back up the database.
-- 2. Stamp the new baseline as already-applied so EF doesn't try to re-create tables.
DELETE FROM __EFMigrationsHistory;
INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
VALUES ('20260423123830_Initial_AspireBaseline', '9.0.0');
```

After that, every future migration goes through `src/Migrations/SimplCommerce.Migrations/`.

`docs/migration/data-migration-runbook.md` carries the full Phase 6 procedure (backup, staging copy, dry-run, smoke test).

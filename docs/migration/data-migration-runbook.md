# Data migration runbook — SimplCommerce → .NET Aspire

> Phase 6 (`P6-01`..`P6-07`). Migrate an existing SimplCommerce SQL Server
> database (running on the legacy ASP.NET Core 8 `SimplCommerce.WebHost`)
> onto the new Aspire-hosted stack without losing data.

## Pre-flight

| Check | How |
|---|---|
| Legacy DB reachable | `sqlcmd -S <server> -Q "SELECT @@VERSION"` |
| Legacy schema version | Open `__EFMigrationsHistory`; last row should be `20240311113057_AddedProductBackInStockSubscription` (the final migration in `SimplCommerce.WebHost/Migrations/`). Unknown rows → **stop**, investigate custom migrations first. |
| Disk free | 2× DB size (for the backup) |
| Aspire stack | `dotnet run --project src/AppHost/SimplCommerce.AppHost` starts cleanly on a dev box |
| `dotnet-ef` | `dotnet tool install --global dotnet-ef --version 9.0.0` |

## Migration strategy — additive only

Phase 2 consolidated the three legacy migrations into
`src/Migrations/SimplCommerce.Migrations/` but **did not change** any
table or column definition — the next migration to be generated is
`Initial_AspireBaseline`, a no-op snapshot against the same schema.

Rules this runbook enforces:

1. **Never drop a column.** If Phase 2 / Phase 3 discovers a field that
   isn't needed in the new stack, mark it obsolete in code but leave
   the column in place until Phase 8 cutover is verified.
2. **Never rename an FK.** Any rename introduces a destructive
   migration pair (drop + add) that can break in-flight transactions.
3. **Additive widening only.** Longer `nvarchar`, new nullable columns,
   new tables — fine. Narrowing / NOT NULL additions require a two-
   step migration (add nullable → backfill → alter to NOT NULL).

## Step 1 — regenerate the consolidated migration (one-time)

Run once on a clean working copy; commit the result.

```bash
# 1. Point the DbContext at the new Migrations project.
#    In AddCustomizedDataStore + the ApiService Program.cs this is
#    already set to MigrationsAssembly("SimplCommerce.Migrations").

# 2. Drop the three legacy migration files from WebHost/Migrations/
#    and move them into the new project (git mv keeps history):
git mv src/SimplCommerce.WebHost/Migrations/*.cs \
       src/Migrations/SimplCommerce.Migrations/Migrations/

# 3. Rebuild so dotnet-ef finds the moved classes.
dotnet build SimplCommerce.sln

# 4. Regenerate the snapshot only; existing migrations stay as-is.
dotnet ef migrations add Initial_AspireBaseline \
    --project src/Migrations/SimplCommerce.Migrations \
    --startup-project src/Apps/SimplCommerce.ApiService \
    --context SimplDbContext

# 5. Verify idempotency: on a *clean* SQL Server, `dotnet ef database update`
#    must reach the same final schema as on the legacy DB.
```

## Step 2 — migrate a live database

Three supported paths — pick whichever matches your rollout model.

### Path A — in-place upgrade (shortest, highest risk)

```powershell
# On the production SQL Server host — run the PowerShell wrapper below.
pwsh tools/migrate-data.ps1 `
    -Server "sql-prod" -Database "SimplCommerce" `
    -SignedOffBy "ops-lead@example"
```

The script backs up the DB, applies the consolidated migrations, and
verifies row-counts for 5 critical tables (`Core_User`, `Catalog_Product`,
`Orders_Order`, `ShoppingCart_Cart`, `Reviews_Review`). If any count
regresses it restores from backup before exiting non-zero.

### Path B — parallel database (recommended for first cutover)

1. Restore the production backup onto a fresh `SimplCommerce_v2` DB.
2. Apply migrations against `SimplCommerce_v2` via `dotnet ef database
   update --connection "..."`.
3. Run `aspire run` with `ConnectionStrings__SimplCommerce` pointing at
   `SimplCommerce_v2`. Smoke-test storefront + admin.
4. Once green, switch DNS / connection strings over; the old DB stays
   untouched as a rollback target for 48 h.

### Path C — blue/green (Kubernetes / Container Apps)

Deploy the new stack to a sibling environment, apply migrations via
the init container (Aspire publishes a migration bundle if you run
`dotnet ef migrations bundle`), flip the ingress.

## Step 3 — sample data (optional, dev/staging only)

`SimplCommerce.Module.SampleData.Services.SampleDataService` still
drives the `/sample-data` bootstrap flow that existed in the legacy
WebHost. After Phase 2 its `ISampleDataService` moved to
`Application/Services/` and the impl to `Infrastructure/Services/`; the
runtime behaviour is unchanged (it replays SQL scripts under
`SampleContent/{Fashion,Phones}` via `ISqlRepository.RunCommand`).

```bash
# Once the admin is signed in on a fresh DB:
curl -X POST https://<admin-host>/api/admin/sample-data/seed  # deferred UI
# Or invoke SampleDataService.InstallSampleData(...) from a DB seed tool.
```

Production DBs **must not** run this — the sample scripts INSERT with
fixed IDs that would collide with real data.

## Step 4 — post-migration checks

```sql
-- Schema version landed
SELECT TOP 1 MigrationId FROM __EFMigrationsHistory ORDER BY MigrationId DESC;
-- Expected: Initial_AspireBaseline

-- Row-count sanity (baseline numbers taken from Step 2's pre-backup):
SELECT 'Core_User' AS t, COUNT(*) FROM Core_User UNION ALL
SELECT 'Catalog_Product',  COUNT(*) FROM Catalog_Product UNION ALL
SELECT 'Orders_Order',     COUNT(*) FROM Orders_Order UNION ALL
SELECT 'ShoppingCart_Cart', COUNT(*) FROM ShoppingCart_Cart UNION ALL
SELECT 'Reviews_Review',   COUNT(*) FROM Reviews_Review;

-- Smoke the API (admin token required):
curl -s "https://api/api/storefront/catalog/products?pageSize=1" | jq '.totalCount'
curl -s "https://api/api/admin/orders?pageSize=1"                | jq '.total'
```

## Rollback

Path A: restore the `.bak` written by `tools/migrate-data.ps1`.
Path B/C: flip traffic back to the old connection string; old DB is
untouched.

`dotnet ef migrations remove` is **not** a valid rollback once users
have been writing to the new stack — it only scrubs the migration file
(the DB state stays mutated). Always restore from backup.

## Blocked in this sandbox

- Steps 1/2 above require a running SQL Server; Phase 0/1/2/3
  manual-steps docs carry the full list of Docker-dependent tasks.
- The `Initial_AspireBaseline` migration has **not** been generated
  yet — file move in step 1.2 plus `dotnet ef migrations add` is a
  one-shot action that needs a developer box with `dotnet-ef` and a
  live DB for the scaffolder to reach.
- Row-count baseline will be user-specific; the pre-flight script
  captures it from the live DB before doing anything destructive.

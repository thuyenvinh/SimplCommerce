# SimplCommerce.Migrations

Consolidated EF Core migrations project. Target home for every EF migration owned by SimplCommerce.

## Status (Phase 2 scaffold)

- Project exists with EF Core 9 Design + Tools + SqlServer packages.
- References every module project that contributes entities so `dotnet ef` can discover `ICustomModelBuilder` implementations across the modular domain.
- **Existing migrations still live in `src/SimplCommerce.WebHost/Migrations/`** and `MigrationsAssembly("SimplCommerce.WebHost")` is still configured in `AddCustomizedDataStore`. Do not move them until Phase 2 P2-04/P2-05 runs end-to-end against a live SQL Server (requires Docker + Aspire).

## Runbook: generate the consolidated `Initial_AspireBaseline` migration

Prerequisites:
- Docker running so Aspire can spin up SQL Server
- Working directory: repo root

1. Update `AddCustomizedDataStore` so the DbContext pool uses:
   ```csharp
   options.UseSqlServer(connectionString, b => b.MigrationsAssembly("SimplCommerce.Migrations"));
   ```
2. Run AppHost so SQL Server resource is available:
   ```bash
   dotnet run --project src/AppHost/SimplCommerce.AppHost
   ```
3. In a second shell, export the connection string Aspire created (visible in the dashboard) as `ConnectionStrings__SimplCommerce`.
4. Copy the 3 existing migrations from `src/SimplCommerce.WebHost/Migrations/` into `src/Migrations/SimplCommerce.Migrations/Migrations/`.
5. Re-add a snapshot migration pointing at the new assembly:
   ```bash
   dotnet ef migrations add Initial_AspireBaseline \
       --project src/Migrations/SimplCommerce.Migrations \
       --startup-project src/SimplCommerce.WebHost \
       --context SimplDbContext
   ```
6. Verify: `dotnet ef database update` on a clean DB applies cleanly.
7. Delete the copies left in `src/SimplCommerce.WebHost/Migrations/`.

Idempotency: all steps are safe to re-run. The DbContext lives in `SimplCommerce.Module.Core.Data.SimplDbContext` and does not need to be moved.

# SimplCommerce.ApiService.IntegrationTests

Smoke tests against the ApiService composition root.

## Status

**Scaffold-only.** The full `WebApplicationFactory<Program>` smoke tests
(P3-58..P3-63 in `MIGRATION_PROGRESS.md`) are **blocked**:

1. The ApiService requires:
   - A SQL Server connection string (`ConnectionStrings:SimplCommerce`)
   - A Redis connection string (`ConnectionStrings:redis`)
   - An Azure Blob Storage connection string (`ConnectionStrings:blobs`)
   Aspire provides these via `builder.AddSqlServerDbContext`,
   `AddRedisDistributedCache`, `AddAzureBlobServiceClient`. Booting the
   host without them fails at service construction.

2. The sandbox that produced this branch has no Docker daemon, so the
   Aspire orchestrator can't run, and the EF InMemory provider is not
   drop-in compatible with our SQL Server-specific configuration
   (column types, raw SQL in seed data, `EF.Functions.Like`, etc.).

## Runbook (to be executed once on a developer box)

1. `aspire run` in one shell to stand up SQL/Redis/Azurite.
2. `dotnet test tests/SimplCommerce.ApiService.IntegrationTests`
3. Uncomment the `SmokeTests` class below (remove the `Skip = ...`) and
   assert against the endpoints.

Tests to add here, in priority order:

- [ ] `/alive` + `/health` return 200 (this is ServiceDefaults — already
      wired in Program.cs).
- [ ] `/api/auth/register` -> 200 (new user), `/api/auth/login` -> JWT.
- [ ] Authorized GET with the returned token on `/api/auth/me`.
- [ ] `/api/storefront/catalog/products` returns seeded products.
- [ ] `/api/admin/orders` requires AdminOnly policy (401 without token,
      200 with admin token).

This directory is referenced by `SimplCommerce.sln` via a solution
folder so `dotnet build` validates it compiles, even while the tests
themselves are deferred.

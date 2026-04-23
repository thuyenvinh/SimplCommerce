# SimplCommerce — .NET 9 + Aspire + Blazor

> A cross-platform, modulith ecommerce system. The codebase is mid-migration
> from ASP.NET Core 8 MVC + AngularJS (1.x) onto .NET 9 + Aspire 13 + Blazor
> (2.0). See `CHANGELOG.md` for the release summary, `MIGRATION_PROGRESS.md`
> for phase-by-phase status, `docs/architecture.md` for the target
> architecture.

![SimpleCommerce - Modulith architecture](https://raw.githubusercontent.com/simplcommerce/SimplCommerce/master/modular-architecture.png)

## Quick start (Aspire — recommended)

```bash
# one-time
dotnet tool install --global dotnet-ef --version 9.0.0

# run the whole stack: SQL, Redis, Azurite, MailPit, Seq + ApiService +
# Storefront + Admin + the legacy WebHost (still included for cutover parity)
dotnet run --project src/AppHost/SimplCommerce.AppHost
```

Aspire dashboard: `https://localhost:17001`.

Default URLs once everything is healthy:

| Surface | URL |
|---|---|
| Storefront | `https://localhost:7100` |
| Admin | `https://localhost:7200` |
| ApiService + Scalar UI | `https://localhost:7001/scalar/v1` |
| Seq (logs) | exposed by Aspire, shown in dashboard |

Default admin credentials (unchanged from 1.x):
`admin@simplcommerce.com` / `1qazZAQ!`

### Prerequisites

- .NET 9 SDK (`global.json` pins `9.0.100` + `latestMinor`)
- Docker / Podman Desktop (Aspire spins up SQL/Redis/Azurite/MailPit/Seq
  as containers)

That's it — no SQL Server install, no PostgreSQL setup, no manual
connection-string editing. Aspire injects every connection string at
runtime.

## Quick start (without Aspire — self-hosted Docker)

```bash
cp .env.sample .env             # fill in SQL_SA_PASSWORD + JWT_SIGNING_KEY
docker compose up --build
```

Ports: `7001` (api), `7100` (storefront), `7200` (admin), `1433` (sql),
`6379` (redis), `8025` (mailpit UI), `5341` (Seq).

## Repository layout

```
src/
  AppHost/                 Aspire orchestrator
  ServiceDefaults/         OTel + health + service discovery shared by every app
  Migrations/              Consolidated EF Core migrations (target for dotnet ef)
  Apps/
    SimplCommerce.ApiService          Minimal-API JWT backend (OpenAPI /scalar/v1)
    SimplCommerce.Storefront          Blazor Web App, Interactive Auto + WASM client
    SimplCommerce.Storefront.Client   WASM companion to Storefront
    SimplCommerce.Admin               Blazor Web App, Interactive Server
  Modules/                  43 domain modules (Clean-architecture-lite layout)
  SimplCommerce.Infrastructure/
  SimplCommerce.WebHost/    Legacy ASP.NET Core 8 MVC host — kept during cutover

tests/
  SimplCommerce.ApiService.IntegrationTests  Integration test scaffold
test/                       Existing unit test projects (Infrastructure + 6 modules)

tools/
  migrate-data.ps1          PowerShell data-migration helper (see runbook)
  loadtest/storefront.js    k6 load scenario (p95 < 500ms @ 50 VU target)

docs/
  architecture.md           Topology + trace path
  deployment.md             Aspire / Azure Container Apps / k8s / Compose
  development.md            How to add a module or endpoint
  migration/                Phase 0–8 inventory + runbooks
```

## Build & test

```bash
dotnet build SimplCommerce.sln
dotnet test SimplCommerce.sln
```

CI: `.github/workflows/ci.yml` (GitHub Actions) + `azure-pipelines.yml`
(Azure DevOps). Both build with `TreatWarningsAsErrors=true`; CI on
`master` pushes container images to `ghcr.io/<owner>/simpl-{api,
storefront, admin}:<sha>`.

## Online demo (v1.x)

- Store front: `http://demo.simplcommerce.com` (legacy build)
- Admin: `http://demo.simplcommerce.com/admin`

A v2.0 demo will replace the v1.x demo after Phase 8 cutover completes.

## Technologies

- .NET 9, Aspire 13, Blazor Web App (Interactive Auto + Server)
- MudBlazor 7 UI
- EF Core 9, SQL Server 2022, Redis 7, Azurite (Azure Blob emulator)
- ASP.NET Identity Core + JWT (Bearer on API) + cookie BFF (Blazor hosts)
- MediatR 12 for domain events
- OpenTelemetry + Seq for logs/metrics/traces
- k6 for load tests, xUnit + FluentAssertions for the suite

## How to contribute

- Star the project on GitHub
- Report bugs / suggest features by creating issues
- Submit pull requests against `master`
- Spread the word — blog, tweet, link

## Contributors

<a href="https://github.com/simplcommerce/SimplCommerce/graphs/contributors"><img src="https://opencollective.com/simplcommerce/contributors.svg?width=890" title="contributors" alt="contributors" /></a>

## Backers & sponsors

- [Become a backer](https://opencollective.com/simplcommerce#backer)
- [Become a sponsor](https://opencollective.com/simplcommerce#sponsor)

## License

SimplCommerce is licensed under the Apache 2.0 license. See `License.txt`.

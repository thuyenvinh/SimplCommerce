# SimplCommerce — .NET 9 + Aspire + Blazor

> A cross-platform, modulith ecommerce system.
> v2.0 runs on .NET 9 + Aspire 13 + Blazor. The legacy ASP.NET Core 8 MVC + AngularJS
> stack has been removed in Phase 8 (see `MIGRATION_PROGRESS.md` / `CHANGELOG.md`).

![SimpleCommerce - Modulith architecture](https://raw.githubusercontent.com/simplcommerce/SimplCommerce/master/modular-architecture.png)

## Quick start (Aspire — recommended)

```bash
# one-time
dotnet tool install --global dotnet-ef --version 9.0.0

# run the whole stack: SQL, Redis, Azurite, MailPit, Seq +
# ApiService + Storefront + Admin
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
  SimplCommerce.Infrastructure/   Repos, value objects, EF abstractions
  SimplCommerce.RealTime/         Cross-host SignalR contracts (AdminNotificationHub)
  Apps/
    SimplCommerce.ApiService          Minimal-API JWT backend (OpenAPI /scalar/v1)
    SimplCommerce.Storefront          Blazor Web App, Interactive Auto + WASM client
    SimplCommerce.Storefront.Client   WASM companion to Storefront
    SimplCommerce.Admin               Blazor Web App, Interactive Server (+ NotificationBell hub client)
  Modules/                  41 domain modules: Catalog / Orders / Checkouts / Shipping /
                            Tax / Payments (+8 providers incl. VNPay) / Inventory /
                            Reviews / Comments / Contacts / CMS / News / Pricing /
                            Vendors / Localization / SampleData / Search / Storage /
                            WishList / DinkToPdf / EmailSenderSmtp / SignalR / etc.

tests/                       New-stack tests (xUnit + WebApplicationFactory)
  SimplCommerce.ApiService.UnitTests          Auth, hardening, webhooks, checkout, media
  SimplCommerce.ApiService.IntegrationTests   Testcontainers.MsSql + real SQL
  SimplCommerce.Storefront.UnitTests          API client + sitemap
test/                       Legacy-era unit test projects (Infrastructure + 12 modules)

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

## Build & test (with coverage)

```bash
# Default suite (no Docker required)
dotnet test SimplCommerce.sln --filter "Category!=RequiresDocker"

# Integration suite (needs Docker for Testcontainers.MsSql)
dotnet test tests/SimplCommerce.ApiService.IntegrationTests --filter "Category=RequiresDocker"
```

## Online demo

A v2.0 demo will be linked here after the next release tag. The legacy v1.x demo
(`demo.simplcommerce.com`) is being decommissioned.

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

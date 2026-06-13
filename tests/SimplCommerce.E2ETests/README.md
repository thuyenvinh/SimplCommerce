# SimplCommerce E2E Tests (Playwright for .NET)

Playwright + NUnit suite that drives the Blazor admin app end-to-end. Generates
documentation-ready screenshots and videos under `Artifacts/`.

## Prerequisites

- .NET 9 SDK
- Docker (for the Aspire-orchestrated SQL Server / Redis / Storage Emulator /
  MailPit / Seq containers — required by `SimplCommerce.AppHost`)
- Playwright browsers (installed once after first build)

## One-time browser install

```bash
dotnet build tests/SimplCommerce.E2ETests
./tests/SimplCommerce.E2ETests/bin/Debug/net9.0/.playwright/node/linux-x64/node \
  tests/SimplCommerce.E2ETests/bin/Debug/net9.0/.playwright/package/cli.js \
  install chromium
```

(On Windows / macOS use the equivalent `playwright.ps1 install chromium`.)

## Configuration

`appsettings.Test.json` carries non-secret defaults. Override anything via
environment variables prefixed `E2E__`:

| Variable | Default | Notes |
|----------|---------|-------|
| `E2E__AdminBaseUrl` | `http://localhost:5236` | Admin Blazor server |
| `E2E__StorefrontBaseUrl` | `http://localhost:5237` | Public storefront |
| `E2E__ApiBaseUrl` | `http://localhost:5096` | Minimal-API backend |
| `E2E__AdminEmail` | seed admin | Override in CI to avoid mutating the seed account |
| `E2E__AdminPassword` | seed admin | Same |
| `E2E__Headless` | `true` | Set `false` for live debugging |
| `E2E__SlowMoMs` | `0` | Bump to ~150 to make doc videos easier to follow |
| `E2E__ViewportWidth` / `Height` | 1440×900 | Standard documentation viewport |
| `E2E__VideoMode` | `On` | `On`, `OnFailure`, `Off` |
| `E2E__ScreenshotMode` | `On` | per-step captures via `ArtifactWriter` |
| `E2E__TraceMode` | `OnFailure` | `On` for every test |

## Running the app stack

```bash
cd src/AppHost/SimplCommerce.AppHost
dotnet run
```

Aspire stands up SQL Server, Redis, the storage emulator, MailPit, Seq, plus
the three application projects (api, admin, storefront). Wait for the Aspire
dashboard to mark all resources `Running`. Note the port that `admin` is bound
to and set `E2E__AdminBaseUrl` accordingly (Aspire picks a port on each launch).

## Running the tests

```bash
# Full suite
dotnet test tests/SimplCommerce.E2ETests

# Single class
dotnet test tests/SimplCommerce.E2ETests \
  --filter "FullyQualifiedName~Tests.Catalog.ProductCrudTests"

# Documentation-only walkthroughs (slowed down + clean captures)
E2E__SlowMoMs=150 \
  dotnet test tests/SimplCommerce.E2ETests \
  --filter "FullyQualifiedName~Tests.Documentation"
```

## Where artifacts land

```
tests/SimplCommerce.E2ETests/Artifacts/
├── Screenshots/{Module}/{TestName}/{NN}-{Step}.png
├── Videos/{Module}/{TestName}/{playwright-id}.webm
└── Traces/{Module}/{TestName}/trace.zip            (on failure unless TraceMode=On)
```

- `Screenshots` go straight into the user-documentation site.
- `Videos` are `.webm`; convert to `.mp4` with ffmpeg for embed:
  `ffmpeg -i input.webm -c:v libx264 -crf 23 output.mp4`
- `Traces` open in the Playwright trace viewer:
  `pwsh bin/Debug/net9.0/playwright.ps1 show-trace Artifacts/Traces/.../trace.zip`

## Suite layout

```
Tests/
├── Auth/AuthTests.cs            — login OK, wrong password, anonymous redirect, logout
├── Navigation/NavigationTests.cs — every primary nav link opens its page
├── Catalog/ProductCrudTests.cs   — create, validation, edit, delete
├── Orders/OrdersFlowTests.cs    — list + detail (Inconclusive if no seed data)
├── Vendors/VendorApplicationsTests.cs — onboarding queue
└── Documentation/               — slowed-down happy paths whose screenshots feed docs
```

## Troubleshooting

- **`Inconclusive: E2E__AdminEmail / E2E__AdminPassword not configured`** —
  set the env vars or fill `appsettings.Test.json`.
- **`Timeout 30000ms exceeded ... waiting for navigation`** during login flows
  → the Admin app is up but the ApiService isn't (auth POSTs to /api/auth/login
  on the API). Confirm Aspire shows `api` as `Running`.
- **MudBlazor styles missing in screenshots** — the test ran against a static
  prerender pass (SignalR circuit didn't connect). Make sure the API is up and
  the page reaches `LoadState.NetworkIdle` AFTER the circuit handshake.
- **`Inconclusive: No orders in the test database`** — run the SimplCommerce
  sample-data seeder, or place an order through the storefront first.

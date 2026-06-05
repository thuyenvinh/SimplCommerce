# SimplCommerce — Aspire + Blazor architecture

> High-level view of the stack after Phase 0–7 of the migration.

## Topology

```
                          ┌──────────────────────┐
  browser ─────────────▶  │  Storefront (Blazor  │
                          │  Web App, Interactive │────┐
                          │  Auto)  :7100/:5100   │    │  JWT (via BFF cookie)
                          └──────────────────────┘    │
                          ┌──────────────────────┐    │
  admin users ────────▶   │  Admin (Blazor Server │────┤
                          │  Interactive) :7200   │    │
                          └──────────────────────┘    ▼
                                                ┌──────────────────┐
                                                │  ApiService      │  ← OpenAPI /scalar/v1
                                                │  (Minimal API,   │  ← JWT Bearer
                                                │   net9.0)        │  ← rate-limited /api/auth/*
                                                │   :7001/:5011    │  ← SignalR /signalr
                                                └──────┬───────────┘
                                                       │
                    ┌──────────────────────────────────┼───────────────────────────────────┐
                    ▼                    ▼             ▼           ▼                       ▼
              ┌──────────┐         ┌──────────┐  ┌──────────┐ ┌──────────┐          ┌──────────┐
              │ SqlServer│         │  Redis   │  │  Azurite │ │  MailPit │          │   Seq    │
              │  2022    │         │ +commander│  │ (blobs)  │ │  (smtp)  │          │ (OTel)   │
              └──────────┘         └──────────┘  └──────────┘ └──────────┘          └──────────┘
                                        ▲
                                        │ SignalR backplane
                    Admin server ───────┘
```

Everything inside the box is orchestrated by `SimplCommerce.AppHost`
(Aspire 13). On a dev machine: `dotnet run --project src/AppHost/SimplCommerce.AppHost`
brings up the whole thing. The dashboard at `https://localhost:17001`
shows the resource graph, live logs, traces, and OTLP metrics.

## Projects

| Project | Kind | Purpose |
|---|---|---|
| `src/AppHost/SimplCommerce.AppHost` | Aspire host (.NET 9) | Orchestration of sql / redis / storage / mail / seq + 4 projects |
| `src/ServiceDefaults/SimplCommerce.ServiceDefaults` | Class library | OTel, health, service discovery, HttpClient resilience — every app calls `AddServiceDefaults()` + `MapDefaultEndpoints()` |
| `src/Migrations/SimplCommerce.Migrations` | EF migrations | Consolidated target for `dotnet ef` — references all modules that own entities |
| `src/Apps/SimplCommerce.ApiService` | Minimal-API host | JWT-authenticated REST surface. 22 endpoint groups (9 storefront + 13 admin) live inside individual modules' `Endpoints/` folders and get wired in Program.cs |
| `src/Apps/SimplCommerce.Storefront` + `.Storefront.Client` | Blazor Web App (Interactive Auto) | Public shop. BFF: cookie → JWT exchange against ApiService |
| `src/Apps/SimplCommerce.Admin` | Blazor Web App (Interactive Server) | Staff admin. SignalR-ready (Redis backplane when `ConnectionStrings:redis` is set) |
| `src/SimplCommerce.WebHost` | Legacy MVC | Kept running in parallel until Phase 8 cutover |
| `src/Modules/SimplCommerce.Module.*` | Class libraries (43) | Domain/Application/Infrastructure/Endpoints layered; each exposes `Add<Name>Module(IServiceCollection)` and (where relevant) `Map<Name>Endpoints(IEndpointRouteBuilder)` |

## Key decisions (see MIGRATION_DECISIONS.md for the full log)

- **.NET 9** across the board. `net8.0` remnants are gone as of Phase 1.
- **Aspire 13.2.2**. The prompt pinned "9.x" but that version no longer exists on nuget.org (Microsoft moved Aspire to its own semver after GA).
- **MailPit** in place of MailDev. `CommunityToolkit.Aspire.Hosting.MailDev` isn't published; MailPit has the same role (SMTP + webUI).
- **Namespace-stable refactor** in Phase 2. Folder layout changed; `namespace` declarations inside files did not. Net effect: hundreds of file moves with zero caller breakage.
- **BFF auth**. Storefront + Admin hold cookies, ApiService holds JWT; a `DelegatingHandler` reads the JWT from a private cookie claim on every outbound HttpClient call. Clients never see the raw token.

## Phase 7 hardening (this project, as shipped)

| Concern | Mechanism |
|---|---|
| Rate limit on auth | `RequireRateLimiting("auth")` → 100 req/min/IP, fixed window |
| Global rate limit | Token bucket, 200 tokens/min/IP |
| Headers | `SecurityHeadersMiddleware` sets X-Content-Type-Options nosniff, X-Frame-Options DENY, Referrer-Policy, Permissions-Policy, strict CSP |
| HSTS | `app.UseHsts()` in non-Dev |
| CORS | Limited to Storefront + Admin origins |
| HTML sanitisation | `Ganss.Xss.HtmlSanitizer` singleton, strips iframe/object/embed + javascript:/vbscript: |
| CSRF | `app.UseAntiforgery()` on Blazor hosts; webhooks explicitly `DisableAntiforgery` |
| Correlation id | `CorrelationIdMiddleware` — respects inbound `X-Correlation-Id`, echoes back, tags Activity + LogScope |
| Custom metrics | `SimplMetrics` meter "SimplCommerce" exposing `simpl.orders.created`, `simpl.payments.failed`, `simpl.carts.abandoned`; meter added to OpenTelemetry in ServiceDefaults |
| Detailed health | `/health` includes `AddDbContextCheck<SimplDbContext>` + `ready` tag |
| Logs | Structured via Serilog → Seq (Aspire-injected); OTLP metrics + tracing fan-out to Aspire dashboard |

## Trace path example — "add to cart"

```
Storefront  → POST /api/storefront/cart/items  (JWT via cookie BFF)
            ← 200 { … }

Span 1  Storefront.HttpClient.POST /cart/items
Span 2    ApiService.Request
Span 3      Catalog.IProductService.FindProduct
Span 4      ShoppingCart.ICartService.AddToCart
Span 5        EFCore SqlServer.SELECT Cart
Span 6        EFCore SqlServer.INSERT CartItem
```

All 6 spans inherit the same `X-Correlation-Id`. Aspire dashboard
groups them automatically; Seq shows them under a single
`CorrelationId` scope.

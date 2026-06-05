# Changelog

## Unreleased — `v2.0.0-aspire-blazor` (planned)

Re-platforming of SimplCommerce from ASP.NET Core 8 MVC + AngularJS
onto **.NET 9 + Aspire 13 + Blazor Web App (Interactive Auto) + MudBlazor**.
Bookkeeping lives in `MIGRATION_TODO.md` / `MIGRATION_PROGRESS.md` /
`MIGRATION_DECISIONS.md`; this file is the user-facing summary.

### Breaking changes

- **Target framework** bumped from `net8.0` → `net9.0` across the solution.
  `global.json` pins SDK 9.0 `latestMinor`.
- **Aspire 13.2.2** replaces the legacy WebHost's ad-hoc bootstrap.
  `dotnet run --project src/AppHost/SimplCommerce.AppHost` is the single
  entry point; SQL/Redis/Azurite/MailPit/Seq spin up as containers
  automatically.
- `modules.json` is gone. Modules register statically through
  `ServiceCollection.Add<Name>Module()` extensions; the file is archived
  at `docs/migration/legacy/modules.json` for reference.
- **MediatR 12.x** (was already 12.1.1 on the WebHost side; confirmed
  behaviour, no code changes).
- URL convention for the new stack:
  - Storefront now at its own host (`/` root) — no more `/admin/` vs `/`
    coupling.
  - Admin at its own host with route segments matching module concerns
    (e.g. `/products`, `/orders`, `/pricing`).

### New services

- `SimplCommerce.ApiService` (Minimal API, net9.0) — JWT-authenticated
  REST surface. 9 storefront + 13 admin endpoint groups, OpenAPI at
  `/scalar/v1`, payment webhook stubs, media upload gateway.
- `SimplCommerce.Storefront` + `.Storefront.Client` (Blazor Web App,
  Interactive Auto) — 14 pages including home, catalog, product detail,
  search, cart, account, orders, addresses, wishlist, CMS page, news.
  Cookie-→-JWT BFF auth against ApiService.
- `SimplCommerce.Admin` (Blazor Web App, Interactive Server) — 15 pages
  including dashboard, catalog CRUD, orders, users, reviews, inventory,
  pricing, shipping, payments, vendors, activity log. SignalR with Redis
  backplane for realtime pushes.

### Module refactor (all 43 modules)

Each module now follows Clean-architecture-lite layout:

```
Module.Foo/
  Domain/          Entities, Events, Enums, ValueObjects, Constants
  Application/     Services (interfaces), EventHandlers, Repositories,
                   ViewModels, Queries
  Infrastructure/  Data, Services (impl), BackgroundServices, Hangfire,
                   TagHelpers, Settings, Identity, Web, Configuration,
                   Localization, Helpers
  Endpoints/       FooStorefrontEndpoints, FooAdminEndpoints
  FooModuleExtensions.cs  (AddFooModule extension, source-of-truth DI)
  ModuleInitializer.cs    ([Obsolete] shim that delegates to AddFooModule)
```

### Security & hardening

- Rate limiting: `/api/auth/*` = 100 req/min/IP, global = 200/min/IP.
- Strict CSP, X-Frame-Options DENY, Permissions-Policy.
- HTML sanitisation (`Ganss.Xss`) for review + CMS bodies.
- HSTS in non-Dev; anti-forgery on Blazor hosts.
- Correlation id propagation via `CorrelationIdMiddleware` across
  logs + spans.
- Custom OpenTelemetry metrics: `simpl.orders.created`,
  `simpl.payments.failed`, `simpl.carts.abandoned`.

### Tooling & deployment

- `tools/migrate-data.ps1` — PowerShell 7.2+ helper that backs up the
  live DB, runs `dotnet ef database update`, and auto-restores on
  row-count regression.
- `tools/loadtest/storefront.js` — k6 scenario targeting p95 < 500 ms at
  50 VU on the five hottest storefront paths.
- New multi-stage `Dockerfile`s for ApiService, Storefront, Admin.
- `compose.yaml` + `.env.sample` for production-like self-hosted
  deployments without Aspire.
- `.github/workflows/ci.yml` and `azure-pipelines.yml` rebuilt against
  the new stack (single fast Linux build + containerised publish).

### Carried forward (unchanged from 1.x)

- Order workflow semantics: `New → PendingPayment → PaymentReceived →
  Shipping → Shipped → Complete` (+ `Canceled`, `Refunded`).
- Storefront public URLs (`/product/{slug}`, `/category/{slug}`,
  `/search?q=`, `/cart`, `/checkout`).
- Payment gateway callback paths (Stripe / PayPal / MoMo / Braintree
  / Cashfree / NganLuong / COD).
- Default admin `admin@simplcommerce.com / 1qazZAQ!` still works after
  a clean data migration.

### Known gaps at v2.0.0 tag time

Runtime-dependent tasks remain **BLOCKED-Docker** in the sandbox that
produced this work:

- `aspire run` + E2E smoke, full integration tests, Playwright, OWASP
  ZAP baseline, dashboard trace verify.
- First-time `Initial_AspireBaseline` migration generation (step-by-step
  runbook at `docs/migration/data-migration-runbook.md`).

Follow-up sub-PRs queued:

- Full product-edit tab editor (General / Media / Attributes / Variants
  / Categories / SEO / Vendor / Shipping).
- Checkout multi-step UI and payment provider config forms.
- Category tree view + drag-drop reordering.
- Translation editor.
- Customer detail + role permission matrix.
- Notification hub (`AdminNotificationHub`) + `<NotificationBell>`.
- Legacy asset removal (see "Cutover" in `MIGRATION_PROGRESS.md` §Phase 8).

### Migration path for existing operators

1. Back up the live DB (`pre-aspire-<timestamp>.bak`).
2. Follow `docs/migration/data-migration-runbook.md` Path A (in-place)
   or Path B (parallel DB — recommended for first cutover).
3. Provision Redis + Blob storage if you weren't using them before.
4. Update your JWT signing key in Key Vault / env var
   `Jwt__SigningKey`.
5. Point DNS / reverse proxy at the new Storefront + Admin + ApiService
   services.
6. Keep the 1.x WebHost running side-by-side for 48 hours as a
   rollback target.

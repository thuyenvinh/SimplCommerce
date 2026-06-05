# Development guide

> How to work on the SimplCommerce .NET 9 + Aspire + Blazor codebase
> as it stands after Phase 0–7 of the migration.

## Clone & build

```bash
git clone <repo>
cd SimplCommerce
dotnet build SimplCommerce.sln
```

You'll get 59 projects + 1 scaffold test project. Build should be
clean (0 errors, 0 warnings) on any current .NET 9 SDK.

## Run the whole stack locally

```bash
dotnet run --project src/AppHost/SimplCommerce.AppHost
```

- Aspire dashboard: `https://localhost:17001`
- API + OpenAPI UI: `https://localhost:7001/scalar/v1`
- Storefront: `https://localhost:7100`
- Admin: `https://localhost:7200`
- Legacy WebHost: Aspire assigns a random port; check the dashboard

Docker is required (SQL/Redis/Azurite/MailPit/Seq). Without Docker the
projects still build but you can't boot the host.

## Tests

```bash
dotnet test SimplCommerce.sln
```

42 tests across 8 projects today. Integration tests that boot the API
in-memory live in `tests/SimplCommerce.ApiService.IntegrationTests/`
(scaffold only — see its README for what to fill in).

## Adding a new module

1. `dotnet new classlib -n SimplCommerce.Module.Foo -o src/Modules/SimplCommerce.Module.Foo`
2. Add the project to `SimplCommerce.sln` under the `Modules` folder.
3. Layer the folders as per the Phase 2 pattern:
   ```
   SimplCommerce.Module.Foo/
     Domain/          { Entities, Enums, Events, ValueObjects }
     Application/     { Services, Repositories, EventHandlers, ViewModels }
     Infrastructure/  { Data, Services, BackgroundServices, ... }
     Endpoints/       { FooStorefrontEndpoints, FooAdminEndpoints }
   ```
4. Expose a DI extension `AddFooModule(this IServiceCollection)` at the
   project root. Register every concrete service here; **never** use a
   reflection-based scanner for the module wiring.
5. Add `ProjectReference` from the hosts that need it:
   - `src/Apps/SimplCommerce.ApiService` (every module)
   - `src/SimplCommerce.WebHost` (legacy parallel stack)
   - `src/Migrations/SimplCommerce.Migrations` (if the module owns EF entities)
6. Add the call to `builder.Services.Add<Foo>Module()` in the
   composition roots in topological order (dependencies first).
7. If the module exposes HTTP endpoints: `app.MapFooStorefrontEndpoints()` / `app.MapFooAdminEndpoints()` in the API Program.cs.
8. If it owns entities: `dotnet ef migrations add Foo_Initial --project src/Migrations/SimplCommerce.Migrations --startup-project src/Apps/SimplCommerce.ApiService`.

## Adding a new API endpoint

Endpoint groups live inside modules, one file per (module, audience):

```csharp
// src/Modules/SimplCommerce.Module.Foo/Endpoints/FooStorefrontEndpoints.cs
public static class FooStorefrontEndpoints
{
    public static IEndpointRouteBuilder MapFooStorefrontEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/storefront/foo").WithTags("Storefront.Foo");
        group.MapGet("/", ListAsync);
        group.MapGet("/{id:long}", GetAsync);
        return app;
    }
    // Handlers use typed Results<T, ...> + FluentValidation as per ApiService convention.
}
```

Conventions:

- Paging params clamped `Math.Clamp(pageSize, 1, 100)`, default 20
- Sorted result → include an `OrderBy` so paging is stable
- Request records in the same file (local to the endpoint group) unless they're shared
- Use `Results<Ok<T>, NotFound, BadRequest>` where possible; raw `IResult` only when the shape varies per branch
- `.RequireAuthorization("policy")` — policies are `AdminOnly`, `AdminOrVendor`, `CustomerOnly`

## Adding a new Blazor page

Storefront (public, prefer InteractiveAuto):

```razor
@page "/foo/{Slug}"
@rendermode RenderMode.InteractiveAuto
@inject IFooApi Foo
<PageTitle>Foo — SimplCommerce</PageTitle>
…
```

Admin (always InteractiveServer + authorised):

```razor
@page "/foo"
@rendermode RenderMode.InteractiveServer
@inject IAdminFooApi Foo
…
```

- Every page needs `<PageTitle>`; include `<HeadContent>` for meta
  description + og tags on public pages
- MudBlazor first; drop into plain HTML only if MudBlazor doesn't ship the component
- Server-side `EditForm` + `DataAnnotationsValidator` for simple forms; `FluentValidation` for API-backed validation
- Inject the typed API client; **never** `HttpClient` directly
- Use `Snackbar` for success/error toasts

## Migration tooling

See `docs/migration/data-migration-runbook.md`. Key commands:

```bash
# generate a new migration
dotnet ef migrations add <Name> \
    --project src/Migrations/SimplCommerce.Migrations \
    --startup-project src/Apps/SimplCommerce.ApiService

# apply to a DB
dotnet ef database update \
    --project src/Migrations/SimplCommerce.Migrations \
    --startup-project src/Apps/SimplCommerce.ApiService

# production migration with backup + rollback
pwsh tools/migrate-data.ps1 -Server sql-prod -Database SimplCommerce -SignedOffBy me@example
```

## Logs & traces during development

- Aspire dashboard shows everything: logs, traces, metrics
- Seq (`https://localhost:8001` by default on the Aspire-provisioned container) for long-running log inspection
- OTLP exporter is controlled by `OTEL_EXPORTER_OTLP_ENDPOINT`; Aspire sets it automatically
- Every request now carries an `X-Correlation-Id` (client-supplied or generated) — always include it when reporting a bug

## Coding standards

- .NET 9 + C# 13 + `ImplicitUsings` + `Nullable` on for new code
- File-scoped namespaces
- `sealed` by default on classes that don't need inheritance
- Async methods take `CancellationToken` and thread it through
- FluentValidation over DataAnnotations for any API request record with non-trivial rules
- No `System.Reflection` for DI — every registration must compile

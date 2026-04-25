# SimplCommerce.ApiService.IntegrationTests

Integration tests against the ApiService composition root.

## Two suites in this project

1. **Always-green smoke** (`SmokeTests`) — trivial test so CI proves the assembly compiles even when Docker isn't available. No filter needed.
2. **Testcontainers SQL suite** (`HealthAndWebhookTests`, future files) — marked `[Trait("Category", "RequiresDocker")]`. Spins `mcr.microsoft.com/mssql/server:2022-latest` via Testcontainers, applies the consolidated `Initial_AspireBaseline` migration, and hits real endpoints through `WebApplicationFactory<Program>`.

## Running locally

```bash
# Default: skip Docker-dependent tests.
dotnet test tests/SimplCommerce.ApiService.IntegrationTests --filter "Category!=RequiresDocker"

# Requires Docker daemon accessible (local Docker Desktop, Rancher, DOCKER_HOST env, etc.).
dotnet test tests/SimplCommerce.ApiService.IntegrationTests --filter "Category=RequiresDocker"
```

First Docker run pulls the ~1.5 GB SQL image; subsequent runs reuse the cached layer. Container lifetime is scoped per `SimplApiFactory` (class fixture) — one container per test class, shared across `[Fact]` methods within.

## Running in CI

`.github/workflows/ci.yml` splits the suites:

| Job | Filter | Runner needs Docker? |
|---|---|---|
| `build-test` | `Category!=RequiresDocker` | No |
| `integration-tests` | `Category=RequiresDocker` | Yes (GitHub-hosted ubuntu-latest has it preinstalled) |

A transient Docker Hub outage red-walls only the `integration-tests` job; `build-test` stays green on PRs.

## Adding new tests

- Create a class with `[Collection("ApiServiceDb")]` and `[Trait("Category", "RequiresDocker")]`.
- Take `SimplApiFactory` via constructor DI.
- Use `factory.CreateClient()` — it targets the running container automatically.

## Historical note

Before 2026-04-23 this project was scaffold-only because the runbook
assumed `aspire run` had to boot SQL/Redis/Azurite before the tests could
start. The Testcontainers switch makes each test class hermetic; Redis /
Azurite are not wired yet because `ConnectionStrings:redis` and `:blobs`
resolve to empty-string which Aspire's `AddRedisDistributedCache` /
`AddAzureBlobServiceClient` treat as "disable". Storefront endpoints that
read through the distributed cache fall through to the DbContext, which is
the container we provisioned, so the tests still pass end-to-end. Wiring
a Redis + Azurite container is a sub-PR when we need it.

# Deployment

> Covers the Aspire-based deployment model (P8-11/P8-15). Legacy
> `Dockerfile` + `Dockerfile-sqlite` remain as reference until Phase 8
> cutover; do not use them for new deploys.

## Prerequisites on any environment

- .NET 9 runtime (container image: `mcr.microsoft.com/dotnet/aspnet:9.0`)
- SQL Server 2022+ or Azure SQL MI (connection-string-based)
- Redis 7+ (for Storefront sessions + Admin SignalR backplane)
- Blob storage (Azure or S3-compatible)
- Seq or any OTLP collector (optional but recommended for observability)

## Local — Aspire

```bash
dotnet run --project src/AppHost/SimplCommerce.AppHost
```

Aspire spins up SQL / Redis / Azurite / MailPit / Seq as Docker
containers automatically, then `api`, `storefront`, `admin`, and the
legacy `webhost` as project resources. The dashboard URL prints on
startup (default `https://localhost:17001`).

Docker is required locally; on sandboxes without Docker the runtime
verification tasks (P0-24, P1-16..P1-19, P2-47/48, P6-04/05) are
documented as blocked in `docs/migration/phase0-manual-steps.md`.

## Azure Container Apps

```bash
# 1. Publish manifests + container images via Aspire's publisher
dotnet aspire publish src/AppHost/SimplCommerce.AppHost \
    --output ./publish --publisher container-apps

# 2. Provision / update Azure infrastructure
az login
az deployment sub create \
    --location westeurope \
    --template-file ./publish/infra/main.bicep \
    --parameters environmentName=simpl-prod sqlAdminPassword=$SQL_ADMIN_PWD

# 3. Image push happens as part of `aspire publish`. First run of the
#    api container applies migrations via dotnet-ef (bundled image).
```

Required secrets / parameters:

- `Jwt:SigningKey` — 32-byte+ symmetric key, **production override mandatory** (Key Vault → env var `Jwt__SigningKey`).
- `ConnectionStrings:SimplCommerce` — full SQL connection string for production DB.
- `ConnectionStrings:redis` — Azure Cache for Redis endpoint + access key.
- `ConnectionStrings:blobs` — Storage account connection string (or MI-backed endpoint).
- `Authentication:Google:ClientId` / `ClientSecret` — if OAuth is turned on at the API.
- `Authentication:Facebook:AppId` / `AppSecret` — same.
- Payment provider keys are stored in DB (`PaymentProvider.AdditionalSettings`) and edited through the admin UI — no env var required.

## Kubernetes

Aspire can emit a Kubernetes manifest too:

```bash
dotnet aspire publish src/AppHost/SimplCommerce.AppHost \
    --output ./publish --publisher kubernetes
```

The generated manifest puts every project resource in its own
Deployment + Service. Provide your own `Ingress` / `IngressController`
and persistence volumes for SQL/Redis if you're not bringing
cloud-managed dependencies.

## Self-hosted Docker Compose

Outside Aspire, a minimal compose topology is:

```yaml
services:
  sql:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      ACCEPT_EULA: Y
      MSSQL_SA_PASSWORD: <strong>
    ports: ["1433:1433"]
    volumes: [sqldata:/var/opt/mssql]
  redis:
    image: redis:7-alpine
    ports: ["6379:6379"]
  azurite:
    image: mcr.microsoft.com/azure-storage/azurite
    ports: ["10000:10000", "10001:10001", "10002:10002"]
  api:
    image: ghcr.io/yourorg/simpl-api:<sha>
    environment:
      ConnectionStrings__SimplCommerce: "Server=sql;Database=SimplCommerce;User Id=sa;Password=<strong>;TrustServerCertificate=true"
      ConnectionStrings__redis: "redis:6379"
      ConnectionStrings__blobs: "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=…;BlobEndpoint=http://azurite:10000/devstoreaccount1"
      Jwt__SigningKey: ${JWT_SIGNING_KEY}
    depends_on: [sql, redis, azurite]
  storefront:
    image: ghcr.io/yourorg/simpl-storefront:<sha>
    environment:
      services__api__https__0: https://api
  admin:
    image: ghcr.io/yourorg/simpl-admin:<sha>
    environment:
      services__api__https__0: https://api
      ConnectionStrings__redis: "redis:6379"
```

A production-ready `compose.yaml` is the final P8-15 deliverable.

## First-run migration

The API container does **not** run migrations automatically — run the
runbook in `docs/migration/data-migration-runbook.md` manually the first
time, or attach an init container that invokes the EF bundle:

```bash
dotnet ef migrations bundle \
    --project src/Migrations/SimplCommerce.Migrations \
    --startup-project src/Apps/SimplCommerce.ApiService \
    --output publish/efbundle \
    --self-contained
```

Ship that bundle as part of the image and run it against the target DB
once, before starting the API replicas.

## Observability

- **Aspire dashboard** (dev): full traces + logs + metrics, no configuration
- **Seq** (the `seq` Aspire resource): structured log browser
- **Prometheus / Grafana**: scrape the OTLP exporter — meter `SimplCommerce` publishes `simpl.orders.created`, `simpl.payments.failed`, `simpl.carts.abandoned` plus the standard .NET runtime + ASP.NET Core metrics

## Scaling knobs

| Layer | Knob | Notes |
|---|---|---|
| API | Replica count | Stateless — scale horizontally. Rate limiter is per-instance; deploy an edge proxy (YARP, Envoy) with its own limiter when traffic > 1k RPS |
| Storefront | Replica count | Stateless — WASM download is cache-friendly; pair with CDN |
| Admin | Replica count | SignalR **requires Redis backplane** (`ConnectionStrings:redis` set). With backplane, scale freely |
| SQL | Vertical | Standard tier for up to ~500k orders/year; add read-replica for reporting only |
| Redis | Vertical | 2 GB is enough; shard only if guest-cart dwell time exceeds a day |

# Baseline NuGet & Toolchain Versions

> Ghi nhận chính xác version hiện tại (commit HEAD, 2026-04-17). Phục vụ so sánh sau migration.

## .NET

- `global.json` (trước Phase 0): .NET SDK `8.0.0` `latestMinor`, `allowPrerelease=false`
- `global.json` (sau P0-19): **.NET SDK `9.0.100` `latestMinor`** (Phase 0 update); runtime thực tế `9.0.313`
- Module Directory.Build.props: `<TargetFramework>net9.0</TargetFramework>` (bumped Phase 1 — DECISION-008)
- WebHost csproj: `net9.0` (bumped Phase 1)
- Infrastructure + test projects: `net9.0` (bumped Phase 1)

## Aspire

- **Chốt thực tế:** `13.2.2` (Microsoft semver độc lập — DECISION-006)
- AppHost SDK: `Aspire.AppHost.Sdk 13.2.2`
- Hosting packages: `Aspire.Hosting.AppHost`, `Aspire.Hosting.SqlServer`, `Aspire.Hosting.Redis`, `Aspire.Hosting.Azure.Storage`, `Aspire.Hosting.Seq` — tất cả 13.2.2
- CommunityToolkit: `CommunityToolkit.Aspire.Hosting.MailPit 13.1.1` (MailDev không có official package — DECISION-007)

## WebHost package references

| Package | Version |
|---|---|
| IdentityServer4.AspNetIdentity | 4.1.2 *(deprecated — drop trong Phase 3)* |
| BuildBundlerMinifier | 3.2.449 *(drop trong Phase 8)* |
| Microsoft.AspNetCore.Antiforgery | 2.2.0 *(legacy version – framework-shipped in 8+)* |
| Microsoft.AspNetCore.Authentication.Facebook | 8.0.0 |
| Microsoft.AspNetCore.Authentication.Google | 8.0.0 |
| Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore | 8.0.0 |
| Microsoft.AspNetCore.Mvc.NewtonsoftJson | 8.0.0 |
| Microsoft.EntityFrameworkCore.Tools | 8.0.0 |
| Microsoft.EntityFrameworkCore.SqlServer | 8.0.0 |
| Serilog.Extensions.Logging | 7.0.0 |
| Serilog.Settings.Configuration | 7.0.1 |
| Serilog.Sinks.RollingFile | 3.3.1-dev-00771 |
| Humanizer | 2.14.1 |
| MediatR | **12.1.1** (đã sẵn 12.x → task P2-42 hầu như NO-OP, chỉ verify config) |
| Swashbuckle.AspNetCore | 6.5.0 *(drop trong Phase 3 đổi sang Scalar)* |

## Module-level package highlights

| Package | Version | Module |
|---|---|---|
| Stripe.net | 22.8.0 | PaymentStripe |
| Braintree | 5.20.0 | PaymentBraintree |
| SendGrid | 9.28.1 | EmailSenderSendgrid |
| MailKit | 4.2.0 | EmailSenderSmtp |
| Azure.Storage.Blobs | 12.18.0 | StorageAzureBlob |
| AWSSDK.S3 | 3.7.205.15 | StorageAmazonS3 |
| DinkToPdf | (xem csproj) | DinkToPdf |
| Hangfire.AspNetCore | (xem csproj) | HangfireJobs |
| Hangfire.SqlServer | (xem csproj) | HangfireJobs |

## Database

- SQL Server (primary) — connection string `Server=.;Database=SimplCommerce;...`
- PostgreSQL: có file `Dockerfile-sqlite` (tên gây nhầm, thực chất vẫn sqlserver dev) và chi tiết docker-entrypoint. Prompt CLAUDE_CODE_PROMPT chốt giữ SQL Server làm primary và drop PostgreSQL. Hiện tại không có PostgreSQL provider reference trong csproj nên không cần action ở Phase 0.

## Frontend runtime (sẽ bị xoá Phase 8)

- AngularJS 1.6.x (bundled via BuildBundlerMinifier + bundleconfig.json + libman.json)
- Bootstrap
- Các thư viện bundler: xem `libman.json` trong WebHost

## Action cần làm khi có môi trường dotnet

1. Verify: `dotnet --info` → .NET 9.0.x present.
2. `dotnet build SimplCommerce.sln` PASS (ghi thời gian baseline).
3. `dotnet list src/SimplCommerce.WebHost package --include-transitive > docs/migration/baseline-packages-full.txt` để có snapshot đầy đủ package graph.
4. `dotnet ef migrations script --project src/SimplCommerce.WebHost --idempotent > docs/migration/baseline-schema.sql`

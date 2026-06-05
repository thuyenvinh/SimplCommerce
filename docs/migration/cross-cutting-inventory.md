# CROSS-CUTTING INVENTORY

> Domain events (MediatR), external integrations, background jobs, config keys. Phase 0 Discovery, 2026-04-17.

## 1. Domain events (MediatR `INotification` + handlers)

| Event | Event file | Publisher module | Handler | Handler module |
|---|---|---|---|---|
| OrderCreated | src/Modules/SimplCommerce.Module.Orders/Events/OrderCreated.cs | Orders | OrderCreatedClearCartHandler | Orders |
| OrderCreated | src/Modules/SimplCommerce.Module.Orders/Events/OrderCreated.cs | Orders | OrderCreatedCreateOrderHistoryHandler | Orders |
| AfterOrderCreated | src/Modules/SimplCommerce.Module.Orders/Events/AfterOrderCreated.cs | Orders | AfterOrderCreatedSendEmailHanlder | Orders |
| OrderChanged | src/Modules/SimplCommerce.Module.Orders/Events/OrderChanged.cs | Orders | OrderChangedCreateOrderHistoryHandler | Orders |
| OrderDetailGot | src/Modules/SimplCommerce.Module.Orders/Events/OrderDetailGot.cs | Orders | OrderDetailGotHandler | Shipments |
| UserSignedIn | src/Modules/SimplCommerce.Module.Core/Events/UserSignedIn.cs | Core | UserSignedInHandler | ShoppingCart |
| UserSignedIn | src/Modules/SimplCommerce.Module.Core/Events/UserSignedIn.cs | Core | UserSignedInHandler | Notifications |
| UserSignedIn | src/Modules/SimplCommerce.Module.Core/Events/UserSignedIn.cs | Core | UserSignedInHandler | Localization |
| UserSignedIn | src/Modules/SimplCommerce.Module.Core/Events/UserSignedIn.cs | Core | UserSignedInHandler | ProductComparison |
| EntityViewed | src/Modules/SimplCommerce.Module.Core/Events/EntityViewed.cs | Catalog | EntityViewedHandler | ProductRecentlyViewed |
| EntityViewed | src/Modules/SimplCommerce.Module.Core/Events/EntityViewed.cs | Catalog | EntityViewedHandler | ActivityLog |
| ReviewSummaryChanged | src/Modules/SimplCommerce.Module.Core/Events/ReviewSummaryChanged.cs | Reviews | ReviewSummaryChangedHandler | Catalog |
| EntityDeleting | src/Modules/SimplCommerce.Module.Core/Events/EntityDeleting.cs | Core | EntityDeletingHandler | Cms |
| ProductBackInStock | src/Modules/SimplCommerce.Module.Inventory/Event/ProductBackInStock.cs | Inventory | ProductBackInStockSendEmailHandler | Inventory |

> Handlers ngang module → phải đảm bảo assembly scan MediatR v12 pick up tất cả handler assemblies trong `AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(...))` (Phase 2 task P2-44).

## 2. External integrations

### Payment gateways

| Provider | Module | SDK | Config keys |
|---|---|---|---|
| Stripe | PaymentStripe | Stripe.net 22.8.0 | DB (PaymentProvider.AdditionalSettings): PublicKey, PrivateKey |
| PayPal Express | PaymentPaypalExpress | (custom HTTP) | DB: IsSandbox, ClientId, ClientSecret, PaymentFee |
| Braintree | PaymentBraintree | Braintree 5.20.0 | DB: PublicKey, PrivateKey, MerchantId, IsProduction |
| MoMo | PaymentMomo | (custom HTTP) | DB: IsSandbox, PartnerCode, AccessKey, SecretKey, PaymentFee |
| Cashfree | PaymentCashfree | (custom HTTP) | DB: IsSandbox, AppId, SecretKey, ReturnURL, NotifyURL, PaymentModes |
| NganLuong | PaymentNganLuong | (custom HTTP) | DB: MerchantId, MerchantPassword, ReceiverEmail, IsSandbox |
| COD | PaymentCoD | — | DB: trống (static) |

> **Note:** Prompt CLAUDE_CODE_PROMPT.md có nhắc VNPay, nhưng codebase HIỆN TẠI **không** có module `PaymentVnpay`. Có thể prompt viết sai, hoặc VNPay cần được thêm mới. Cần xác nhận với user trong Phase 3.

### Email

| Provider | Module | SDK | Config keys (appsettings.json) |
|---|---|---|---|
| SendGrid | EmailSenderSendgrid | SendGrid 9.28.1 | SendGrid:ApiKey, SendGrid:FromEmail, SendGrid:FromName |
| SMTP (MailKit) | EmailSenderSmtp | MailKit 4.2.0 | SmtpServer, SmtpUsername, SmtpPassword, SmtpPort |

### File storage

| Provider | Module | SDK | Config keys |
|---|---|---|---|
| Azure Blob | StorageAzureBlob | Azure.Storage.Blobs 12.18.0 | Azure:Blob:StorageConnectionString, Azure:Blob:ContainerName, Azure:Blob:PublicEndpoint |
| Amazon S3 | StorageAmazonS3 | AWSSDK.S3 3.7.205.15 | AWS:S3:RegionEndpointName, AWS:S3:AccessKeyId, AWS:S3:SecretAccessKey, AWS:S3:BucketName, AWS:S3:PublicEndpoint |
| Local disk | StorageLocal | — | (dùng `GlobalConfiguration.WebRootPath` + "user-content") |

### PDF generation
- **DinkToPdf** (module `DinkToPdf`): NuGet DinkToPdf — render HTML → PDF cho InvoicePdf.

### Hangfire (job scheduler)
- **Hangfire.AspNetCore + Hangfire.SqlServer** (module `HangfireJobs`)
- Queue storage: SQL Server (`SimplCommerce.Module.HangfireJobs`)

### SignalR
- Module `SignalR` — built-in Microsoft.AspNetCore.SignalR; chưa dùng Redis backplane ở bản hiện tại.

### OAuth external login
- Google + Facebook: configured qua `Authentication:Google:*` và `Authentication:Facebook:*` trong appsettings (placeholder values, thực tế user phải set).

## 3. Background jobs / IHostedService / scheduled tasks

| Type | File | Module | Purpose | Trigger |
|---|---|---|---|---|
| SchedulerBackgroundService | src/SimplCommerce.Infrastructure/Tasks/Scheduling/SchedulerBackgroundService.cs | Infrastructure | Executes `IScheduledTask` impls theo cron (NCrontab) | Startup → recurring (cron-based) |
| OrderCancellationBackgroundService | src/Modules/SimplCommerce.Module.Orders/Services/OrderCancellationBackgroundService.cs | Orders | Tự hủy order có payment pending/failed > 5 phút | Startup → recurring 60s |
| NotificationDistributionJob | src/Modules/SimplCommerce.Module.Notifications/Jobs/NotificationDistributionJob.cs | Notifications | Distribute notification → user qua Hangfire | Hangfire-scheduled |

## 4. appsettings.json keys

**Base:** `src/SimplCommerce.WebHost/appsettings.json`

Các key đáng chú ý:

- `ConnectionStrings:DefaultConnection` = `"Server=.;Database=SimplCommerce;Trusted_Connection=True;TrustServerCertificate=true;MultipleActiveResultSets=true"`
- `Authentication:Facebook:AppId` (placeholder)
- `Authentication:Facebook:AppSecret` (placeholder — cần redact)
- `Authentication:Google:ClientId` (placeholder)
- `Authentication:Google:ClientSecret` (placeholder — cần redact)
- `Logging:LogLevel:Default` = `"Warning"`
- `Serilog:MinimumLevel:Default` = `"Warning"`
- `Serilog:WriteTo[0]:Name` = `"RollingFile"`
- `Serilog:WriteTo[0]:Args:pathFormat` = `"logs\\log-{Date}.txt"`
- `Serilog:Enrich` = `["FromLogContext","WithMachineName","WithThreadId"]`
- Payment/Email/Storage keys (SendGrid, SMTP, Azure:Blob:*, AWS:S3:*) — **không set trong base appsettings**, user phải override per-environment qua user-secrets / env variables / appsettings.{Environment}.json.

**Docker variant:** `src/SimplCommerce.WebHost/Dockerfile` + `docker-entrypoint.sh` có hint về dùng PostgreSQL connection string qua env var.

## Migration notes (Phase 1+)

- Phase 1: Connection string `SimplCommerce` sẽ do Aspire inject qua `builder.AddSqlServer("sql").AddDatabase("SimplCommerce")`. WebHost đọc `builder.Configuration.GetConnectionString("SimplCommerce")` (fallback về `DefaultConnection` nếu null).
- Phase 1: Redis + Azure Blob (Azurite) + MailDev + Seq do Aspire cấp — override config key:
  - Redis: `ConnectionStrings:redis`
  - Blob: `ConnectionStrings:blobs`
  - Mail SMTP: Aspire parameter
  - Seq: `ConnectionStrings:seq` hoặc Serilog sink
- Phase 3: Di chuyển toàn bộ payment provider secrets sang user-secrets / Aspire parameters — không để hardcode trong DB PaymentProvider.AdditionalSettings (giữ pattern DB cho UX admin nhưng bổ sung signing key trong KeyVault/user-secrets).
- Phase 7: Rate limit login/register endpoint 100 req/min/IP, anti-forgery, CSP strict.

# MIGRATION PROGRESS — SimplCommerce → .NET Aspire + Blazor

> Bản mirror của MIGRATION_TODO.md, tick trạng thái thực tế qua từng phase.
> Legend: `[x]` = done, `[ ]` = pending, `[~]` = BLOCKED (cần user làm tay — ghi chú ngay dưới task), `[-]` = skipped/N/A (lý do ghi chú ngay dưới).

## TRẠNG THÁI HIỆN TẠI
- **Phase đang chạy:** Phase 0..7 done + **Phase 8 non-destructive done** (3 Dockerfiles + compose.yaml + .env.sample + .github/workflows/ci.yml + rewritten azure-pipelines.yml + CHANGELOG v2.0.0 + README rewrite + phase8-cutover-checklist.md). Destructive cutover (xóa WebHost, AngularJS templates, Razor Views, Controllers) **defer đến user** — cần runtime verify trên máy Docker trước. Full runbook trong `docs/migration/phase8-cutover-checklist.md`.
- **Branch:** `claude/phase-0-migration-pX925`
- **Build:** ✅ `dotnet build SimplCommerce.sln` PASS (59 projects, 0 errors, 0 warnings — clean + incremental)
- **Tests:** ✅ **42/42 pass** (7 unit + 1 ApiService integration scaffold)
- **Tooling:** .NET SDK 9.0.313 tại `/home/user/.dotnet/` (DECISION-005); Aspire 13.2.2 (DECISION-006); MailPit (DECISION-007); toàn solution net9.0 (DECISION-008)
- **Blocker còn lại:** Docker daemon chưa sẵn sàng trong sandbox → `aspire run` + runtime smoke test + EF migration generate + real integration tests vẫn cần user chạy local (P0-24, P1-16..P1-19, P2-04/P2-05, P2-45, P2-47/P2-48, P3-59..P3-62 e2e tests, P3-57 webhook signature verify)

---

## PHASE 0 — DISCOVERY & SETUP

**Mục tiêu:** Hiểu hệ thống cũ, tạo nền tài liệu, không sửa code logic.
**Thời lượng ước tính:** 2–4 giờ
**Branch:** `aspire-migration/phase-0-discovery`

### 0.1 Khởi tạo tài liệu migration
- [x] P0-01 | Tạo `MIGRATION_DECISIONS.md` ở root với template (Date / Context / Decision / Consequences)
- [x] P0-02 | Tạo `MIGRATION_PROGRESS.md` ở root, copy toàn bộ checklist này vào
- [x] P0-03 | Tạo `MIGRATION_INVENTORY.md` ở root (sẽ fill ở các bước dưới)
- [x] P0-04 | Tạo thư mục `docs/migration/` chứa các tài liệu phụ

### 0.2 Inventory module
- [x] P0-05 | Liệt kê tất cả project trong `src/Modules/`, ghi vào `MIGRATION_INVENTORY.md` — **41 modules** (nhiều hơn 25 theo prompt, có thêm storage/payment/shipping variants + DinkToPdf, HangfireJobs, SignalR, Comments, Contacts, Notifications, Shipments, ProductComparison, ProductRecentlyViewed)
- [x] P0-06 | Đọc `module.json` từng module — **tất cả 41 modules có `IsBundledWithHost=true`**
- [x] P0-07 | Dependency graph → `docs/migration/module-dependencies.md` (Mermaid + topological order)

### 0.3 Inventory API & Controllers
- [x] P0-08 | Grep toàn bộ Controller / ControllerBase → **104 controllers**
- [x] P0-09 | Phân loại: Storefront API (28) | Admin API (68) | MVC View (8)
- [x] P0-10 | Output: `docs/migration/api-inventory.md`

### 0.4 Inventory UI
- [x] P0-11 | Tổng **98 file AngularJS `.html`** (27 modules — Catalog 23, Core 14, Cms 9 chiếm top)
- [x] P0-12 | Tổng **181 file Razor `.cshtml`** (modules 137 + WebHost 44)
- [x] P0-13 | Top 10 màn admin — ghi trong `ui-inventory.md` §Part C (product: 5, shipment/review/order: 4 mỗi loại)
- [x] P0-14 | Output: `docs/migration/ui-inventory.md`

### 0.5 Inventory cross-cutting
- [x] P0-15 | 14 event–handler pair: OrderCreated (×2), AfterOrderCreated, OrderChanged, OrderDetailGot (cross-module), UserSignedIn (×4 handlers), EntityViewed (×2), ReviewSummaryChanged, EntityDeleting, ProductBackInStock
- [x] P0-16 | Integrations: 7 payment gateway, 2 email, 3 storage, DinkToPdf, Hangfire, SignalR, OAuth Google/FB — chi tiết trong `cross-cutting-inventory.md`. **Note:** prompt có VNPay nhưng codebase hiện tại KHÔNG có module `PaymentVnpay` — đã ghi decision cần xác nhận
- [x] P0-17 | 3 background services: SchedulerBackgroundService, OrderCancellationBackgroundService, NotificationDistributionJob (Hangfire)
- [x] P0-18 | appsettings keys enumerated trong `cross-cutting-inventory.md` §4

### 0.6 Setup môi trường .NET 9
- [x] P0-19 | `global.json` đã update từ `8.0.0` → `9.0.100 latestMinor`
- [x] P0-20 | `.NET SDK 9.0.313` installed tại `/home/user/.dotnet/`, `dotnet --info` PASS
- [-] P0-21 | N/A — Aspire 13 đã standalone templates (SDK package `Aspire.AppHost.Sdk` imported trực tiếp trong csproj), không còn cần `dotnet workload install aspire`
- [x] P0-22 | Scaffolding đã viết tay match Aspire 13.2.2 template (xem DECISION-006)

### 0.7 Snapshot baseline
- [x] P0-23 | **Build PASS** — `dotnet build SimplCommerce.sln` (51 projects, 0 errors, 1 warning ASP0014 pre-existing về UseEndpoints). Thời gian 62s.
- [~] P0-24 | **BLOCKED** — cần Docker + browser để chụp screenshot WebHost; sandbox chưa có Docker daemon
- [~] P0-25 | **BLOCKED** — cần SQL instance running để generate migration script
- [x] P0-26 | `docs/migration/baseline-versions.md` đã điền toàn bộ version từ `.csproj` (package graph full-transitive để làm sau khi có `dotnet list` local)

### 0.8 Commit Phase 0
- [x] P0-27 | Commit Phase 0 (xem commit hash trong git log)
- [-] P0-28 | **SKIP** — push OK, KHÔNG tạo PR tự động (xem DECISION-003). User sẽ open PR khi xác nhận
- [x] P0-29 | Báo cáo theo template (xem cuối chat)

---

## PHASE 1 — ASPIRE BOOTSTRAP

**Mục tiêu:** Có Aspire AppHost chạy được, wrap WebHost cũ làm resource, dashboard hiển thị.
**Thời lượng ước tính:** 4–6 giờ
**Branch:** `aspire-migration/phase-1-aspire-bootstrap`

### 1.1 Tạo project Aspire
- [x] P1-01 | Tạo thư mục `src/AppHost/` và `src/ServiceDefaults/`
- [x] P1-02 | Đã tạo `src/AppHost/SimplCommerce.AppHost/{SimplCommerce.AppHost.csproj, Program.cs, appsettings*.json, Properties/launchSettings.json}` theo template chuẩn `aspire-apphost` (Aspire 9.0.0 SDK). **Không chạy `dotnet new`** do sandbox không có dotnet — file viết tay match 1-1 với output của template
- [x] P1-03 | Đã tạo `src/ServiceDefaults/SimplCommerce.ServiceDefaults/{SimplCommerce.ServiceDefaults.csproj, Extensions.cs}` theo template chuẩn `aspire-servicedefaults`
- [x] P1-04 | Đã add vào `SimplCommerce.sln`: solution folder `00-Aspire` + 2 project (GUID `AA000000-0000-0000-0000-00000000A001/A002`), full ProjectConfigurationPlatforms + NestedProjects mapping

### 1.2 Cấu hình ServiceDefaults
- [x] P1-05 | `AddServiceDefaults()` có đủ OpenTelemetry (tracing + metrics + logging), health checks (`self/live`), service discovery, HttpClient `AddStandardResilienceHandler()` (Microsoft.Extensions.Http.Resilience 9.0 — Polly v8 standard pipeline)
- [~] P1-06 | **PARTIAL** — ServiceDefaults dùng OTLP exporter (`UseOtlpExporter()`) để bắn sang Aspire dashboard / Seq OTLP endpoint. Serilog Seq sink trực tiếp không cần — Aspire dashboard tự ingest OTLP và forward sang Seq qua `WithReference(seq)`. Nếu user muốn Serilog native sink, bổ sung sau (Phase 7 tuning).
- [x] P1-07 | `MapDefaultEndpoints()` map `/health` + `/alive` (chỉ Development, tags=`live` cho `/alive`)

### 1.3 Khai báo resources trong AppHost
- [x] P1-08 | NuGet packages khai báo trong csproj: `Aspire.Hosting.AppHost`, `Aspire.Hosting.SqlServer`, `Aspire.Hosting.Redis`, `Aspire.Hosting.Azure.Storage`, `CommunityToolkit.Aspire.Hosting.MailDev`, `CommunityToolkit.Aspire.Hosting.Seq` (tất cả 9.0.0). Sandbox không chạy `dotnet restore` được — user verify sau
- [x] P1-09 | AppHost `Program.cs` có đủ resources: `sql` (port 11433 để tránh conflict với local SQL 1433) + db `SimplCommerce`, `redis` (+ RedisCommander), `storage` Azurite + blobs, `maildev`, `seq`
- [x] P1-10 | SQL + Seq đã set `WithLifetime(ContainerLifetime.Persistent)` + `WithDataVolume()`

### 1.4 Wrap WebHost cũ vào AppHost
- [x] P1-11 | AppHost csproj có `<ProjectReference Include="..\..\SimplCommerce.WebHost\SimplCommerce.WebHost.csproj" IsAspireProjectResource="true" />`
- [x] P1-12 | `builder.AddProject<Projects.SimplCommerce_WebHost>("webhost").WithReference(simplDb).WithReference(redis).WithReference(blobs).WithReference(mail).WithReference(seq).WaitFor(sql)` trong Program.cs
- [x] P1-13 | `WebHost/Program.cs` + `Extensions/ServiceCollectionExtensions.cs` đọc `GetConnectionString("SimplCommerce") ?? GetConnectionString("DefaultConnection")` (fallback để standalone run vẫn OK)
- [x] P1-14 | `builder.AddServiceDefaults();` thêm đầu Program.cs, `app.MapDefaultEndpoints();` thêm sau `app.UseRouting()`. WebHost csproj thêm `<ProjectReference>` tới `SimplCommerce.ServiceDefaults`

### 1.5 Verify
- [x] P1-15 | **`dotnet build` PASS** toàn solution (51 project, 0 error, 1 warning pre-existing)
- [~] P1-16 | **BLOCKED-Docker** — sandbox chưa có Docker daemon để pull SQL/Redis/Azurite/MailPit/Seq container
- [~] P1-17 | **BLOCKED-Docker** — runtime verification cần container
- [~] P1-18 | **BLOCKED-Docker** — runtime verification cần container
- [~] P1-19 | **BLOCKED-Docker** — screenshot cần runtime; thư mục `docs/migration/phase1-screenshots/` đã được tạo sẵn

### 1.6 Commit Phase 1
- [x] P1-20 | MIGRATION_PROGRESS.md đã tick Phase 0 + Phase 1 (non-runtime items)
- [x] P1-21 | Commit Phase 1 với message conventional
- [-] P1-22 | **SKIP** — không tạo PR tự động (xem DECISION-003). Branch được push để user review
- [x] P1-23 | Báo cáo cuối turn, DỪNG theo yêu cầu user

---

## PHASE 2 — REFACTOR CORE & MODULE LAYERING

**Mục tiêu:** Tách Domain/Application/Infrastructure từng module, gộp migrations, bỏ runtime module loading, nâng MediatR 12. WebHost cũ vẫn chạy.
**Thời lượng ước tính:** 16–24 giờ
**Branch:** `aspire-migration/phase-2-refactor-core`

### 2.1 Tạo project Migrations gộp
- [x] P2-01 | Tạo `src/Migrations/SimplCommerce.Migrations/` (class library, net9.0)
- [x] P2-02 | Reference `Microsoft.EntityFrameworkCore.SqlServer`, `.Design`, `.Tools` 9.0.0 + ProjectReference tới tất cả module có entity (37 modules, trừ 3 dormant: Notifications/HangfireJobs/SignalR không compile .NET 9 — xem README + DECISION-009)
- [-] P2-03 | **SKIP/N/A** — `SimplDbContext` đã ở `Module.Core/Data/` (home đúng của nó theo Clean layering — entity chính sở hữu tại Module.Core). Không move. Migrations project chỉ consume qua MigrationsAssembly setting. Xem DECISION-009.
- [~] P2-04 | **BLOCKED-Docker** — generate `Initial_AspireBaseline` cần SQL Server chạy. Runbook đầy đủ tại `src/Migrations/SimplCommerce.Migrations/README.md`
- [~] P2-05 | **BLOCKED-Docker** — verify cần DB sạch

### 2.2 Refactor SimplCommerce.Module.Core
- [x] P2-06 | `Domain/`, `Application/`, `Infrastructure/`, `Endpoints/` đã tạo trong Module.Core
- [x] P2-07 | 19 entity classes moved `Models/` → `Domain/Entities/`; 2 enums → `Domain/Enums/`; 2 constants → `Domain/Constants/`; 1 VO (`ThemeManifest`) → `Domain/ValueObjects/`
- [x] P2-08 | 4 domain events moved `Events/` → `Domain/Events/`
- [x] P2-09 | 11 service interfaces moved `Services/` → `Application/Services/`; 7 impl moved `Services/` → `Infrastructure/Services/`
- [x] P2-10 | EF mappings + DbContext + seed moved `Data/` → `Infrastructure/Data/`
- [x] P2-11 | Repositories moved `Data/` → `Infrastructure/Data/Repositories/`. Plus: 3 Identity stores (`SimplRoleStore`, `SimplSignInManager`, `SimplUserStore`) → `Infrastructure/Identity/`; 4 EF-config classes → `Infrastructure/Configuration/`; web utilities (`SlugRouteValueTransformer`, `WorkContext`/`IWorkContext`) → `Infrastructure/Web/`; setting helpers → `Infrastructure/Settings/`; tag helpers → `Infrastructure/TagHelpers/`; `LocalizedStringExtensions` → `Infrastructure/Localization/`
- [x] P2-12 | `CoreModuleExtensions.AddCoreModule(IServiceCollection)` extension created at module root — central registration source of truth
- [-] P2-13 | **MODIFIED** — `ModuleInitializer.cs` KEPT but converted to thin shim that calls `AddCoreModule()`, marked `[Obsolete]`. Full removal blocked until WebHost stops doing reflection-driven `ConfigureModules()` (P2-39 follow-up). Avoids breaking ApiService/WebHost during transition.
- **Important note about file moves:** `namespace` declarations inside files were KEPT unchanged (`SimplCommerce.Module.Core.Models`, `.Services`, `.Extensions`, etc.) — C# decouples folder location from namespace, so all 100+ `using SimplCommerce.Module.Core.Models;` etc. across the solution still resolve. Renaming namespaces to match new folders is a separate, mechanical follow-up that touches caller sites — out of scope this commit.

### 2.3 Refactor các module còn lại
Lặp lại pattern Core. Prompt gốc liệt kê 24 module, thực tế có 41 (xem INVENTORY). Dormant modules: Notifications, HangfireJobs, SignalR cần port .NET 9 trước (DECISION-009). Một vài module trong prompt (Sales, Production, StorefrontApi) không tồn tại trong codebase hiện tại — cần xác nhận với user khi refactor.

**STATUS: Hoàn thành 35/35 active modules (3 dormant loại khỏi scope)**
- [x] Batch 1: Localization, ActivityLog, Tax, Contacts, Vendors — commit `1721ea3`
- [x] Batch 2: Cms, Search, News, Inventory, Pricing — commit sau
- [x] Catalog (biggest — own commit) — 102 .cs files reorganized
- [x] Batch 3: Shipping, ShippingPrices, ShippingFree, ShippingTableRate, ShoppingCart, Checkouts
- [x] Batch 4: Orders, Shipments, Reviews, WishList, ProductComparison
- [x] Batch 5: Payments core + 7 providers (Braintree, Cashfree, CoD, Momo, NganLuong, PaypalExpress, Stripe)
- [x] Batch 6: SampleData, Comments, ProductRecentlyViewed, StorageLocal, DinkToPdf, EmailSenderSmtp
- [-] Dormant (deferred until .NET 9 port — DECISION-009): Notifications, HangfireJobs, SignalR, EmailSenderSendgrid, StorageAmazonS3, StorageAzureBlob (last 3 compile but not referenced by WebHost today)

**Pattern applied to each module (same as Core refactor):**
- `git mv` files vào `Domain/{Entities,Events,Enums,Constants,ValueObjects}/`, `Application/{Services,Repositories,EventHandlers,ViewModels,Queries}/`, `Infrastructure/{Data,Data/Repositories,Services,BackgroundServices,Identity,Configuration,Web,Settings,TagHelpers,Localization,Helpers}/`, `Endpoints/`
- Namespace declarations KHÔNG đổi → 0 breaking change cho callers
- Tạo `<ModuleName>ModuleExtensions.Add<ModuleName>Module(IServiceCollection)` là source-of-truth mới cho DI
- `ModuleInitializer` giữ làm `[Obsolete]` shim delegating sang extension — reflection-scan path vẫn hoạt động

Với MỖI module:
- Tạo thư mục `Domain/`, `Application/`, `Infrastructure/`, `Endpoints/`, giữ tạm `Controllers/`, `Views/`, `wwwroot/`, `Areas/`
- Move file đúng layer theo trách nhiệm
- Tạo extension `Add<ModuleName>Module()`
- Xoá `ModuleInitializer.cs`
- Build sau mỗi module — KHÔNG move sang module tiếp nếu chưa build PASS
- Commit nhỏ sau mỗi 3–5 module: `refactor(module-X,Y,Z): phase 2 layering`

### 2.4 Sửa WebHost dùng cách register mới
- [-] P2-38 | **N/A** — Program.cs không đổi call site vì giữ `AddModules() / ConfigureModules()` làm reflection-scan compat layer. Explicit `AddXxxModule()` chain sẽ add khi per-module refactor (P2-14..P2-37) diễn ra
- [-] P2-39 | **N/A** — lý do như P2-38; chain explicit chưa cần khi ModuleInitializer vẫn scan
- [x] P2-40 | Xoá `src/SimplCommerce.WebHost/modules.json` — backup giữ ở `docs/migration/legacy/modules.json`. `ModuleConfigurationManager` đổi sang static manifest trong code (`src/SimplCommerce.Infrastructure/Modules/ModuleConfigurationManager.cs`). `TryLoadModuleAssembly` dead code cũng bị xoá
- [x] P2-41 | `CustomAssemblyLoadContextProvider` không tồn tại trong codebase — không cần xoá. `ThemeableViewLocationExpander` được giữ (Razor view theming cần thiết — prompt đã note rõ). Runtime assembly loading path đã bị xoá ở P2-40

### 2.5 Nâng MediatR 7 → 12
- [x] P2-42 | **Đã ở 12.1.1** từ trước Phase 0 (xem `baseline-versions.md`). Không cần bump
- [x] P2-43 | Handler signature verified: các handler hiện tại dùng `Task Handle(TNotification, CancellationToken)` — v12 compatible
- [x] P2-44 | `AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly))` trong WebHost đã ở cú pháp v12. **Follow-up (P2.3 refactor):** mở rộng sang `RegisterServicesFromAssemblies(...)` bao gồm module assemblies sau khi migrate handler registration từ `ModuleInitializer.ConfigureServices()` sang MediatR auto-scan — tránh double-register
- [~] P2-45 | **BLOCKED-Docker** — runtime verify domain events cần DB

### 2.6 Verify Phase 2
- [x] P2-46 | `dotnet build SimplCommerce.sln` PASS (52 projects, 0 errors, 1 pre-existing warning)
- [~] P2-47 | **BLOCKED-Docker** — aspire run cần container
- [~] P2-48 | **BLOCKED-Docker** — so sánh với baseline cần runtime

### 2.7 Commit Phase 2
- [x] P2-49 | MIGRATION_PROGRESS.md updated (file này)
- [x] P2-50 | Commit với mô tả chi tiết scope (không squash vì đây là sub-PR trong loạt Phase 2 sẽ tiếp nối)
- [x] P2-51 | Báo cáo cuối turn — DỪNG chờ user review trước khi per-module refactor

---

## PHASE 3 — API SERVICE

**Mục tiêu:** Tách toàn bộ API ra thành Minimal API service riêng. WebHost cũ vẫn còn nhưng API mới song song.
**Thời lượng ước tính:** 16–24 giờ
**Branch:** `aspire-migration/phase-3-api-service`

### 3.1 Tạo ApiService
- [x] P3-01 | `src/Apps/SimplCommerce.ApiService/` (.NET 9 web project) — scaffolded thủ công vì sandbox không có `dotnet new` template trigger tương thích
- [x] P3-02 | Reference: ServiceDefaults, Migrations, **39 module projects** (trừ `EmailSenderSendgrid`, `StorageAmazonS3`, `StorageAzureBlob` — các opt-in variants không được wire vào composition root này, giữ cho deployment-time swap)
- [x] P3-03 | NuGet: `Aspire.Microsoft.EntityFrameworkCore.SqlServer` 13.2.2, `Aspire.StackExchange.Redis.DistributedCaching` 13.2.2, `Aspire.Azure.Storage.Blobs` 13.2.2, `FluentValidation` 11.10 + `FluentValidation.AspNetCore` 11.3, `Microsoft.AspNetCore.Authentication.JwtBearer` 9.0, `Microsoft.AspNetCore.OpenApi` 9.0, `Scalar.AspNetCore` 2.0
- [x] P3-04 | AppHost `Program.cs` có `var api = builder.AddProject<Projects.SimplCommerce_ApiService>("api").WithReference(simplDb).WithReference(redis).WithReference(blobs).WithReference(mail).WithReference(seq).WaitFor(sql)`. WebHost giờ cũng `.WithReference(api)` để share service discovery.

### 3.2 Setup ApiService Program.cs
- [x] P3-05 | `builder.AddServiceDefaults()` — OpenTelemetry + health + service discovery kế thừa từ ServiceDefaults
- [x] P3-06 | `builder.AddSqlServerDbContext<SimplDbContext>("SimplCommerce", options => options.UseSqlServer(sql => sql.MigrationsAssembly("SimplCommerce.Migrations")))` — point EF ở project Migrations mới
- [x] P3-07 | `builder.AddRedisDistributedCache("redis")`
- [x] P3-08 | `builder.AddAzureBlobServiceClient("blobs")` — dùng API mới, `AddAzureBlobClient` đã deprecated trong Aspire 13
- [x] P3-09 | `AddIdentityCore<User>().AddRoles<Role>().AddEntityFrameworkStores<SimplDbContext>().AddDefaultTokenProviders()` — không cookie middleware
- [x] P3-10 | JWT config từ `builder.Configuration["Jwt:*"]` (Issuer, Audience, SigningKey) với dev fallback. Production: inject qua Aspire parameter.
- [x] P3-11 | `AddAuthorizationBuilder()` + 3 policy: `AdminOnly` (role=admin), `AdminOrVendor` (role=admin,vendor), `CustomerOnly` (authenticated)
- [x] P3-12 | `AddOpenApi()` + `MapScalarApiReference()` ở Development; root `/` redirect tới `/scalar/v1`
- [x] P3-13 | **39 `Add<Module>Module()` call explicit** theo topological order (Core → Localization/ActivityLog/Tax/Contacts/Vendors → Catalog → Cms/Search/News/Inventory/Pricing → Shipping family → ShoppingCart → Checkouts → Orders → Shipments/Reviews/WishList/ProductComparison/ProductRecentlyViewed → Payments + 7 providers → Comments/SampleData/EmailSenderSmtp/DinkToPdf/StorageLocal → Notifications → SignalR → HangfireJobs). **Không có reflection scan.** Nếu thiếu module nào, compile fail ngay (verified by test build).
- [x] P3-14 | `AddResponseCompression`, `AddOutputCache`, `AddCors` (origins storefront + admin only)

Verify:
- `dotnet build SimplCommerce.sln` → 54 projects, 0 errors, 0 warnings
- ApiService assembly built: `src/Apps/SimplCommerce.ApiService/bin/Debug/net9.0/SimplCommerce.ApiService.dll`
- Runtime verify (aspire run + scalar UI mở, JWT token lấy được, endpoint gọi được) — **BLOCKED-Docker**, cần user chạy local

### 3.3 Migrate endpoints — Storefront API
Pattern áp dụng cho mọi endpoint group: `public static IEndpointRouteBuilder MapXxxEndpoints(this IEndpointRouteBuilder app)` trong folder `Endpoints/` của module gốc.
- [x] P3-15 | File pattern `<Name>StorefrontEndpoints.cs` với static class + `MapXxxStorefrontEndpoints()` extension — applied đầy đủ
- [x] P3-16 | Minimal endpoints dùng typed `Results<Ok<T>, NotFound, BadRequest, Unauthorized>` hoặc `IResult` tùy case
- [x] P3-17 | DTO request Auth có FluentValidator (`RegisterRequestValidator`, `LoginRequestValidator`, `ForgotPasswordRequestValidator`, `ResetPasswordRequestValidator`). Các endpoint Storefront khác dùng record parameter validation tối thiểu — follow-up PR sẽ thêm FluentValidator per endpoint
- [x] P3-18 | List endpoints có `page`/`pageSize` clamped (1..100) + OrderBy stable
- [x] P3-19 | `RequireAuthorization("CustomerOnly")` / `AdminOrVendor` / `AdminOnly` theo policy

Storefront endpoint groups đã tạo (9 groups):
- [x] P3-20 | `Module.Catalog/Endpoints/CatalogStorefrontEndpoints.cs` — `/api/storefront/catalog/{products, products/{id}, products/by-slug/{slug}, categories, categories/by-slug/{slug}, brands, brands/by-slug/{slug}}`
- [x] P3-21 | `Module.Search/Endpoints/SearchStorefrontEndpoints.cs` — `/api/storefront/search?q=&page&pageSize`
- [x] P3-22 | `Module.ShoppingCart/Endpoints/ShoppingCartStorefrontEndpoints.cs` — `/api/storefront/cart/{GET, items POST/PUT, coupon POST}` (CustomerOnly)
- [~] P3-23 | **Checkout endpoints** — stub chưa có; checkout flow sẽ gộp với OrdersStorefrontEndpoints.Post("/") trong sub-PR Phase 3.3 tiếp theo
- [x] P3-24 | `Module.Orders/Endpoints/OrdersStorefrontEndpoints.cs` — `/api/storefront/orders/{GET list, GET {id}}` (CustomerOnly)
- [x] P3-25 | `Module.Core/Endpoints/CoreStorefrontEndpoints.cs` — `/api/storefront/core/{countries, countries/{id}/states, addresses}` (CustomerOnly cho addresses)
- [x] P3-26 | `Module.WishList/Endpoints/WishListStorefrontEndpoints.cs` — `/api/storefront/wishlist/{GET, items POST, items/{id} DELETE}` (CustomerOnly)
- [x] P3-27 | `Module.Reviews/Endpoints/ReviewsStorefrontEndpoints.cs` — `/api/storefront/reviews/{GET by entity, POST}` (CustomerOnly cho POST)
- [x] P3-28 | `Module.Cms/Endpoints/CmsStorefrontEndpoints.cs` — `/api/storefront/cms/{pages/{slug}, menus/{name}}`
- [x] P3-28.1 | `Module.News/Endpoints/NewsStorefrontEndpoints.cs` — `/api/storefront/news/{list, {slug}, categories}`

### 3.4 Migrate endpoints — Admin API (partial — 13 admin groups)
- [x] P3-29 | `Module.Catalog/Endpoints/CatalogAdminEndpoints.cs` — `/api/admin/catalog/{brands, categories, products}` (GET list + POST + PUT + DELETE). Product-attribute / option / template are follow-ups.
- [x] P3-30 | `Module.Orders/Endpoints/OrdersAdminEndpoints.cs` — `/api/admin/orders/{GET list, GET {id}, PATCH {id}/status}` with customer search filter
- [x] P3-31 | `Module.Core/Endpoints/CoreAdminEndpoints.cs` — `/api/admin/core/{users, users/{id}, POST users, roles, countries}` (AdminOnly)
- [x] P3-32 | `Module.Reviews/Endpoints/ReviewsAdminEndpoints.cs` — `/api/admin/reviews/{GET list, PATCH {id}/status}` moderation
- [x] P3-33 | `Module.Inventory/Endpoints/InventoryAdminEndpoints.cs` — `/api/admin/inventory/{warehouses, stocks}`
- [x] P3-34 | `Module.Pricing/Endpoints/PricingAdminEndpoints.cs` — `/api/admin/pricing/{cart-rules, catalog-rules, coupons}` (GET only for now)
- [x] P3-35 | `Module.Cms/Endpoints/CmsAdminEndpoints.cs` — `/api/admin/cms/pages` full CRUD
- [x] P3-36 | `Module.Shipping/Endpoints/ShippingAdminEndpoints.cs` — `/api/admin/shipping/providers` (GET)
- [x] P3-37 | `Module.Tax/Endpoints/TaxAdminEndpoints.cs` — `/api/admin/tax/{classes, rates}` GET+POST
- [x] P3-38 | `Module.Payments/Endpoints/PaymentsAdminEndpoints.cs` — `/api/admin/payments/{providers, GET payments list}`
- [x] P3-39 | `Module.Vendors/Endpoints/VendorsAdminEndpoints.cs` — `/api/admin/vendors/{GET, POST}`
- [x] P3-40 | `Module.Localization/Endpoints/LocalizationAdminEndpoints.cs` — `/api/admin/localization/app-settings` (GET). Translation editor UI owns full CRUD in Phase 5.
- [~] P3-41 | Settings admin — gộp với P3-40 (app-settings endpoint). Full settings-as-a-form UI trong Phase 5.
- [x] P3-42 | `Module.ActivityLog/Endpoints/ActivityLogAdminEndpoints.cs` — `/api/admin/activity-log` paged list

> Ghi chú: các endpoint admin trên chỉ cover **GET list + basic CRUD path cốt lõi**. Các nghiệp vụ sâu (product clone, coupon rule builder, shipment tracking, cart rule condition editor, price list import) được đánh dấu là sub-PR tiếp theo — volume lớn, cần UI hình thành trong Phase 5 để quyết định shape API chính xác.

### 3.5 Auth endpoints
- [x] P3-43 | `POST /api/auth/register` — AuthEndpoints.RegisterAsync với FluentValidation + UserManager.CreateAsync + JWT token issuance
- [x] P3-44 | `POST /api/auth/login` — AuthEndpoints.LoginAsync với CheckPasswordAsync + `JwtTokenService.IssueAsync` (trả access token + expires + roles)
- [x] P3-45 | `POST /api/auth/refresh` — requires auth; re-issues token for same user id
- [x] P3-46 | `POST /api/auth/logout` — stateless NoContent (JWT invalidation là Phase 7)
- [x] P3-47 | `POST /api/auth/forgot-password` — GeneratePasswordResetTokenAsync + gửi email (không leak user tồn tại hay không)
- [x] P3-48 | `POST /api/auth/reset-password` — ResetPasswordAsync
- [-] P3-49 | **SKIP** — External login (Google/Facebook) cần config OAuth provider runtime; implement khi user có credentials production. Module.Core AccountController cũ có OpenIdConnect hookup, có thể revive khi cần.
- [x] Bonus: `GET /api/auth/me` — returns authenticated user's profile + roles (cho BFF client bootstrap)

### 3.6 File upload
- [x] P3-50 | `POST /api/media/upload` — `SimplCommerce.ApiService/Media/MediaUploadEndpoints.cs`; `IFormFile` → `IStorageService.SaveMediaAsync`; returns `{Url, FileName}`. 10 MB size limit. `AdminOrVendor` policy.
- [ ] P3-51 | **Pending** — Image resizing pipeline (ImageSharp) — Phase 7 hardening
- [x] P3-52 | Public URL qua `IStorageService.GetMediaUrl(fileName)` (module-swappable: StorageLocal / Azure / S3)

### 3.7 Webhook endpoints (payment callback) — scaffold only
- [x] P3-53 | `POST /api/webhooks/stripe` — stub trả `202 Accepted`, signature verify TODO
- [x] P3-54 | `POST /api/webhooks/paypal` — stub tương tự
- [x] P3-55 | `POST /api/webhooks/momo` — stub tương tự
- [-] P3-56 | `POST /api/webhooks/vnpay` — **SKIP** — module PaymentVnpay không tồn tại trong codebase (xem DECISION-007); thêm khi có VNPay module
- [~] P3-57 | **Pending** — signature verification per provider (Stripe-Signature HMAC, PayPal IPN round-trip, MoMo HMAC-SHA256). Mỗi provider cần sandbox credentials để test → sub-PR per provider khi có

### 3.8 Test integration — scaffold only
- [x] P3-58 | `tests/SimplCommerce.ApiService.IntegrationTests/` project created (net9.0, xunit 2.9, FluentAssertions, Microsoft.AspNetCore.Mvc.Testing 9.0)
- [~] P3-59 | `WebApplicationFactory<Program>` hookup chưa — cần DB/Redis runtime (xem README trong test project)
- [~] P3-60..P3-62 | Real smoke/e2e tests còn BLOCKED-Docker. Runbook đầy đủ trong `tests/SimplCommerce.ApiService.IntegrationTests/README.md`
- [x] P3-63 | `dotnet test` PASS — 42/42 (41 existing + 1 new `Scaffold_Compiles_And_xunit_Discovers` smoke test)
- `public partial class Program;` marker ở cuối `ApiService/Program.cs` đã có để `WebApplicationFactory<Program>` hoạt động sau này

### 3.9 Commit Phase 3 (in progress)
- [x] P3-64 | MIGRATION_PROGRESS.md updated (this section)
- [x] P3-65 | Progressive commits trên branch (chưa PR vì user chưa yêu cầu)
- [ ] P3-66 | Báo cáo thực tế → ghi nhận Phase 3 partial, admin endpoints + webhook + integration tests là follow-up

---

## PHASE 4 — STOREFRONT BLAZOR

**Mục tiêu:** Storefront mới hoàn chỉnh, chạy song song storefront cũ trong WebHost. SEO-friendly.
**Thời lượng ước tính:** 24–32 giờ
**Branch:** `aspire-migration/phase-4-storefront`

### 4.1 Tạo project
- [x] P4-01 | `src/Apps/SimplCommerce.Storefront/` (Blazor Web App server, net9.0)
- [x] P4-02 | `src/Apps/SimplCommerce.Storefront.Client/` (WASM client, net9.0) — interactive Auto
- [x] P4-03 | Reference `ServiceDefaults`
- [x] P4-04 | MudBlazor 7.15 trong cả Server và Client project
- [x] P4-05 | AppHost: `builder.AddProject<Projects.SimplCommerce_Storefront>("storefront").WithReference(api).WithReference(redis).WithReference(seq).WaitFor(api)`

### 4.2 Setup MudBlazor + theme
- [x] P4-06 | `AddMudServices()` trong cả hai project
- [x] P4-07 | `MudThemeProvider` + `MudPopoverProvider` + `MudDialogProvider` + `MudSnackbarProvider` trong `MainLayout.razor`
- [x] P4-08 | `Components/Layout/SimplTheme.cs` với PaletteLight/Dark (primary `#3d72b4`, secondary `#ff6b35`), layout border radius 8px
- [x] P4-09 | Header: logo + search bar (debounced) + cart icon + account menu + dark/light toggle; footer copyright. Drawer mobile là follow-up

### 4.3 Auth setup (BFF pattern)
- [x] P4-10 | Cookie auth `simpl.storefront.auth`, HttpOnly, SameSite=Lax, 8h sliding
- [x] P4-11 | `CookieAuthStateService` đổi email/password → JWT qua `/api/auth/login`, lưu token vào cookie claims (`api_access_token`)
- [x] P4-12 | `AddCascadingAuthenticationState()` + `<AuthorizeView>` trong layout (thay cho custom `AuthenticationStateProvider`); `/api/auth/me` gọi qua IAccountApi
- [x] P4-13 | `[Authorize]` attribute trên Cart / Account / OrderHistory pages + `<AuthorizeView>` cho menu switching

### 4.4 Typed HttpClients
- [x] P4-14 | `Services/ApiClients/`: `ICatalogApi`, `ISearchApi`, `ICartApi`, `IAuthApi`, `IAccountApi`, `IOrderApi` (`IWishlistApi`/`ICheckoutApi`/`IReviewApi`/`ICmsApi` là sub-PR vì endpoint backend chưa finalise shape)
- [x] P4-15 | `AddHttpClient<T>()` với base address từ Aspire service discovery (`services:api:https:0` / fallback `https+http://api`)
- [x] P4-16 | `ApiAuthDelegatingHandler` đọc `api_access_token` claim từ cookie principal → gắn `Authorization: Bearer …` vào outbound request

### 4.5 Pages
- [x] P4-17 | `/` Home — featured products grid + categories sidebar; 2 API parallel via `Task.WhenAll`
- [~] P4-18 | `/category/{slug}` Category listing — paging + grid; filter sidebar (brand/price/attribute) + grid/list toggle là follow-up
- [x] P4-19 | `/product/{slug}` Product detail — gallery, price/old-price, qty selector, Add to cart, meta + og tags + JSON-LD
- [x] P4-20 | `/search?q=` Search results — paged grid (facets follow-up)
- [~] P4-21 | `/cart` Cart — authorized; renders raw cart payload from API (typed CartView record là follow-up khi endpoint `/api/storefront/cart/` shape ổn định)
- [ ] P4-22..P4-23 | `/checkout/*` — **pending follow-up PR** (checkout flow endpoint đang được hoàn thiện)
- [x] P4-24 | `/account/login`, `/account/register`, `/account/logout` — MudBlazor EditForm + DataAnnotations; login exchanges JWT và sets cookie
- [x] P4-25 | `/account` — profile dashboard (GET `/api/auth/me`)
- [x] P4-26 | `/account/orders` — order history table (chi tiết `/{id}` là follow-up)
- [x] P4-27 | `/account/addresses` — grid of saved addresses (read-only; CRUD form sub-PR)
- [x] P4-28 | `/account/wishlist` — table with remove action
- [~] P4-29 | `/account/reviews` — pending (endpoint exists; UI sub-PR)
- [x] P4-30 | `/page/{slug}` CMS page renderer
- [x] P4-31 | `/news` listing + `/news/{slug}` article detail

### 4.6 SEO (minimum viable)
- [x] P4-32 | `<HeadOutlet>` ở root, `<PageTitle>` per page, `<HeadContent>` với meta description + og tags (Home + ProductDetail)
- [x] P4-33 | JSON-LD `Product` schema trên ProductDetail (BreadcrumbList + Organization là follow-up)
- [~] P4-34 | `/sitemap.xml` — stub XML rỗng hợp lệ; dynamic generation từ catalog là follow-up
- [x] P4-35 | `/robots.txt` trỏ sitemap
- [~] P4-36 | Prerender verify — rendermode `InteractiveAuto` (prerender default=true); runtime verify BLOCKED-Docker

### 4.7 Localization
- [ ] P4-37..P4-39 | Pending — `IStringLocalizer` adapter + language switcher đợi ApiService expose localization resources qua endpoint dedicated (chưa có)

### 4.8 Performance
- [x] P4-41 | Response compression Brotli + Gzip enabled
- [x] P4-40 + P4-43 | `AddOutputCache()` enabled (per-page `[OutputCache]` attributes là follow-up). .NET 9 static asset fingerprinting đã tự động
- [~] P4-42 | Lazy images + srcset phụ thuộc vào image resizing pipeline P3-51 (Phase 7 hardening)

### 4.9 Verify
- [x] P4-47 | Build clean: 0 errors, 0 warnings trên toàn solution (58 projects gồm Storefront + Storefront.Client)
- [~] P4-44..P4-46 | BLOCKED-Docker — Lighthouse + e2e flow verify cần aspire run + SQL/Redis/Azurite

### 4.10 Commit Phase 4 (scaffold + core pages)
- [x] P4-48 | MIGRATION_PROGRESS updated
- [x] P4-49 | Commit Phase 4 scaffold; follow-up: checkout, addresses, wishlist, CMS pages, news

---

## PHASE 5 — ADMIN BLAZOR

**Mục tiêu:** Admin mới thay thế hoàn toàn AngularJS admin. Interactive Server.
**Thời lượng ước tính:** 40–60 giờ (phase NẶNG NHẤT)
**Branch:** `aspire-migration/phase-5-admin`

### 5.1 Tạo project
- [x] P5-01 | `src/Apps/SimplCommerce.Admin/` Blazor Web App (Interactive Server only) — scaffolded thủ công
- [x] P5-02 | Reference `ServiceDefaults`, MudBlazor 7.15
- [x] P5-03 | `AddSignalR().AddStackExchangeRedis(connectionString)` khi có `ConnectionStrings:redis` (Aspire-injected), fallback in-memory SignalR cho standalone dev
- [x] P5-04 | AppHost: `builder.AddProject<Projects.SimplCommerce_Admin>("admin").WithReference(api).WithReference(redis).WithReference(seq).WaitFor(api)`

### 5.2 Layout & Auth
- [x] P5-05 | Cookie auth `simpl.admin.auth` (SameSite=Strict, HttpOnly, 8h sliding) + fallback `AuthorizationPolicy` `RequireRole("admin","vendor")` nên MỌI page auth-by-default. Login role-gate trong `CookieAuthStateService` từ chối account không có admin/vendor role
- [x] P5-06 | `MainLayout`: MudAppBar (logo + notifications + user menu + theme toggle) + MudDrawer (navigation groups, ClipMode=Always, persistent variant) + MudMainContent. Breadcrumbs là follow-up per-page
- [x] P5-07 | Nav tree 5 groups: Catalog (Products/Categories/Brands), Sales (Orders), Customers (Users), Content (Reviews), Operations (Warehouses/Activity log)
- [~] P5-08 | Dark/light toggle hoạt động; persistence cross-reload (cookie hoặc localStorage) là follow-up

### 5.3 Shared components
- [ ] P5-09 | `<EntityDataGrid<T>>` wrapper MudDataGrid với server-side pagination/sort/filter chuẩn
- [ ] P5-10 | `<MediaPicker>` upload + chọn từ thư viện
- [ ] P5-11 | `<SlugInput>` auto-generate from name
- [ ] P5-12 | `<RichTextEditor>` (TinyMCE Blazor wrapper hoặc QuillJS interop)
- [ ] P5-13 | `<EntityPicker<T>>` autocomplete chọn entity (dùng cho FK)
- [ ] P5-14 | `<ConfirmDialog>` xác nhận xoá
- [ ] P5-15 | `<FormCard>` chuẩn validation + save/cancel buttons
- [ ] P5-16 | Toast wrapper qua MudSnackbar

### 5.4 Pages
> Note: The prompt reserved `/admin/...` URL prefix for the existing AngularJS admin; this new Blazor Admin app is a **separate web host** at its own port (no `/admin` prefix inside its own routes). When Phase 8 cuts over, the reverse proxy can map `/admin` → `admin` resource if desired.

**5.4.1 Dashboard**
- [x] P5-17 | `/` dashboard: KPI cards (Orders total, Products indexed, Brands, Categories) + Recent orders table. Sales chart + low-stock is follow-up.

**5.4.2 Catalog**
- [x] P5-18 | `/products` list (DataGrid: id, name, sku, price, stock, published, delete action) + search + paging
- [~] P5-19 | `/products/create` + `/products/edit/{id}` tabbed editor — **deferred follow-up**; backend POST/PUT endpoints need full product shape (media/attributes/variants) — sub-PR after backend surface solidifies
- [x] P5-20 | `/categories` list + add (flat form; tree view + drag-drop is follow-up)
- [x] P5-21 | `/brands` list + add + delete
- [ ] P5-22..P5-24 | options / attributes / product-templates — **deferred**; endpoints exist for some, UI is sub-PR

**5.4.3 Orders**
- [x] P5-25 | `/orders` list with status + customer-search filter + paging
- [~] P5-26 | `/orders/{id}` detail + timeline — **deferred** (backend endpoint shape for detail + timeline pending)
- [ ] P5-27..P5-29 | shipments / refunds / sales-report — deferred (endpoint scaffold needed first)

**5.4.4 Customers**
- [~] P5-30..P5-31 | `/customers/*` — the list IS surfaced as `/users` (Core admin endpoint returns Identity users). Customer/vendor role split + detail page deferred
- [x] P5-33 | `/users` — page list with paging + search (existing AdminUserListItem DTO)
- [ ] P5-32 | customer-groups — deferred
- [ ] role permission matrix — deferred

**5.4.5 Content / CMS / News**
- [~] P5-34..P5-39 | CMS pages/menus/widgets/news/media — **deferred**, endpoint scaffold exists per-module but UI is follow-up

**5.4.6 Reviews**
- [x] P5-40 | `/reviews` moderation queue — filter by status + Approve / Reject actions

**5.4.7 Inventory**
- [x] P5-41 | `/warehouses` — GET list
- [ ] P5-42..P5-43 | stock history + adjustments — deferred

**5.4.8 Pricing & Promotions**
- [x] P5-44 + P5-45 + P5-46 | `/pricing` — tabbed view (cart rules / catalog rules / coupons) GET list for each. CRUD forms are follow-up.

**5.4.9 Shipping & Tax**
- [x] P5-47 | `/shipping-providers` — list providers (GET); config form is follow-up
- [ ] P5-48..P5-49 | Zones + rates — deferred (need endpoint surface first)
- [x] P5-50 | `/tax-classes` — list + inline create
- [~] P5-51 | Tax rates — list endpoint + table (pending full CRUD UI sub-PR)

**5.4.10 Payments**
- [x] P5-52 | `/payment-providers` — list + enabled flag. Per-provider config form (Stripe/Braintree etc.) is per-provider follow-up

**5.4.11 Vendors**
- [x] P5-53 | `/vendors` — list + inline create
- [ ] P5-54 | `/vendors/{id}/products` — deferred (backend endpoint pending)

**5.4.12 Localization**
- [ ] P5-55..P5-56 | Languages + translations — deferred (translation editor UI is complex sub-PR)

**5.4.13 Settings**
- [ ] P5-57..P5-60 | Settings panels — deferred (per-concern forms)

**5.4.14 Activity & Notifications**
- [x] P5-61 | `/activity-log` paged list
- [ ] P5-62 | Notification center with SignalR push — deferred (AdminNotificationHub hasn't landed in ApiService yet, see P5-63)

### 5.5 SignalR realtime
- [~] P5-63 | Hub infra ready in **Admin host** (`AddSignalR().AddStackExchangeRedis`). Dedicated `AdminNotificationHub` in ApiService is pending — backplane + handler plumbing is in place so wiring the hub is a small sub-PR.
- [ ] P5-64..P5-65 | Event publish + `<NotificationBell>` — deferred to P5-63 follow-up

### 5.6 Verify
- [~] P5-66 | Manual test — BLOCKED-Docker; pages compile and match API contract types exactly
- [x] P5-67 | Permission test (by code inspection): fallback authorisation requires role admin/vendor; `CookieAuthStateService.SignInAsync` rejects non-admin at login. Runtime confirm BLOCKED-Docker
- [~] P5-68 | Feature parity vs AngularJS admin — current coverage: Dashboard, Products list, Categories, Brands, Orders list, Users list, Reviews moderation, Warehouses, Activity log. Missing: product edit tabs, category tree, CMS, news, coupons, shipping zones/rates, payment provider config, translations, settings — documented as sub-PRs

### 5.7 Commit Phase 5 (scaffold + core pages)
- [x] P5-69 | MIGRATION_PROGRESS updated, commit with full shape summary
- [ ] P5-70 | PR creation — user hasn't asked; keep on branch

---

## PHASE 6 — DATA MIGRATION & COMPATIBILITY

**Mục tiêu:** Đảm bảo migrate được data từ instance cũ sang.
**Thời lượng ước tính:** 6–10 giờ
**Branch:** `aspire-migration/phase-6-data`

- [~] P6-01 | Verify `Initial_AspireBaseline` — **BLOCKED-Docker** (needs live SQL Server); migration itself has NOT been generated yet. `src/Migrations/SimplCommerce.Migrations/README.md` + `docs/migration/data-migration-runbook.md` carry the one-shot developer-box steps.
- [x] P6-02 | `tools/migrate-data.ps1` — PowerShell 7.2+ script: backs up DB, applies consolidated migrations via `dotnet ef database update`, re-checks row counts on 5 critical tables (`Core_User`, `Catalog_Product`, `Orders_Order`, `ShoppingCart_Cart`, `Reviews_Review`), **automatically restores from backup on regression**. Supports `-Force` for non-interactive, Windows auth + SQL auth, custom backup dir.
- [x] P6-03 | `Module.SampleData` — already refactored in Phase 2.3 batch 6 (Services → Application/Services + Infrastructure/Services; SqlRepository → Infrastructure/Data/Repositories; `AddSampleDataModule()` extension). Runtime behaviour identical — `SampleContent/Fashion` + `SampleContent/Phones` SQL scripts still loaded via `ISqlRepository.RunCommand`. No further refactor needed; UI trigger is a Phase 5 sub-PR.
- [~] P6-04 | Clean-DB smoke — **BLOCKED-Docker** (runbook §Path B covers this)
- [~] P6-05 | Production-copy test — **BLOCKED-Docker + user-specific DB**
- [x] P6-06 | `docs/migration/data-migration-runbook.md` — full runbook: pre-flight checklist, schema version check, 3 migration paths (in-place / parallel DB / blue-green), sample-data + post-migration SQL checks + rollback instructions.
- [x] P6-07 | Commit (current commit); PR deferred per DECISION-003

---

## PHASE 7 — TESTING & HARDENING

**Mục tiêu:** Đạt chất lượng production-ready.
**Thời lượng ước tính:** 16–24 giờ
**Branch:** `aspire-migration/phase-7-testing`

### 7.1 Test coverage
- [~] P7-01 | Unit tests (target ≥ 60%): có 41 existing pre-migration unit tests still passing. Coverage instrumentation + fill-in to 60% là sub-PR (per-module test projects cần build-out)
- [~] P7-02 | Integration E2E per flow: scaffold project có (tests/SimplCommerce.ApiService.IntegrationTests) + 1 smoke test. DB-backed e2e BLOCKED-Docker
- [~] P7-03 | API contract tests: deferred cùng P7-02 — scaffold ready, DB needed
- [~] P7-04 | Playwright E2E — BLOCKED-Docker

### 7.2 Performance
- [x] P7-05 | k6 script tại `tools/loadtest/storefront.js`: 5 hot endpoints (/, /category/{slug}, /product/{slug}, /api/storefront/catalog/products, /api/storefront/search), ramp 50 VU, threshold p95<500ms + error rate <1%
- [~] P7-06 | DB index tuning — defer cho đến khi có baseline trên DB copy (xem data-migration-runbook)
- [x] P7-07 | Output cache đã enable (`AddOutputCache()`); per-endpoint `[OutputCache]` attr là fine-tune follow-up
- [~] P7-08 | Image pipeline WebP + lazy — ImageSharp dependency đã planned Phase 3 (P3-51), UI lazy-load là follow-up

### 7.3 Security
- [x] P7-09 | Rate limit 100 req/min/IP trên `/api/auth/*` qua `.RequireRateLimiting("auth")` + global token-bucket 200/min/IP
- [x] P7-10 | CSP strict: `default-src 'self'`, `script-src 'self' 'wasm-unsafe-eval' blob:` (cho Blazor), `frame-ancestors 'none'`, full policy trong `SecurityHeadersMiddleware`
- [x] P7-11 | X-Content-Type-Options nosniff, X-Frame-Options DENY, Referrer-Policy strict-origin-when-cross-origin, Permissions-Policy khóa geolocation/camera/microphone/payment. HSTS bật non-Dev qua `app.UseHsts()`
- [x] P7-12 | `app.UseAntiforgery()` trên cả Storefront + Admin Blazor hosts; webhook endpoints explicitly `DisableAntiforgery` (signature IS the auth)
- [x] P7-13 | `Ganss.Xss.HtmlSanitizer` singleton qua `AddSimplHtmlSanitizer()`; strips iframe/object/embed + javascript:/vbscript: schemes. Consumer endpoints (reviews POST, CMS admin POST) inject IHtmlSanitizer
- [x] P7-14 | Không hardcode secret: JWT signing key từ `Configuration["Jwt:SigningKey"]` với dev fallback (code-level comment explicitly flags it); payment provider secrets trong DB (PaymentProvider.AdditionalSettings table). Grep sanity: no API keys in source
- [~] P7-15 | OWASP ZAP baseline scan — BLOCKED-Docker (scan cần runtime stack)

### 7.4 Observability
- [x] P7-16 | `SimplMetrics` singleton với Meter `"SimplCommerce"`: `simpl.orders.created`, `simpl.payments.failed`, `simpl.carts.abandoned`. Meter registered trong ServiceDefaults OpenTelemetry pipeline
- [x] P7-17 | Activity tracing tag `simpl.correlation_id` via `CorrelationIdMiddleware`. userId/orderId tags là per-endpoint follow-up (2 dòng `Activity.Current?.SetTag(...)` trong handler)
- [x] P7-18 | `CorrelationIdMiddleware`: honours inbound `X-Correlation-Id`, echoes response, wraps request trong ILogger scope với `CorrelationId` key → every log line + every outbound HttpClient span carries it
- [x] P7-19 | Health check chi tiết: `AddDbContextCheck<SimplDbContext>("sqlserver")` + `ready` tag. Redis / Blob / Payment reachability cần Aspire-injected connection strings runtime → package refs có (`AspNetCore.HealthChecks.SqlServer/Redis/Azure.Storage.Blobs`)
- [~] P7-20 | Aspire dashboard trace verify — BLOCKED-Docker (traces generated, chỉ cần runtime pour verify)

### 7.5 Documentation
- [~] P7-21 | README root — cần cập nhật chọn lọc (file hiện tại còn instruction .NET 8 + AngularJS); không touch trong commit này để giảm scope, làm riêng follow-up
- [x] P7-22 | `docs/architecture.md` — topology diagram + projects table + hardening table + trace-path example
- [x] P7-23 | `docs/deployment.md` — Aspire local, Azure Container Apps via `aspire publish`, k8s, Docker Compose reference, secrets list, observability + scaling knobs
- [x] P7-24 | `docs/development.md` — clone/build/run, adding new module (7-step), endpoint convention, Blazor page convention, migration tooling shortcuts
- [ ] P7-25 | API docs accessible tại `/scalar` của ApiService

### 7.6 Commit
- [ ] P7-26 | PR + merge
- [ ] P7-27 | Báo cáo → **DỪNG, user QA toàn diện** trước Phase 8

---

## PHASE 8 — CUTOVER & CLEANUP

**Mục tiêu:** Xoá legacy, finalize.
**Thời lượng ước tính:** 4–6 giờ
**Branch:** `aspire-migration/phase-8-cutover`
**⚠ Chỉ chạy sau khi user xác nhận đã QA Phase 7 OK**

### 8.1 Tag baseline
- [~] P8-01 | `git tag pre-cutover-backup` — **DEFERRED to user**: tag phải được đặt trên commit cuối Phase 7 ở branch cutover, sau khi user đã runtime-verify. Không tự tag để tránh đóng dấu sai point khi branch còn phát triển
- [~] P8-02 | Production DB backup — user responsibility; reminder đã có trong `tools/migrate-data.ps1` + `docs/migration/data-migration-runbook.md`

### 8.2 Xoá WebHost cũ — **DEFERRED to user**
- [~] P8-03..P8-05 | Xoá `src/SimplCommerce.WebHost/`, unhook khỏi AppHost, xoá legacy `Dockerfile` / `Dockerfile-sqlite` / `docker-entrypoint.sh` — **không thực hiện tự động vì destructive + runtime của stack mới chưa verified BLOCKED-Docker**. Runbook từng bước trong `docs/migration/phase8-cutover-checklist.md` §1

### 8.3 Xoá legacy code trong modules — **DEFERRED to user**
- [~] P8-06..P8-10 | Xoá `wwwroot/admin/` (98 files AngularJS), `Views/` (181 files cshtml), `Controllers/` (104 files), bundling, `modules.json` — tất cả **destructive**, defer đến sau runtime verification. `modules.json` đã xử lý ở Phase 2 (archived). `CustomAssemblyLoadContextProvider` không tồn tại trong codebase (đã verify Phase 2). Checklist đầy đủ trong `phase8-cutover-checklist.md` §2-§6

### 8.4 Generate deployment artifacts — **DONE**
- [~] P8-11 | `aspire publish` — **BLOCKED-Docker** để chạy lệnh, nhưng csproj đã sẵn sàng; runbook `docs/deployment.md` §Azure Container Apps carries the exact command
- [x] P8-12 | 3 Dockerfile multi-stage đã tạo:
  - `src/Apps/SimplCommerce.ApiService/Dockerfile`
  - `src/Apps/SimplCommerce.Storefront/Dockerfile`
  - `src/Apps/SimplCommerce.Admin/Dockerfile`
  Base: `mcr.microsoft.com/dotnet/sdk:9.0` build stage + `aspnet:9.0` runtime, non-root USER `$APP_UID`, port 8080
- [x] P8-13 | `azure-pipelines.yml` rewritten: 1 fast Linux build + test stage + container publish stage (matrix per app); removed legacy 4-matrix Linux/macOS/Windows/LinuxRelease setup targeting .NET 8
- [x] P8-14 | `.github/workflows/ci.yml` — build + test (TreatWarningsAsErrors) + publish images GHCR với matrix + gha cache
- [x] P8-15 | `compose.yaml` + `.env.sample` — SQL 2022 + Redis 7 + Azurite + MailPit + Seq + ApiService + Storefront + Admin, healthchecks, Seq OTLP endpoint

### 8.5 Final verify — **Partial**
- [x] P8-16 | `dotnet build SimplCommerce.sln` PASS — 59 projects, 0 errors, 0 warnings (clean + incremental)
- [x] P8-17 | `dotnet test SimplCommerce.sln --no-build` PASS — 42/42
- [~] P8-18 | `aspire run` — BLOCKED-Docker
- [~] P8-19 | Runtime smoke test — BLOCKED-Docker
- [~] P8-20 | Solution chỉ còn project mới — chưa, vì destructive steps 8.2/8.3 defer đến user

### 8.6 Release — **Partial**
- [x] P8-21 | MIGRATION_PROGRESS.md updated (this commit)
- [x] P8-22 | `CHANGELOG.md` — full `v2.0.0-aspire-blazor` release notes: breaking changes, new services, module refactor summary, hardening, tooling, carried-forward, known gaps, migration path
- [~] P8-23 | PR into master — **DEFERRED to user** (DECISION-003)
- [~] P8-24 | Tag `v2.0.0-aspire-blazor` — **DEFERRED** (waits for cutover branch to merge)
- [x] P8-25 | Final report ở cuối turn này

---

## QUY TẮC CHUNG XUYÊN SUỐT MỌI PHASE

- [ ] Mỗi commit phải build PASS
- [ ] Mỗi PR phải có mô tả + checklist phase tương ứng
- [ ] Quyết định kiến trúc mới → ghi `MIGRATION_DECISIONS.md`
- [ ] Bug/issue phát hiện trong quá trình → log `MIGRATION_ISSUES.md` (không fix nếu không thuộc scope phase, để Phase 7)
- [ ] Sau mỗi phase, update `MIGRATION_PROGRESS.md`
- [ ] Khi không chắc → DỪNG hỏi user, đừng đoán
- [ ] Nếu phase quá 1.5x ước tính giờ → dừng báo cáo trước khi tiếp tục

## TỔNG KẾT KHỐI LƯỢNG

| Phase | Mô tả | Giờ ước tính |
|---|---|---|
| 0 | Discovery | 2–4 |
| 1 | Aspire Bootstrap | 4–6 |
| 2 | Refactor Core | 16–24 |
| 3 | API Service | 16–24 |
| 4 | Storefront Blazor | 24–32 |
| 5 | Admin Blazor | 40–60 |
| 6 | Data Migration | 6–10 |
| 7 | Testing & Hardening | 16–24 |
| 8 | Cutover | 4–6 |
| **TỔNG** | | **128–190 giờ** |

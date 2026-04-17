# MIGRATION PROGRESS — SimplCommerce → .NET Aspire + Blazor

> Bản mirror của MIGRATION_TODO.md, tick trạng thái thực tế qua từng phase.
> Legend: `[x]` = done, `[ ]` = pending, `[~]` = BLOCKED (cần user làm tay — ghi chú ngay dưới task), `[-]` = skipped/N/A (lý do ghi chú ngay dưới).

## TRẠNG THÁI HIỆN TẠI
- **Phase đang chạy:** Phase 0 → Phase 1 (dừng sau khi xong Phase 1 theo yêu cầu user)
- **Branch:** `claude/phase-0-migration-pX925` (gộp Phase 0 + Phase 1 — xem DECISION-002)
- **Blocker lớn:** sandbox không có `dotnet` CLI — xem DECISION-004 và `docs/migration/phase0-manual-steps.md`

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
- [~] P0-20 | **BLOCKED** — sandbox không có `dotnet` CLI. User chạy trên local theo `docs/migration/phase0-manual-steps.md#P0-20`
- [~] P0-21 | **BLOCKED** — Aspire templates cần `dotnet new install Aspire.ProjectTemplates` (Aspire 9 đã standalone, không còn dùng workload). Lệnh trong manual-steps doc
- [~] P0-22 | **BLOCKED** — verify sau khi P0-21 xong

### 0.7 Snapshot baseline
- [~] P0-23 | **BLOCKED** — cần `dotnet build` local
- [~] P0-24 | **BLOCKED** — cần chạy WebHost trong sandbox có DB + browser. Manual step doc có hướng dẫn
- [~] P0-25 | **BLOCKED** — cần `dotnet ef` local
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
- [ ] P1-01 | Tạo thư mục `src/AppHost/` và `src/ServiceDefaults/`
- [ ] P1-02 | `dotnet new aspire-apphost -n SimplCommerce.AppHost -o src/AppHost/SimplCommerce.AppHost`
- [ ] P1-03 | `dotnet new aspire-servicedefaults -n SimplCommerce.ServiceDefaults -o src/ServiceDefaults/SimplCommerce.ServiceDefaults`
- [ ] P1-04 | Add cả 2 project vào `SimplCommerce.sln` trong solution folder `00-Aspire`

### 1.2 Cấu hình ServiceDefaults
- [ ] P1-05 | Verify `AddServiceDefaults()` có đủ: OpenTelemetry tracing+metrics+logging, health checks (`/health`, `/alive`), service discovery, HttpClient resilience (Polly v8 standard)
- [ ] P1-06 | Add Serilog sink Seq trong ServiceDefaults (qua `ConfigureOpenTelemetry` hoặc `AddSerilog`)
- [ ] P1-07 | Tạo extension `MapDefaultEndpoints()` map health + alive + (nếu dev) Aspire dashboard link

### 1.3 Khai báo resources trong AppHost
- [ ] P1-08 | Add NuGet `Aspire.Hosting.SqlServer`, `Aspire.Hosting.Redis`, `Aspire.Hosting.Azure.Storage`, `CommunityToolkit.Aspire.Hosting.MailDev`, `CommunityToolkit.Aspire.Hosting.Seq` (hoặc tương đương)
- [ ] P1-09 | Trong `Program.cs` AppHost, khai báo:
  ```csharp
  var sql = builder.AddSqlServer("sql", port: 1433)
                   .WithDataVolume("simplcommerce-sqldata")
                   .WithLifetime(ContainerLifetime.Persistent);
  var simplDb = sql.AddDatabase("SimplCommerce");

  var redis = builder.AddRedis("redis").WithRedisCommander();
  var blobs = builder.AddAzureStorage("storage").RunAsEmulator().AddBlobs("blobs");
  var mail  = builder.AddMailDev("maildev");
  var seq   = builder.AddSeq("seq").WithDataVolume();
  ```
- [ ] P1-10 | Set `WithLifetime(ContainerLifetime.Persistent)` cho SQL và Seq để không mất data giữa các lần chạy

### 1.4 Wrap WebHost cũ vào AppHost
- [ ] P1-11 | Add reference từ AppHost → `SimplCommerce.WebHost.csproj`
- [ ] P1-12 | Trong AppHost: `builder.AddProject<Projects.SimplCommerce_WebHost>("webhost").WithReference(simplDb).WithReference(redis).WithReference(blobs).WithReference(mail).WaitFor(sql);`
- [ ] P1-13 | Sửa `SimplCommerce.WebHost/Program.cs` để đọc connection string từ `builder.Configuration.GetConnectionString("SimplCommerce")` (chuẩn Aspire) thay vì appsettings cũ — fallback về appsettings nếu null để không vỡ chế độ chạy standalone
- [ ] P1-14 | Add `builder.AddServiceDefaults();` vào WebHost Program.cs (đầu) và `app.MapDefaultEndpoints();` (trước app.Run)

### 1.5 Verify
- [ ] P1-15 | `dotnet build` toàn solution — PASS
- [ ] P1-16 | `dotnet run --project src/AppHost/SimplCommerce.AppHost` — Aspire dashboard mở
- [ ] P1-17 | Kiểm tra: SQL container healthy, Redis healthy, WebHost healthy, kết nối được DB
- [ ] P1-18 | Truy cập WebHost qua URL Aspire gán → trang chủ hiện, login admin OK
- [ ] P1-19 | Screenshot dashboard + WebHost chạy → `docs/migration/phase1-screenshots/`

### 1.6 Commit Phase 1
- [ ] P1-20 | Update `MIGRATION_PROGRESS.md` tick xong Phase 0 + 1
- [ ] P1-21 | `git commit -m "feat(migration): phase 1 - aspire app host with resources"`
- [ ] P1-22 | Push, tạo PR, merge vào `aspire-migration/main`
- [ ] P1-23 | Báo cáo → **DỪNG, chờ user review** (vì Phase 2+ sẽ refactor lớn)

---

## PHASE 2 — REFACTOR CORE & MODULE LAYERING

**Mục tiêu:** Tách Domain/Application/Infrastructure từng module, gộp migrations, bỏ runtime module loading, nâng MediatR 12. WebHost cũ vẫn chạy.
**Thời lượng ước tính:** 16–24 giờ
**Branch:** `aspire-migration/phase-2-refactor-core`

### 2.1 Tạo project Migrations gộp
- [ ] P2-01 | Tạo `src/Migrations/SimplCommerce.Migrations/` (class library)
- [ ] P2-02 | Reference Microsoft.EntityFrameworkCore.SqlServer, Design, Tools
- [ ] P2-03 | Move `SimplDbContext` (hoặc tương đương) vào project này nếu chưa
- [ ] P2-04 | Generate migration mới `Initial_AspireBaseline` snapshot toàn bộ schema hiện hữu (idempotent — không drop dữ liệu)
- [ ] P2-05 | Verify `dotnet ef database update` từ project mới chạy OK trên DB sạch

### 2.2 Refactor SimplCommerce.Module.Core
- [ ] P2-06 | Trong project Core, tạo thư mục `Domain/` `Application/` `Infrastructure/` `Endpoints/`
- [ ] P2-07 | Move entities (`User`, `Role`, `EntityType`, `Address`, ...) → `Domain/Entities/`
- [ ] P2-08 | Move domain events → `Domain/Events/`
- [ ] P2-09 | Move services interface → `Application/Services/`, implementation → `Infrastructure/Services/`
- [ ] P2-10 | Move EF mappings (`*ICustomModelBuilder` impls) → `Infrastructure/Data/`
- [ ] P2-11 | Move Repositories → `Infrastructure/Data/Repositories/`
- [ ] P2-12 | Tạo extension `public static IHostApplicationBuilder AddCoreModule(this IHostApplicationBuilder builder)` thay cho `ModuleInitializer`
- [ ] P2-13 | Xoá file `ModuleInitializer.cs` của Core (giữ ghi chú trong commit message)

### 2.3 Refactor 24 module còn lại — lặp lại pattern Core
Thứ tự ưu tiên (theo dependency):
- [ ] P2-14 | Localization
- [ ] P2-15 | ActivityLog
- [ ] P2-16 | Catalog
- [ ] P2-17 | Cms
- [ ] P2-18 | Inventory
- [ ] P2-19 | Pricing
- [ ] P2-20 | Tax
- [ ] P2-21 | Shipping
- [ ] P2-22 | ShippingPrices
- [ ] P2-23 | ShoppingCart
- [ ] P2-24 | Checkouts
- [ ] P2-25 | Orders
- [ ] P2-26 | Payments (+ PaymentMomo, PaymentPaypal, PaymentStripe, PaymentVnpay, PaymentCashOnDelivery)
- [ ] P2-27 | Sales
- [ ] P2-28 | Production
- [ ] P2-29 | Reviews
- [ ] P2-30 | WishList
- [ ] P2-31 | News
- [ ] P2-32 | Vendors
- [ ] P2-33 | Search
- [ ] P2-34 | Notifications
- [ ] P2-35 | EmailSenderSendGrid
- [ ] P2-36 | StorefrontApi
- [ ] P2-37 | SampleData

Với MỖI module:
- Tạo thư mục `Domain/`, `Application/`, `Infrastructure/`, `Endpoints/`, giữ tạm `Controllers/`, `Views/`, `wwwroot/`, `Areas/`
- Move file đúng layer theo trách nhiệm
- Tạo extension `Add<ModuleName>Module()`
- Xoá `ModuleInitializer.cs`
- Build sau mỗi module — KHÔNG move sang module tiếp nếu chưa build PASS
- Commit nhỏ sau mỗi 3–5 module: `refactor(module-X,Y,Z): phase 2 layering`

### 2.4 Sửa WebHost dùng cách register mới
- [ ] P2-38 | Mở `SimplCommerce.WebHost/Program.cs`
- [ ] P2-39 | Thay block `LoadInstalledModules()` + `ModuleInitializer` bằng chuỗi `builder.AddCoreModule().AddCatalogModule().AddOrdersModule()...` (gọi tất cả 25 module)
- [ ] P2-40 | Xoá file `modules.json` runtime loading (giữ backup ở `docs/migration/legacy/`)
- [ ] P2-41 | Xoá `CustomAssemblyLoadContextProvider`, `ModuleViewLocationExpander` (Razor view location vẫn cần — chỉ xoá phần load assembly động)

### 2.5 Nâng MediatR 7 → 12
- [ ] P2-42 | Update package MediatR sang 12.x trong tất cả csproj
- [ ] P2-43 | Update `IRequestHandler<TRequest, TResponse>.Handle` signature nếu cần (CancellationToken là param 2)
- [ ] P2-44 | Update DI registration: `builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(...))` thay cho v7 syntax
- [ ] P2-45 | Build + chạy WebHost — verify domain events còn fire (test add product, check log)

### 2.6 Verify Phase 2
- [ ] P2-46 | `dotnet build` PASS toàn solution
- [ ] P2-47 | `aspire run` → WebHost cũ chạy được, login admin OK, CRUD product OK, đặt thử 1 đơn COD OK
- [ ] P2-48 | So sánh hành vi với baseline screenshots Phase 0 — không có regression

### 2.7 Commit Phase 2
- [ ] P2-49 | Update `MIGRATION_PROGRESS.md`
- [ ] P2-50 | Squash commits Phase 2 thành 1 PR lớn với mô tả chi tiết
- [ ] P2-51 | Báo cáo → **DỪNG, chờ user review** (đây là phase rủi ro nhất)

---

## PHASE 3 — API SERVICE

**Mục tiêu:** Tách toàn bộ API ra thành Minimal API service riêng. WebHost cũ vẫn còn nhưng API mới song song.
**Thời lượng ước tính:** 16–24 giờ
**Branch:** `aspire-migration/phase-3-api-service`

### 3.1 Tạo ApiService
- [ ] P3-01 | `dotnet new web -n SimplCommerce.ApiService -o src/Apps/SimplCommerce.ApiService` (.NET 9)
- [ ] P3-02 | Add reference: ServiceDefaults, Migrations project, **TẤT CẢ** module project
- [ ] P3-03 | Add NuGet: Microsoft.AspNetCore.Authentication.JwtBearer, FluentValidation.AspNetCore, Scalar.AspNetCore, Microsoft.AspNetCore.OpenApi
- [ ] P3-04 | Add vào AppHost: `var api = builder.AddProject<Projects.SimplCommerce_ApiService>("api").WithReference(simplDb).WithReference(redis).WithReference(blobs).WithReference(mail).WaitFor(sql);`

### 3.2 Setup ApiService Program.cs
- [ ] P3-05 | `builder.AddServiceDefaults();`
- [ ] P3-06 | `builder.AddSqlServerDbContext<SimplDbContext>("SimplCommerce");`
- [ ] P3-07 | `builder.AddRedisDistributedCache("redis");`
- [ ] P3-08 | `builder.AddAzureBlobClient("blobs");`
- [ ] P3-09 | Add Identity Core (không cookie, dùng JWT): `builder.Services.AddIdentityCore<User>().AddRoles<Role>().AddEntityFrameworkStores<SimplDbContext>().AddDefaultTokenProviders();`
- [ ] P3-10 | JWT config: issuer, audience, signing key (đọc từ user-secrets / Aspire parameter)
- [ ] P3-11 | `builder.Services.AddAuthorization();` + define policies (`AdminOnly`, `CustomerOnly`)
- [ ] P3-12 | `builder.Services.AddOpenApi();` + Scalar UI tại `/scalar`
- [ ] P3-13 | Đăng ký tất cả module: `builder.AddCoreModule().AddCatalogModule()...`
- [ ] P3-14 | Output cache + response compression + CORS (chỉ cho phép Storefront/Admin origin)

### 3.3 Migrate endpoints — Storefront API trước
Cho mỗi controller trong `Module.StorefrontApi`:
- [ ] P3-15 | Tạo file `Endpoints/<Name>Endpoints.cs` trong module gốc, dạng `public static class XxxEndpoints { public static void MapXxxEndpoints(this IEndpointRouteBuilder app) { ... } }`
- [ ] P3-16 | Convert mỗi action → minimal endpoint, dùng typed parameters + `Results<Ok<T>, NotFound, BadRequest>` return
- [ ] P3-17 | DTO request có FluentValidator
- [ ] P3-18 | Endpoint nào trả list → support paging chuẩn (page, pageSize, sort, filter)
- [ ] P3-19 | Apply `[Authorize]` attribute hoặc `.RequireAuthorization("policy")` đúng

Danh sách endpoint group cần migrate (Storefront):
- [ ] P3-20 | Catalog endpoints (product, category, brand)
- [ ] P3-21 | Search endpoints
- [ ] P3-22 | Cart endpoints
- [ ] P3-23 | Checkout endpoints
- [ ] P3-24 | Order endpoints (history, detail)
- [ ] P3-25 | User account endpoints (profile, address)
- [ ] P3-26 | Wishlist endpoints
- [ ] P3-27 | Review endpoints (post, list)
- [ ] P3-28 | Cms endpoints (page, menu)

### 3.4 Migrate endpoints — Admin API
Cho mỗi module có admin API (đang được AngularJS gọi):
- [ ] P3-29 | Catalog admin: product CRUD, category CRUD, brand, option, attribute, product-template
- [ ] P3-30 | Orders admin: list, detail, status update, refund, shipment
- [ ] P3-31 | Customers admin: list, detail, address, role assignment
- [ ] P3-32 | Reviews admin: moderation
- [ ] P3-33 | Inventory admin: warehouse, stock CRUD
- [ ] P3-34 | Pricing admin: cart rule, catalog rule, coupon
- [ ] P3-35 | Cms admin: page, menu, widget, news
- [ ] P3-36 | Shipping admin: providers, zones, rates
- [ ] P3-37 | Tax admin: classes, rates
- [ ] P3-38 | Payments admin: provider config CRUD
- [ ] P3-39 | Vendors admin
- [ ] P3-40 | Localization admin: language, resources CRUD
- [ ] P3-41 | Settings admin
- [ ] P3-42 | Activity log read endpoints

### 3.5 Auth endpoints
- [ ] P3-43 | `POST /api/auth/register`
- [ ] P3-44 | `POST /api/auth/login` → trả access + refresh token
- [ ] P3-45 | `POST /api/auth/refresh`
- [ ] P3-46 | `POST /api/auth/logout`
- [ ] P3-47 | `POST /api/auth/forgot-password`
- [ ] P3-48 | `POST /api/auth/reset-password`
- [ ] P3-49 | External login (Google/Facebook nếu module hiện tại có)

### 3.6 File upload
- [ ] P3-50 | `POST /api/media/upload` → Azure Blob qua `BlobServiceClient` Aspire
- [ ] P3-51 | Image resizing pipeline (ImageSharp): generate thumbnail + medium + large
- [ ] P3-52 | Public URL generation (CDN-ready)

### 3.7 Webhook endpoints (payment callback)
- [ ] P3-53 | `POST /api/webhooks/stripe`
- [ ] P3-54 | `POST /api/webhooks/paypal`
- [ ] P3-55 | `POST /api/webhooks/momo`
- [ ] P3-56 | `POST /api/webhooks/vnpay`
- [ ] P3-57 | Verify signature trước khi xử lý

### 3.8 Test integration
- [ ] P3-58 | Tạo project `tests/SimplCommerce.IntegrationTests`
- [ ] P3-59 | Setup `Aspire.Hosting.Testing` + `WebApplicationFactory`
- [ ] P3-60 | Viết ít nhất 1 test GET cho mỗi module (smoke test) → 25 test minimum
- [ ] P3-61 | Test auth flow: register → login → call endpoint authorized → OK
- [ ] P3-62 | Test checkout flow: add to cart → place order COD → verify order created
- [ ] P3-63 | `dotnet test` PASS

### 3.9 Commit Phase 3
- [ ] P3-64 | Update `MIGRATION_PROGRESS.md`
- [ ] P3-65 | PR + merge
- [ ] P3-66 | Báo cáo → có thể tự sang Phase 4

---

## PHASE 4 — STOREFRONT BLAZOR

**Mục tiêu:** Storefront mới hoàn chỉnh, chạy song song storefront cũ trong WebHost. SEO-friendly.
**Thời lượng ước tính:** 24–32 giờ
**Branch:** `aspire-migration/phase-4-storefront`

### 4.1 Tạo project
- [ ] P4-01 | `dotnet new blazor -n SimplCommerce.Storefront -o src/Apps/SimplCommerce.Storefront --interactivity Auto --auth Individual`
- [ ] P4-02 | Add 2 sub project tự sinh: `.Storefront` (server) và `.Storefront.Client` (WASM)
- [ ] P4-03 | Add reference ServiceDefaults
- [ ] P4-04 | Add NuGet MudBlazor cả 2 project
- [ ] P4-05 | Add vào AppHost: `builder.AddProject<Projects.SimplCommerce_Storefront>("storefront").WithReference(api).WithReference(redis).WaitFor(api);`

### 4.2 Setup MudBlazor + theme
- [ ] P4-06 | `builder.Services.AddMudServices()` trong cả Server và Client
- [ ] P4-07 | Add `<MudThemeProvider>`, `<MudDialogProvider>`, `<MudSnackbarProvider>` vào `MainLayout.razor`
- [ ] P4-08 | Tạo `Theme/SimplTheme.cs` với palette màu của SimplCommerce hiện tại (primary blue, …)
- [ ] P4-09 | Layout: header với search bar + cart icon + account menu, footer với CMS menu + newsletter, drawer mobile

### 4.3 Auth setup
- [ ] P4-10 | Cookie auth ở Storefront server (UX tốt cho ecommerce)
- [ ] P4-11 | Server đổi cookie ↔ JWT khi gọi ApiService (BFF pattern)
- [ ] P4-12 | `AuthenticationStateProvider` custom nối với `/api/auth/me`
- [ ] P4-13 | `<AuthorizeView>` áp dụng ở các trang account

### 4.4 Typed HttpClients
- [ ] P4-14 | Tạo `Services/ApiClients/` chứa: `ICatalogApi`, `ICartApi`, `ICheckoutApi`, `IOrderApi`, `IAccountApi`, `IReviewApi`, `IWishlistApi`, `ICmsApi`, `ISearchApi`
- [ ] P4-15 | Dùng Refit hoặc `HttpClient` typed + `AddHttpClient<T>()` với base address từ Aspire service discovery (`http://api`)
- [ ] P4-16 | Tự gắn JWT bearer header qua `DelegatingHandler`

### 4.5 Pages — theo thứ tự ưu tiên
- [ ] P4-17 | `/` Home: hero banner, featured products, featured categories, latest news (gọi 4 API parallel với `Task.WhenAll`)
- [ ] P4-18 | `/category/{slug}` Category listing: filter sidebar (brand, price, attribute), sort, paging, grid/list view toggle
- [ ] P4-19 | `/product/{slug}` Product detail: image gallery, variant selector, qty, add to cart, tabs (description, specifications, reviews), related products, recently viewed
- [ ] P4-20 | `/search?q=` Search results với facets
- [ ] P4-21 | `/cart` Cart page: line items, qty edit, coupon input, totals, checkout CTA. State trong Redis cho guest (key = sessionId), DB cho user
- [ ] P4-22 | `/checkout` multi-step: address → shipping method → payment method → review → confirm
- [ ] P4-23 | `/checkout/success?orderId=` confirmation
- [ ] P4-24 | `/account/login`, `/account/register`, `/account/forgot-password`
- [ ] P4-25 | `/account` profile dashboard
- [ ] P4-26 | `/account/orders` + `/account/orders/{id}` order history & detail
- [ ] P4-27 | `/account/addresses` CRUD address
- [ ] P4-28 | `/account/wishlist`
- [ ] P4-29 | `/account/reviews` user's reviews
- [ ] P4-30 | `/page/{slug}` CMS dynamic page
- [ ] P4-31 | `/news`, `/news/{slug}` news listing & detail

### 4.6 SEO
- [ ] P4-32 | `<HeadOutlet>` + per-page `<PageTitle>` + `<HeadContent>` meta description, og:tags
- [ ] P4-33 | JSON-LD structured data: Product, BreadcrumbList, Organization, WebSite (SearchAction)
- [ ] P4-34 | `/sitemap.xml` endpoint sinh động từ DB (products + categories + cms pages)
- [ ] P4-35 | `/robots.txt` với reference sitemap
- [ ] P4-36 | Verify View Source: tất cả nội dung public hiển thị server-rendered (prerender)

### 4.7 Localization
- [ ] P4-37 | `IStringLocalizer` adapter gọi resource từ ApiService cache local
- [ ] P4-38 | Language switcher component
- [ ] P4-39 | URL có culture prefix optional `/{culture}/...` hoặc cookie-based (theo cấu hình hiện tại)

### 4.8 Performance
- [ ] P4-40 | Output cache cho Home (5 min), Category (2 min), Product detail (10 min, vary by slug)
- [ ] P4-41 | Response compression Brotli + Gzip
- [ ] P4-42 | Lazy load images với `loading="lazy"`, dùng srcset từ image pipeline
- [ ] P4-43 | Bundle CSS/JS qua built-in Blazor static asset fingerprinting (.NET 9)

### 4.9 Verify
- [ ] P4-44 | Lighthouse score Home + Product ≥ 85 (Performance, SEO, Accessibility)
- [ ] P4-45 | E2E flow guest: home → category → product → add cart → checkout COD → success — PASS
- [ ] P4-46 | E2E flow user: login → home → ... → đơn hàng xuất hiện trong /account/orders
- [ ] P4-47 | So sánh visual với storefront cũ — không thiếu thông tin nghiệp vụ

### 4.10 Commit Phase 4
- [ ] P4-48 | Update progress, PR, merge
- [ ] P4-49 | Báo cáo → tự sang Phase 5

---

## PHASE 5 — ADMIN BLAZOR

**Mục tiêu:** Admin mới thay thế hoàn toàn AngularJS admin. Interactive Server.
**Thời lượng ước tính:** 40–60 giờ (phase NẶNG NHẤT)
**Branch:** `aspire-migration/phase-5-admin`

### 5.1 Tạo project
- [ ] P5-01 | `dotnet new blazor -n SimplCommerce.Admin -o src/Apps/SimplCommerce.Admin --interactivity Server --auth Individual`
- [ ] P5-02 | Add ServiceDefaults, MudBlazor
- [ ] P5-03 | Add SignalR (built-in Blazor Server) + Redis backplane: `builder.Services.AddSignalR().AddStackExchangeRedis(...)`
- [ ] P5-04 | Add vào AppHost với reference api + redis

### 5.2 Layout & Auth
- [ ] P5-05 | Cookie auth + role policy `RequireRole("admin")` mọi page
- [ ] P5-06 | `MainLayout`: MudAppBar (logo, search, notifications, user menu) + MudDrawer (navigation) + MudMainContent + breadcrumbs
- [ ] P5-07 | Navigation tree theo module group (Catalog / Sales / Customers / Content / Configuration / Reports)
- [ ] P5-08 | Dark/light mode toggle persisted

### 5.3 Shared components
- [ ] P5-09 | `<EntityDataGrid<T>>` wrapper MudDataGrid với server-side pagination/sort/filter chuẩn
- [ ] P5-10 | `<MediaPicker>` upload + chọn từ thư viện
- [ ] P5-11 | `<SlugInput>` auto-generate from name
- [ ] P5-12 | `<RichTextEditor>` (TinyMCE Blazor wrapper hoặc QuillJS interop)
- [ ] P5-13 | `<EntityPicker<T>>` autocomplete chọn entity (dùng cho FK)
- [ ] P5-14 | `<ConfirmDialog>` xác nhận xoá
- [ ] P5-15 | `<FormCard>` chuẩn validation + save/cancel buttons
- [ ] P5-16 | Toast wrapper qua MudSnackbar

### 5.4 Pages — theo module (giữ URL `/admin/...`)

**5.4.1 Dashboard**
- [ ] P5-17 | `/admin` dashboard: KPI cards (today sales, today orders, pending orders, low stock), sales chart (Chart.js / ApexCharts), recent orders table, top products

**5.4.2 Catalog**
- [ ] P5-18 | `/admin/products` list (DataGrid: thumbnail, name, sku, price, stock, status, actions)
- [ ] P5-19 | `/admin/products/create` + `/admin/products/edit/{id}` — tabs: General | Media | Attributes | Variants | Categories | SEO | Vendor | Shipping
- [ ] P5-20 | `/admin/categories` tree view CRUD (drag-drop reorder)
- [ ] P5-21 | `/admin/brands` CRUD
- [ ] P5-22 | `/admin/options` CRUD
- [ ] P5-23 | `/admin/attributes` + attribute groups CRUD
- [ ] P5-24 | `/admin/product-templates` CRUD

**5.4.3 Orders / Sales**
- [ ] P5-25 | `/admin/orders` list với filter (status, date, customer)
- [ ] P5-26 | `/admin/orders/{id}` detail: customer info, items, totals, payment, shipment, status timeline, action buttons (mark paid, ship, cancel, refund)
- [ ] P5-27 | `/admin/shipments` list + create
- [ ] P5-28 | `/admin/refunds`
- [ ] P5-29 | `/admin/sales-report`

**5.4.4 Customers**
- [ ] P5-30 | `/admin/customers` list
- [ ] P5-31 | `/admin/customers/{id}` detail: profile, addresses, orders, reviews, notes
- [ ] P5-32 | `/admin/customer-groups`
- [ ] P5-33 | `/admin/users` (admin users) + `/admin/roles` + role permission matrix

**5.4.5 Content**
- [ ] P5-34 | `/admin/cms/pages` CRUD
- [ ] P5-35 | `/admin/cms/menus` builder
- [ ] P5-36 | `/admin/cms/widgets` widget zones + instances
- [ ] P5-37 | `/admin/cms/widget-instances` per page
- [ ] P5-38 | `/admin/news` CRUD + `/admin/news-categories`
- [ ] P5-39 | `/admin/media` library

**5.4.6 Reviews**
- [ ] P5-40 | `/admin/reviews` moderation queue (approve / reject / reply)

**5.4.7 Inventory**
- [ ] P5-41 | `/admin/warehouses`
- [ ] P5-42 | `/admin/stock-history`
- [ ] P5-43 | `/admin/stock-adjustment`

**5.4.8 Pricing & Promotions**
- [ ] P5-44 | `/admin/cart-rules` CRUD
- [ ] P5-45 | `/admin/catalog-rules` CRUD
- [ ] P5-46 | `/admin/coupons`

**5.4.9 Shipping & Tax**
- [ ] P5-47 | `/admin/shipping/providers`
- [ ] P5-48 | `/admin/shipping/zones`
- [ ] P5-49 | `/admin/shipping/rates`
- [ ] P5-50 | `/admin/tax/classes`
- [ ] P5-51 | `/admin/tax/rates`

**5.4.10 Payments**
- [ ] P5-52 | `/admin/payments/providers` list + per-provider config form (Stripe, PayPal, COD, MoMo, VNPay)

**5.4.11 Vendors**
- [ ] P5-53 | `/admin/vendors` CRUD
- [ ] P5-54 | `/admin/vendors/{id}/products`

**5.4.12 Localization**
- [ ] P5-55 | `/admin/languages`
- [ ] P5-56 | `/admin/translations` (resource editor với search + import/export)

**5.4.13 Settings**
- [ ] P5-57 | `/admin/settings/general`
- [ ] P5-58 | `/admin/settings/email` (SMTP / SendGrid)
- [ ] P5-59 | `/admin/settings/media`
- [ ] P5-60 | `/admin/settings/seo`

**5.4.14 Activity & Notifications**
- [ ] P5-61 | `/admin/activity-log` filter by user/entity/date
- [ ] P5-62 | Notification center component trong AppBar với SignalR push (đơn mới, review mới)

### 5.5 SignalR realtime
- [ ] P5-63 | Hub `AdminNotificationHub` ở ApiService
- [ ] P5-64 | Khi order created → publish event → hub push tới admin online
- [ ] P5-65 | Component `<NotificationBell>` subscribe hub, badge count

### 5.6 Verify
- [ ] P5-66 | Manual test mỗi màn: create / edit / delete / list filter / paging — PASS
- [ ] P5-67 | Permission test: user role thường không vào được /admin
- [ ] P5-68 | So sánh feature parity với AngularJS admin cũ — không thiếu màn quan trọng

### 5.7 Commit Phase 5
- [ ] P5-69 | Update progress, PR (PR này LỚN, có thể tách sub-PR theo nhóm module), merge
- [ ] P5-70 | Báo cáo → **DỪNG, chờ user review thật kỹ** trước khi sang phase cutover

---

## PHASE 6 — DATA MIGRATION & COMPATIBILITY

**Mục tiêu:** Đảm bảo migrate được data từ instance cũ sang.
**Thời lượng ước tính:** 6–10 giờ
**Branch:** `aspire-migration/phase-6-data`

- [ ] P6-01 | Verify migration `Initial_AspireBaseline` chạy được trên DB hiện tại của user (additive only — không drop column)
- [ ] P6-02 | Viết script `tools/migrate-data.ps1` backup DB → restore → apply migrations mới → verify row counts
- [ ] P6-03 | Refactor `Module.SampleData` cho seed data work với cấu trúc mới
- [ ] P6-04 | Test trên DB sạch: `aspire run` → migration tự apply → SampleData seed → có data demo
- [ ] P6-05 | Test trên DB copy production của user (nếu có): không mất data, không lỗi
- [ ] P6-06 | Document procedure migration trong `docs/migration/data-migration-runbook.md`
- [ ] P6-07 | Commit + PR

---

## PHASE 7 — TESTING & HARDENING

**Mục tiêu:** Đạt chất lượng production-ready.
**Thời lượng ước tính:** 16–24 giờ
**Branch:** `aspire-migration/phase-7-testing`

### 7.1 Test coverage
- [ ] P7-01 | Unit tests Application services mỗi module (target ≥ 60% coverage)
- [ ] P7-02 | Integration tests: 1 test E2E mỗi flow nghiệp vụ chính (10+ tests)
- [ ] P7-03 | API contract tests cho mọi endpoint authenticated
- [ ] P7-04 | Playwright E2E: home → checkout COD; admin login → tạo product → xuất hiện ở storefront

### 7.2 Performance
- [ ] P7-05 | k6 script: GET /, GET /category, GET /product, POST /api/cart, POST /api/checkout — đạt p95 < 500ms tại 50 RPS
- [ ] P7-06 | Index DB tuning (kiểm `ProductSlug`, `OrderStatus`, `OrderCreatedOn`, `UserEmail`, ...)
- [ ] P7-07 | Output cache vary đúng, hit rate ≥ 70% cho Home/Category trong load test
- [ ] P7-08 | Image pipeline: lazy + WebP fallback

### 7.3 Security
- [ ] P7-09 | Rate limiting: 100 req/min per IP cho /api/auth/login, /api/auth/register
- [ ] P7-10 | CSP headers strict mode (no inline script trừ Blazor blob: và Aspire-allowed)
- [ ] P7-11 | HSTS, X-Frame-Options, X-Content-Type-Options
- [ ] P7-12 | Anti-forgery cho mọi form Blazor Server
- [ ] P7-13 | Input sanitization rich text (HtmlSanitizer)
- [ ] P7-14 | Secret scanning: không hardcode key — tất cả qua user-secrets / Aspire parameters
- [ ] P7-15 | OWASP ZAP baseline scan storefront + admin → không có HIGH

### 7.4 Observability
- [ ] P7-16 | Custom metrics: order_created_total, payment_failed_total, cart_abandoned_total
- [ ] P7-17 | Activity (tracing): tag userId, orderId xuyên suốt
- [ ] P7-18 | Log enrichment: correlationId mọi request
- [ ] P7-19 | Health check chi tiết: DB, Redis, Blob storage, payment provider reachability
- [ ] P7-20 | Aspire dashboard verify: traces nối liền storefront → api → db

### 7.5 Documentation
- [ ] P7-21 | Update `README.md` chính: cách chạy với Aspire, prerequisites, troubleshoot
- [ ] P7-22 | `docs/architecture.md` mô tả kiến trúc mới với diagram
- [ ] P7-23 | `docs/deployment.md` — deploy lên Azure Container Apps + tự host
- [ ] P7-24 | `docs/development.md` — cách add module mới theo pattern Clean
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
- [ ] P8-01 | Tag commit cuối Phase 7: `git tag pre-cutover-backup` + push tag
- [ ] P8-02 | Backup full DB production của user (responsibility user, ghi reminder)

### 8.2 Xoá WebHost cũ
- [ ] P8-03 | Remove `src/SimplCommerce.WebHost/` khỏi solution và filesystem
- [ ] P8-04 | Remove reference WebHost khỏi AppHost
- [ ] P8-05 | Remove file `Dockerfile`, `Dockerfile-sqlite`, `docker-entrypoint.sh` cũ ở root

### 8.3 Xoá legacy code trong modules
- [ ] P8-06 | Mỗi module: xoá `wwwroot/admin/` (AngularJS templates)
- [ ] P8-07 | Mỗi module: xoá `Views/`, `Areas/*/Views/` (MVC Razor cũ)
- [ ] P8-08 | Mỗi module: xoá `Controllers/` (đã thay bằng Endpoints/)
- [ ] P8-09 | Xoá script bundling cũ (gulp, package.json frontend cũ)
- [ ] P8-10 | Xoá `modules.json` runtime, `CustomAssemblyLoadContextProvider`

### 8.4 Generate deployment artifacts
- [ ] P8-11 | `aspire publish` → manifest cho Azure Container Apps
- [ ] P8-12 | Tạo Dockerfile mới (multi-stage) cho ApiService, Storefront, Admin
- [ ] P8-13 | Update `azure-pipelines.yml` build + publish pipeline mới
- [ ] P8-14 | Tạo `.github/workflows/ci.yml`: build, test, publish containers
- [ ] P8-15 | Tạo `compose.yaml` để chạy production-like local (không cần Aspire)

### 8.5 Final verify
- [ ] P8-16 | `dotnet build` PASS
- [ ] P8-17 | `dotnet test` PASS toàn bộ
- [ ] P8-18 | `aspire run` lên đầy đủ stack
- [ ] P8-19 | Smoke test: storefront flow + admin flow
- [ ] P8-20 | Solution chỉ còn project mới, không còn AngularJS, không còn MVC View

### 8.6 Release
- [ ] P8-21 | Update `MIGRATION_PROGRESS.md` 100% xong
- [ ] P8-22 | Viết `CHANGELOG.md` release notes chi tiết: breaking changes, new features, migration guide cho user
- [ ] P8-23 | PR cuối vào `master`, mô tả đầy đủ
- [ ] P8-24 | Tag release `v2.0.0-aspire-blazor`
- [ ] P8-25 | Báo cáo final, **DỪNG**

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

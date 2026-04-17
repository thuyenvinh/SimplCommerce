# Prompt cho Claude Code: Migrate SimplCommerce → .NET Aspire + Blazor

> Copy toàn bộ nội dung dưới đây và paste vào Claude Code sau khi đã connect repo SimplCommerce trên GitHub.

---

## CONTEXT

Bạn là kỹ sư .NET senior được giao nhiệm vụ chuyển đổi toàn bộ dự án **SimplCommerce** (https://github.com/simplcommerce/SimplCommerce) — một hệ thống ecommerce modulith chạy trên ASP.NET Core 8 + AngularJS 1.6 — sang kiến trúc hiện đại **.NET 9 + .NET Aspire 9 + Blazor Web App (Interactive Auto)**.

Repo đã được kết nối. Hãy đọc toàn bộ codebase trước khi bắt đầu.

## NGUYÊN TẮC LÀM VIỆC (BẮT BUỘC TUÂN THỦ)

1. **Làm việc theo phase, mỗi phase phải build thành công và commit riêng** với message rõ ràng theo Conventional Commits (`feat:`, `refactor:`, `chore:`...).
2. **Không xoá code cũ cho đến phase cuối** — giữ `SimplCommerce.WebHost` chạy song song để so sánh hành vi.
3. **Mỗi khi hoàn thành một module, viết test integration** (xUnit + `Aspire.Hosting.Testing` + WebApplicationFactory) verify CRUD cơ bản.
4. **Không bịa API contract** — đọc Controllers/Services hiện tại để giữ nguyên signature nghiệp vụ.
5. **Khi gặp quyết định kiến trúc không rõ**, ghi vào `MIGRATION_DECISIONS.md` ở root, KHÔNG tự ý chọn rồi đi tiếp âm thầm.
6. **Sau mỗi phase, update `MIGRATION_PROGRESS.md`** với checklist trạng thái từng module.
7. **Chạy `dotnet build` và `dotnet test` trước mỗi commit.** Không commit code không build được.
8. **Branch strategy:** tạo branch `aspire-migration/main`, mỗi phase tạo sub-branch `aspire-migration/phase-N-xxx` rồi merge vào main migration branch qua PR (mô tả PR đầy đủ).

## KIẾN TRÚC ĐÍCH

```
SimplCommerce.AppHost                ← Aspire orchestrator (.NET 9)
SimplCommerce.ServiceDefaults        ← OTel, health checks, resilience

src/Apps/
  SimplCommerce.ApiService           ← Minimal API + JWT + OpenAPI/Scalar
  SimplCommerce.Storefront           ← Blazor Web App (Interactive Auto, MudBlazor)
  SimplCommerce.Admin                ← Blazor Web App (Interactive Server, MudBlazor)

src/Modules/SimplCommerce.Module.<Name>/
  Domain/          (entities, domain events, value objects)
  Application/     (services, MediatR 12 handlers, DTOs, validators)
  Infrastructure/  (EF mappings, repositories, external integrations)
  Endpoints/       (Minimal API endpoint groups — extension methods)
  Components/      (Razor Class Library — Blazor components dùng chung Storefront/Admin)

src/Shared/
  SimplCommerce.Infrastructure       (giữ, dọn dẹp)
  SimplCommerce.Module.Core          (User, Role, EntityType — refactor cấu trúc)

src/Migrations/
  SimplCommerce.Migrations           (gộp tất cả EF migrations)

tests/
  SimplCommerce.IntegrationTests
  SimplCommerce.UnitTests
```

**Hạ tầng Aspire orchestrate:**
- SQL Server 2022 (container)
- Redis (cache + SignalR backplane + distributed session)
- Azurite (Azure Blob emulator cho file uploads)
- MailDev (test SMTP)
- Seq (structured logs)

## STACK & VERSIONS (CHỐT CỨNG)

- .NET 9 (cập nhật `global.json`)
- Aspire 9.x (latest stable)
- Blazor Web App với render mode `InteractiveAuto` (Storefront), `InteractiveServer` (Admin)
- **MudBlazor** cho UI components (cả 2 app)
- MediatR 12
- FluentValidation 11
- EF Core 9 (giữ SQL Server làm primary, drop PostgreSQL trong phase 1, có thể add lại sau)
- ASP.NET Identity Core (cookie cho Blazor app, JWT cho API)
- Serilog + OpenTelemetry export sang Seq + Aspire dashboard
- xUnit + FluentAssertions + Testcontainers
- Scalar (thay Swagger UI) cho API docs

## LỘ TRÌNH 8 PHASE — THỰC HIỆN TUẦN TỰ

### Phase 0: Discovery & Setup (tạo `MIGRATION_DECISIONS.md`, `MIGRATION_PROGRESS.md`)
- Đọc toàn bộ `src/`, list ra:
  - Tất cả module trong `src/Modules/`
  - Tất cả Controller (MVC + API) trong từng module
  - Tất cả AngularJS view trong `wwwroot/admin` của từng module
  - Tất cả Razor View trong `Views/` của từng module và WebHost
  - Domain events (MediatR `INotification`)
  - External integrations (Stripe, PayPal, MoMo, VNPay, SendGrid…)
- Output: file `MIGRATION_INVENTORY.md` ở root chứa bảng kê đầy đủ.
- Update `global.json` lên .NET 9.
- **Không sửa code khác.** Commit: `chore: phase 0 - migration inventory`.

### Phase 1: Aspire Bootstrap
- Tạo `SimplCommerce.AppHost` và `SimplCommerce.ServiceDefaults`.
- Khai báo resources: SqlServer, Redis, Azurite, MailDev, Seq.
- Wrap `SimplCommerce.WebHost` hiện tại làm project resource trong AppHost (chạy nguyên trạng).
- Verify: `aspire run` → dashboard hiện, WebHost cũ chạy được, kết nối SQL container OK.
- Commit: `feat: phase 1 - aspire app host bootstrap`.

### Phase 2: Refactor Core & Infrastructure
- Refactor `SimplCommerce.Infrastructure` và `SimplCommerce.Module.Core` theo Clean layering (Domain/Application/Infrastructure).
- Loại bỏ `ModuleInitializer` runtime-loading. Mỗi module export extension method `AddXxxModule(IHostApplicationBuilder)` và `MapXxxEndpoints(IEndpointRouteBuilder)`.
- Gộp tất cả EF migrations về `SimplCommerce.Migrations` project. Tạo migration mới `Initial_Aspire` snapshot toàn bộ schema hiện tại.
- Nâng MediatR 7 → 12, sửa breaking changes.
- WebHost cũ vẫn phải build và chạy.
- Commit: `refactor: phase 2 - core layering and module registration`.

### Phase 3: API Service
- Tạo `SimplCommerce.ApiService` (Minimal API, .NET 9).
- Migrate **toàn bộ** `Module.StorefrontApi` controllers → endpoint groups Minimal API.
- Migrate **toàn bộ** API controllers admin (đang được AngularJS gọi) → endpoint groups.
- JWT Bearer auth (issuer = ApiService, ASP.NET Identity backed).
- OpenAPI + Scalar UI tại `/scalar`.
- FluentValidation cho mọi request DTO.
- Health check endpoint chuẩn Aspire.
- Output cache + response compression.
- Test integration: ít nhất 1 endpoint mỗi module (GET list).
- Commit: `feat: phase 3 - api service with all endpoints migrated`.

### Phase 4: Storefront Blazor
- Tạo `SimplCommerce.Storefront` Blazor Web App, Interactive Auto.
- Setup MudBlazor theme (light/dark), layout responsive.
- Implement các trang theo thứ tự ưu tiên:
  1. Home (featured products, categories)
  2. Catalog/Category listing với filter, sort, paging
  3. Product Detail (gallery, variants, add to cart, reviews)
  4. Search results
  5. Cart (Redis-backed cho guest, DB cho user)
  6. Checkout flow (address → shipping → payment → confirm)
  7. Account (login, register, profile, order history, wishlist)
  8. CMS pages (about, contact, dynamic pages từ Module.Cms)
- Localization: load resources từ `Module.Localization` qua API.
- SEO: prerender tất cả trang public, sitemap.xml động, robots.txt, structured data (JSON-LD) cho Product.
- HttpClient typed clients gọi ApiService qua service discovery của Aspire.
- Commit incremental sau mỗi nhóm trang: `feat(storefront): home and catalog pages`, v.v.

### Phase 5: Admin Blazor
- Tạo `SimplCommerce.Admin` Blazor Web App, Interactive Server.
- Auth: ASP.NET Identity cookie + role-based authorization.
- Layout: MudBlazor drawer + appbar + breadcrumbs.
- Migrate AngularJS screens → Razor Components, theo thứ tự:
  1. Dashboard (sales chart, recent orders, low stock)
  2. Catalog: Product list (MudDataGrid server-side), Product edit (tabs: general, media, attributes, variants, SEO), Category tree, Brand, Option, Attribute, Product template
  3. Orders: list, detail (timeline status), refund, shipment
  4. Customers: list, detail, address, order history
  5. Reviews: moderation queue, replies
  6. Inventory: warehouse, stock movement
  7. Pricing: catalog price rule, cart price rule, coupon
  8. CMS: page, menu, widget, news, news category
  9. Shipping: providers, rates, zones
  10. Tax: classes, rates
  11. Payments: providers config (Stripe, PayPal, COD, MoMo, VNPay)
  12. Vendors
  13. Localization: language, resources
  14. Settings: general, email (SendGrid), media
  15. Activity log, Notifications
- File upload qua `InputFile` → ApiService → Azurite/Blob.
- Realtime order notification: SignalR hub (Redis backplane).
- Commit incremental theo nhóm module.

### Phase 6: Data Migration & Compatibility
- Viết script PowerShell + EF migration kiểm tra schema mới tương thích dữ liệu hiện hữu của user (không drop cột, chỉ add).
- Seed sample data qua `Module.SampleData` refactored.
- Export/import script cho user migrate dữ liệu production.
- Commit: `feat: phase 6 - data migration tooling`.

### Phase 7: Testing & Hardening
- Integration test cho:
  - Toàn bộ checkout flow (E2E qua Playwright optional)
  - CRUD mỗi entity chính
  - Auth flow (register, login, logout, password reset)
  - Payment webhook handlers (mock)
- Load test script k6 cho 5 endpoint nóng nhất (product list, search, add to cart, checkout, order create).
- Security: rate limiting, CORS chuẩn, anti-forgery, CSP headers, input sanitization.
- Performance: output caching, response compression, image resizing pipeline.
- Update `README.md` với hướng dẫn chạy mới.
- Commit: `test: phase 7 - integration tests and hardening`.

### Phase 8: Cutover & Cleanup
- Chỉ thực hiện khi tất cả phase trước **xanh**.
- Xoá `SimplCommerce.WebHost` cũ.
- Xoá toàn bộ AngularJS code trong `wwwroot/admin/**` của các module.
- Xoá MVC Views storefront cũ.
- Xoá `Dockerfile`, `Dockerfile-sqlite`, `docker-entrypoint.sh` cũ.
- Generate Aspire publish manifest cho deployment (Azure Container Apps + k8s).
- Update `azure-pipelines.yml` cho pipeline mới.
- Final commit: `chore: phase 8 - remove legacy code, cutover complete`.
- Tạo PR tổng vào `master` với release notes chi tiết.

## RÀNG BUỘC NGHIỆP VỤ — KHÔNG ĐƯỢC THAY ĐỔI

- Workflow đặt hàng: New → PendingPayment → PaymentReceived → Shipping → Shipped → Complete (+ Canceled, Refunded).
- Schema URL public storefront giữ nguyên (`/product/{slug}`, `/category/{slug}`, `/search?q=`, `/cart`, `/checkout`).
- Email templates, payment gateway callback URL giữ nguyên path.
- Admin URL prefix `/admin` giữ nguyên.
- Account `admin@simplcommerce.com / 1qazZAQ!` vẫn login được sau migration.

## DEFINITION OF DONE (CHO TOÀN BỘ MIGRATION)

- [ ] `aspire run` chạy toàn bộ stack từ một lệnh duy nhất, dashboard healthy.
- [ ] Storefront và Admin truy cập được, login admin mặc định OK.
- [ ] Tất cả CRUD chính của 25 module hoạt động qua Admin UI mới.
- [ ] Checkout end-to-end với ít nhất 1 payment provider (COD) thành công.
- [ ] Test suite pass 100%, coverage Application layer ≥ 60%.
- [ ] Không còn reference đến AngularJS, MVC Views cũ.
- [ ] `MIGRATION_PROGRESS.md` checklist 100% xong.
- [ ] README hướng dẫn chạy + deploy đầy đủ.
- [ ] PR cuối có changelog chi tiết.

## QUY TẮC TRÌNH BÀY TIẾN ĐỘ

Sau mỗi phase, báo cáo cho user theo template:
```
✅ Phase N: <Tên>
- Files changed: X | Added: Y | Deleted: Z
- Tests: A passed / B total
- Build: OK
- Commit: <hash> <message>
- Decisions logged: <link MIGRATION_DECISIONS.md sections>
- Next: Phase N+1 - <tên>
- Blockers (nếu có): ...
```

Nếu một phase mất quá 4 giờ làm việc của bạn, **dừng lại, báo cáo trạng thái, hỏi user** trước khi tiếp tục.

## BẮT ĐẦU NGAY

Bắt đầu từ **Phase 0**. Không hỏi lại confirmation. Khi xong Phase 0, báo cáo và tự động sang Phase 1. Khi xong Phase 1, báo cáo và DỪNG để user review trước khi sang Phase 2 (vì Phase 2 trở đi sẽ thay đổi nhiều).

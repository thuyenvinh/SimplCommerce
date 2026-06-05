# MIGRATION INVENTORY

> Bản kê đầy đủ SimplCommerce hiện tại, phục vụ Phase 0 Discovery. Dữ liệu được thu thập tự động từ mã nguồn tại commit HEAD của branch `claude/phase-0-migration-pX925` (2026-04-17).

---

## 1. MODULES

### 1.1 Tổng quan

- **Tổng số module:** 43 (dưới `src/Modules/`)
- **Tổng `.cs` files trong modules:** ~866
- **Modules có `wwwroot/admin/` (AngularJS templates):** 21
- **Modules có `Areas/` (Razor View MVC):** 26
- **Modules có `Migrations/`:** 0 (tất cả migration nằm tập trung ngoài module — xem §6)
- **Tất cả 43 modules đều có `IsBundledWithHost = true`** trong `module.json`

> Note: Prompt CLAUDE_CODE_PROMPT.md ước tính "25 module" — thực tế là **43**. Tất cả các module storage (AmazonS3, AzureBlob, Local), toàn bộ 7 payment providers (Braintree, Cashfree, CoD, Momo, NganLuong, PaypalExpress, Stripe + Payments core), 4 shipping (Shipping, ShippingFree, ShippingPrices, ShippingTableRate), 2 email senders (Smtp, Sendgrid), Shipments, Comments, Contacts, DinkToPdf, HangfireJobs, SignalR, Notifications, ProductComparison, ProductRecentlyViewed — đều là module riêng cần migrate. Cần lưu ý phạm vi Phase 2–5 rộng hơn dự kiến ban đầu.

> **Correction (2026-04-18):** báo cáo trước đây ghi "41 modules" là sai. Bảng chi tiết bên dưới luôn có 43 hàng — lỗi chỉ ở summary text, đã sửa.

### 1.2 Bảng chi tiết

| Module | Path | .cs | wwwroot/admin | Views/ | Areas/ | Migrations/ | IsBundledWithHost |
|---|---|---|---|---|---|---|---|
| ActivityLog | src/Modules/SimplCommerce.Module.ActivityLog | 9 | Yes | No | Yes | No | true |
| Catalog | src/Modules/SimplCommerce.Module.Catalog | 102 | Yes | No | Yes | No | true |
| Checkouts | src/Modules/SimplCommerce.Module.Checkouts | 16 | No | No | Yes | No | true |
| Cms | src/Modules/SimplCommerce.Module.Cms | 32 | Yes | No | Yes | No | true |
| Comments | src/Modules/SimplCommerce.Module.Comments | 13 | Yes | No | Yes | No | true |
| Contacts | src/Modules/SimplCommerce.Module.Contacts | 14 | Yes | No | Yes | No | true |
| Core | src/Modules/SimplCommerce.Module.Core | 123 | Yes | No | Yes | No | true |
| DinkToPdf | src/Modules/SimplCommerce.Module.DinkToPdf | 2 | No | No | No | No | true |
| EmailSenderSendgrid | src/Modules/SimplCommerce.Module.EmailSenderSendgrid | 2 | No | No | No | No | true |
| EmailSenderSmtp | src/Modules/SimplCommerce.Module.EmailSenderSmtp | 4 | No | No | No | No | true |
| HangfireJobs | src/Modules/SimplCommerce.Module.HangfireJobs | 16 | No | No | No | No | true |
| Inventory | src/Modules/SimplCommerce.Module.Inventory | 19 | Yes | No | Yes | No | true |
| Localization | src/Modules/SimplCommerce.Module.Localization | 14 | Yes | No | Yes | No | true |
| News | src/Modules/SimplCommerce.Module.News | 19 | Yes | No | Yes | No | true |
| Notifications | src/Modules/SimplCommerce.Module.Notifications | 47 | No | No | Yes | No | true |
| Orders | src/Modules/SimplCommerce.Module.Orders | 36 | Yes | No | Yes | No | true |
| PaymentBraintree | src/Modules/SimplCommerce.Module.PaymentBraintree | 10 | Yes | No | Yes | No | true |
| PaymentCashfree | src/Modules/SimplCommerce.Module.PaymentCashfree | 9 | Yes | No | Yes | No | true |
| PaymentCoD | src/Modules/SimplCommerce.Module.PaymentCoD | 7 | Yes | No | Yes | No | true |
| PaymentMomo | src/Modules/SimplCommerce.Module.PaymentMomo | 14 | Yes | No | Yes | No | true |
| PaymentNganLuong | src/Modules/SimplCommerce.Module.PaymentNganLuong | 14 | Yes | No | Yes | No | true |
| PaymentPaypalExpress | src/Modules/SimplCommerce.Module.PaymentPaypalExpress | 12 | Yes | No | Yes | No | true |
| PaymentStripe | src/Modules/SimplCommerce.Module.PaymentStripe | 8 | Yes | No | Yes | No | true |
| Payments | src/Modules/SimplCommerce.Module.Payments | 10 | Yes | No | Yes | No | true |
| Pricing | src/Modules/SimplCommerce.Module.Pricing | 21 | Yes | No | Yes | No | true |
| ProductComparison | src/Modules/SimplCommerce.Module.ProductComparison | 13 | No | No | Yes | No | true |
| ProductRecentlyViewed | src/Modules/SimplCommerce.Module.ProductRecentlyViewed | 11 | Yes | No | Yes | No | true |
| Reviews | src/Modules/SimplCommerce.Module.Reviews | 21 | Yes | No | Yes | No | true |
| SampleData | src/Modules/SimplCommerce.Module.SampleData | 8 | No | No | Yes | No | true |
| Search | src/Modules/SimplCommerce.Module.Search | 7 | Yes | No | Yes | No | true |
| Shipments | src/Modules/SimplCommerce.Module.Shipments | 11 | Yes | No | Yes | No | true |
| Shipping | src/Modules/SimplCommerce.Module.Shipping | 5 | Yes | No | Yes | No | true |
| ShippingFree | src/Modules/SimplCommerce.Module.ShippingFree | 4 | No | No | No | No | true |
| ShippingPrices | src/Modules/SimplCommerce.Module.ShippingPrices | 8 | No | No | No | No | true |
| ShippingTableRate | src/Modules/SimplCommerce.Module.ShippingTableRate | 6 | Yes | No | Yes | No | true |
| ShoppingCart | src/Modules/SimplCommerce.Module.ShoppingCart | 17 | No | No | Yes | No | true |
| SignalR | src/Modules/SimplCommerce.Module.SignalR | 10 | No | No | No | No | true |
| StorageAmazonS3 | src/Modules/SimplCommerce.Module.StorageAmazonS3 | 2 | No | No | No | No | true |
| StorageAzureBlob | src/Modules/SimplCommerce.Module.StorageAzureBlob | 2 | No | No | No | No | true |
| StorageLocal | src/Modules/SimplCommerce.Module.StorageLocal | 2 | No | No | No | No | true |
| Tax | src/Modules/SimplCommerce.Module.Tax | 12 | Yes | No | Yes | No | true |
| Vendors | src/Modules/SimplCommerce.Module.Vendors | 6 | Yes | No | Yes | No | true |
| WishList | src/Modules/SimplCommerce.Module.WishList | 12 | No | No | Yes | No | true |

> Ghi chú cột `Views/`: cột này check directory `Views/` ở module root. Thực tế SimplCommerce dùng convention `Areas/<Area>/Views/` nên module nào có `Areas/` đều có .cshtml bên trong. Xem chi tiết Razor View ở §3.

### 1.3 Dependency graph

Xem `docs/migration/module-dependencies.md` (Mermaid + adjacency list đầy đủ).

---

## 2. API & CONTROLLERS

- Tổng controller: **104**
- Storefront API: 28 (26.9%)
- Admin API: 68 (65.4%)
- MVC View: 8 (7.7%)

Chi tiết tại `docs/migration/api-inventory.md`.

---

## 3. UI (AngularJS + Razor)

- AngularJS admin `.html`: **98** (Catalog chiếm 23, Core 14, Cms 9)
- Razor Views `.cshtml`: **181** (Modules 137 + WebHost 44)
- Top màn phức tạp: `product` (5 templates), `shipment`, `review`, `order` (4 mỗi loại)

Chi tiết tại `docs/migration/ui-inventory.md`.

---

## 4. CROSS-CUTTING — DOMAIN EVENTS, INTEGRATIONS, HOSTED SERVICES, CONFIG

### 4.1 Domain events (MediatR)
14 event–handler pairs. Các event chính: `OrderCreated`, `AfterOrderCreated`, `OrderChanged`, `OrderDetailGot`, `UserSignedIn`, `EntityViewed`, `ReviewSummaryChanged`, `EntityDeleting`, `ProductBackInStock`.

### 4.2 External integrations
- **Payment (7):** Stripe, PayPal Express, Braintree, MoMo, Cashfree, NganLuong, COD. Prompt đề cập **VNPay** — chưa có module hiện hữu (cần xác nhận với user).
- **Email (2):** SendGrid, SMTP (MailKit)
- **Storage (3):** Azure Blob, Amazon S3, Local
- **Khác:** DinkToPdf (invoice), Hangfire (job scheduler, SqlServer storage), SignalR (không Redis backplane), OAuth Google + Facebook.

### 4.3 Background services
- `SchedulerBackgroundService` (Infrastructure): cron-based `IScheduledTask`
- `OrderCancellationBackgroundService` (Orders): auto-cancel pending orders > 5min, interval 60s
- `NotificationDistributionJob` (Notifications): Hangfire-driven

### 4.4 Config keys
Chi tiết `docs/migration/cross-cutting-inventory.md` §4. Aspire sẽ inject connection strings qua `builder.AddSqlServer("sql").AddDatabase("SimplCommerce")`, `AddRedis("redis")`, `AddAzureStorage("storage").RunAsEmulator()`, `AddMailDev("maildev")`, `AddSeq("seq")`.

---

## 5. NUGET & TOOLCHAIN BASELINE

Xem `docs/migration/baseline-versions.md`.

---

## 6. DATABASE & MIGRATIONS

Xem `docs/migration/baseline-schema.sql` (export `dotnet ef migrations script` — chạy thủ công vì sandbox không có dotnet, xem `phase0-manual-steps.md`).

# Module Dependency Graph

> Trích xuất từ `<ProjectReference>` trong các `.csproj` dưới `src/Modules/` và `src/SimplCommerce.WebHost/`. Các ProjectReference ngoài SimplCommerce (Infrastructure, NuGet) giữ lại để nhìn rõ hạ tầng.

## Mermaid dependency graph (module → module)

```mermaid
flowchart LR
    Infra[SimplCommerce.Infrastructure]
    Core[Module.Core]

    ActivityLog[Module.ActivityLog] --> Infra
    ActivityLog --> Core
    Catalog[Module.Catalog] --> Infra
    Catalog --> Core
    Catalog --> Tax[Module.Tax]
    Tax --> Infra
    Tax --> Core
    Checkouts[Module.Checkouts] --> Infra
    Checkouts --> Catalog
    Checkouts --> Core
    Checkouts --> ShippingPrices[Module.ShippingPrices]
    Checkouts --> ShoppingCart[Module.ShoppingCart]
    Cms[Module.Cms] --> Infra
    Cms --> Core
    Comments[Module.Comments] --> Infra
    Comments --> Catalog
    Comments --> Core
    Contacts[Module.Contacts] --> Infra
    Contacts --> Core
    Core --> Infra
    DinkToPdf[Module.DinkToPdf] --> Infra
    DinkToPdf --> Core
    Sendgrid[Module.EmailSenderSendgrid] --> Core
    Smtp[Module.EmailSenderSmtp] --> Core
    Hangfire[Module.HangfireJobs] --> Infra
    Inventory[Module.Inventory] --> Infra
    Inventory --> Catalog
    Inventory --> Core
    Localization[Module.Localization] --> Infra
    Localization --> Core
    News[Module.News] --> Infra
    News --> Core
    Notifications[Module.Notifications] --> Infra
    Notifications --> Core
    Notifications --> Hangfire
    Notifications --> SignalR[Module.SignalR]
    SignalR --> Infra
    SignalR --> Core
    Orders[Module.Orders] --> Infra
    Orders --> Catalog
    Orders --> Checkouts
    Orders --> Core
    Orders --> Pricing[Module.Pricing]
    Orders --> ShippingPrices
    Pricing --> Infra
    Pricing --> Catalog
    Pricing --> Core
    Payments[Module.Payments] --> Infra
    Payments --> Checkouts
    Payments --> Core
    Payments --> Orders
    PaymentBraintree[Module.PaymentBraintree] --> Payments
    PaymentBraintree --> Core
    PaymentBraintree --> Infra
    PaymentCashfree[Module.PaymentCashfree] --> Payments
    PaymentCashfree --> Core
    PaymentCashfree --> Infra
    PaymentCashfree --> Orders
    PaymentCoD[Module.PaymentCoD] --> Payments
    PaymentCoD --> Core
    PaymentCoD --> Infra
    PaymentCoD --> Checkouts
    PaymentCoD --> Orders
    PaymentMomo[Module.PaymentMomo] --> Payments
    PaymentMomo --> Core
    PaymentMomo --> Infra
    PaymentMomo --> Orders
    PaymentNganLuong[Module.PaymentNganLuong] --> Payments
    PaymentNganLuong --> Core
    PaymentNganLuong --> Infra
    PaymentNganLuong --> Orders
    PaymentPaypalExpress[Module.PaymentPaypalExpress] --> Payments
    PaymentPaypalExpress --> Core
    PaymentPaypalExpress --> Infra
    PaymentPaypalExpress --> Orders
    PaymentStripe[Module.PaymentStripe] --> Payments
    PaymentStripe --> Core
    PaymentStripe --> Infra
    PaymentStripe --> Orders
    ProductComparison[Module.ProductComparison] --> Infra
    ProductComparison --> Catalog
    ProductComparison --> Core
    ProductRecentlyViewed[Module.ProductRecentlyViewed] --> Catalog
    Reviews[Module.Reviews] --> Infra
    Reviews --> Catalog
    Reviews --> Core
    Reviews --> Orders
    SampleData[Module.SampleData] --> Infra
    SampleData --> Core
    Search[Module.Search] --> Infra
    Search --> Catalog
    Search --> Core
    Search --> Localization
    Shipments[Module.Shipments] --> Infra
    Shipments --> Catalog
    Shipments --> Core
    Shipments --> Inventory
    Shipments --> Orders
    Shipments --> Shipping[Module.Shipping]
    Shipping --> Infra
    Shipping --> Catalog
    Shipping --> Core
    ShippingFree[Module.ShippingFree] --> Infra
    ShippingFree --> Core
    ShippingFree --> ShippingPrices
    ShippingFree --> Shipping
    ShippingPrices --> Infra
    ShippingPrices --> Catalog
    ShippingPrices --> Core
    ShippingPrices --> Shipping
    ShippingTableRate[Module.ShippingTableRate] --> Infra
    ShippingTableRate --> Core
    ShippingTableRate --> ShippingPrices
    ShippingTableRate --> Shipping
    ShoppingCart --> Infra
    ShoppingCart --> Catalog
    ShoppingCart --> Core
    ShoppingCart --> Pricing
    StorageAmazonS3[Module.StorageAmazonS3] --> Core
    StorageAmazonS3 --> Infra
    StorageAzureBlob[Module.StorageAzureBlob] --> Core
    StorageAzureBlob --> Infra
    StorageLocal[Module.StorageLocal] --> Core
    StorageLocal --> Infra
    Vendors[Module.Vendors] --> Infra
    Vendors --> Core
    WishList[Module.WishList] --> Infra
    WishList --> Catalog
    WishList --> Core
```

## Topological order (suggested refactor order for Phase 2)

1. `Infrastructure` (base)
2. `Module.Core`
3. `Module.Localization`, `Module.ActivityLog`, `Module.Cms`, `Module.Tax`, `Module.Contacts`, `Module.Vendors`, `Module.SampleData`, `Module.News`, `Module.SignalR`, `Module.HangfireJobs`, `Module.DinkToPdf`, `Module.StorageLocal`, `Module.StorageAzureBlob`, `Module.StorageAmazonS3`, `Module.EmailSenderSendgrid`, `Module.EmailSenderSmtp`
4. `Module.Catalog` (depends on Tax)
5. `Module.Shipping`, `Module.Pricing`, `Module.Inventory`, `Module.Search`, `Module.Comments`, `Module.ProductComparison`, `Module.ProductRecentlyViewed`, `Module.WishList` (depend on Catalog)
6. `Module.ShippingPrices` (depends on Shipping + Catalog)
7. `Module.ShippingFree`, `Module.ShippingTableRate` (depend on ShippingPrices)
8. `Module.ShoppingCart` (depends on Catalog + Pricing)
9. `Module.Checkouts` (depends on Catalog + ShippingPrices + ShoppingCart)
10. `Module.Orders` (depends on Checkouts + Pricing + ShippingPrices)
11. `Module.Reviews`, `Module.Shipments` (depend on Orders)
12. `Module.Payments` (depends on Orders + Checkouts)
13. `Module.PaymentCoD`, `Module.PaymentMomo`, `Module.PaymentStripe`, `Module.PaymentPaypalExpress`, `Module.PaymentBraintree`, `Module.PaymentCashfree`, `Module.PaymentNganLuong`
14. `Module.Notifications` (depends on Hangfire + SignalR)

> Đây chính xác là thứ tự Phase 2 trong MIGRATION_TODO.md với điều chỉnh nhỏ để match thực tế dependencies (e.g., Tax trước Catalog).

## WebHost dependencies

`SimplCommerce.WebHost.csproj` references:
- SimplCommerce.Infrastructure
- Module.ActivityLog, Catalog, Checkouts, Cms, Comments, Contacts, Core
- Module.DinkToPdf, EmailSenderSmtp
- Module.Inventory, Localization, News, Orders
- Module.PaymentBraintree, PaymentCashfree, PaymentCoD, PaymentMomo, PaymentNganLuong, PaymentPaypalExpress, PaymentStripe, Payments
- Module.Pricing, ProductComparison, ProductRecentlyViewed, Reviews
- Module.SampleData, Search, Shipments
- Module.ShippingFree, ShippingPrices, ShippingTableRate, Shipping
- Module.ShoppingCart, StorageLocal, Tax, Vendors, WishList

> **Thiếu** trong WebHost refs: `Module.EmailSenderSendgrid`, `Module.HangfireJobs`, `Module.Notifications`, `Module.SignalR`, `Module.StorageAmazonS3`, `Module.StorageAzureBlob` — những module này có cơ chế runtime loading qua `modules.json` trong WebHost thay vì ProjectReference. Phase 2 (P2-39) sẽ chuyển tất cả về static project reference + extension method.

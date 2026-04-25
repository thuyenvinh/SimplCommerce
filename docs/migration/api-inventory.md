# API & CONTROLLER INVENTORY

> Nguồn: toàn bộ file `.cs` dưới `src/` có class kế thừa `Controller` / `ControllerBase` tại commit HEAD (2026-04-17).
>
> Tổng số controller: **104**
> - Storefront API: 28 (26.9%)
> - Admin API: 68 (65.4%)
> - MVC View: 8 (7.7%)

## Bảng chi tiết

| Module | Controller | File | Route | HTTP methods | Authorize | Classification |
|---|---|---|---|---|---|---|
| WishList | WishListController | src/Modules/SimplCommerce.Module.WishList/Areas/WishList/Controllers/WishListController.cs | — | GET/POST/DELETE/PATCH | [Authorize] (một số action) | Storefront API |
| Vendors | VendorApiController | src/Modules/SimplCommerce.Module.Vendors/Areas/Vendors/Controllers/VendorApiController.cs | api/vendors | GET/POST/PUT/DELETE | admin | Admin API |
| Tax | TaxClassApiController | src/Modules/SimplCommerce.Module.Tax/Areas/Tax/Controllers/TaxClassApiController.cs | api/tax-classes | GET/POST/PUT/DELETE | admin | Admin API |
| Tax | TaxRateApiController | src/Modules/SimplCommerce.Module.Tax/Areas/Tax/Controllers/TaxRateApiController.cs | api/tax-rates | GET/POST/PUT/DELETE | admin | Admin API |
| ShoppingCart | CartController | src/Modules/SimplCommerce.Module.ShoppingCart/Areas/ShoppingCart/Controllers/CartController.cs | — | GET/POST | none | Storefront API |
| ShoppingCart | CartApiController | src/Modules/SimplCommerce.Module.ShoppingCart/Areas/ShoppingCart/Controllers/CartApiController.cs | — | — | admin | Admin API |
| ShippingTableRate | PriceAndDestinationApiController | src/Modules/SimplCommerce.Module.ShippingTableRate/Areas/ShippingTableRate/Controllers/PriceAndDestinationApiController.cs | api/shippings/table-rate/price-destinations | GET/POST/PUT/DELETE | admin | Admin API |
| Shipping | ShippingProviderApiController | src/Modules/SimplCommerce.Module.Shipping/Areas/Shipping/Controllers/ShippingProviderApiController.cs | api/shipping-providers | GET/POST | admin | Admin API |
| Shipments | ShipmentApiController | src/Modules/SimplCommerce.Module.Shipments/Areas/Shipments/Controllers/ShipmentApiController.cs | api/shipments | GET/POST | admin,vendor | Admin API |
| Search | SearchApiController | src/Modules/SimplCommerce.Module.Search/Areas/Search/Controllers/SearchApiController.cs | api/search | GET | admin | Admin API |
| Search | SearchController | src/Modules/SimplCommerce.Module.Search/Areas/Search/Controllers/SearchController.cs | — | GET | none | Storefront API |
| SampleData | SampleDataController | src/Modules/SimplCommerce.Module.SampleData/Areas/SampleData/Controllers/SampleDataController.cs | — | GET/POST | none | MVC View |
| Reviews | ReviewController | src/Modules/SimplCommerce.Module.Reviews/Areas/Reviews/Controllers/ReviewController.cs | — | POST/GET | none | Storefront API |
| Reviews | ReplyApiController | src/Modules/SimplCommerce.Module.Reviews/Areas/Reviews/Controllers/ReplyApiController.cs | api/review-replies | GET/POST | admin | Admin API |
| Reviews | ReplyController | src/Modules/SimplCommerce.Module.Reviews/Areas/Reviews/Controllers/ReplyController.cs | — | POST | [Authorize] | Storefront API |
| Reviews | ReviewApiController | src/Modules/SimplCommerce.Module.Reviews/Areas/Reviews/Controllers/ReviewApiController.cs | api/reviews | GET/POST | admin | Admin API |
| ProductRecentlyViewed | RecentlyViewedWidgetApiController | src/Modules/SimplCommerce.Module.ProductRecentlyViewed/Areas/ProductRecentlyViewed/Controllers/RecentlyViewedWidgetController.cs | api/recently-viewed-widgets | GET/POST/PUT | admin | Admin API |
| ProductComparison | ComparingProductController | src/Modules/SimplCommerce.Module.ProductComparison/Areas/ProductComparison/Controllers/ComparingProductController.cs | — | POST/DELETE/GET | none | Storefront API |
| Pricing | CartRuleApiController | src/Modules/SimplCommerce.Module.Pricing/Areas/Pricing/Controllers/CartRuleApiController.cs | api/cart-rules | POST/GET/PUT/DELETE | admin | Admin API |
| Pricing | CartRuleUsageApiController | src/Modules/SimplCommerce.Module.Pricing/Areas/Pricing/Controllers/CartRuleUsageApiController.cs | api/cart-rule-usages | POST | admin | Admin API |
| Payments | CheckoutController | src/Modules/SimplCommerce.Module.Payments/Areas/Payments/Controllers/CheckoutController.cs | checkout | GET | [Authorize] | MVC View |
| Payments | PaymentApiController | src/Modules/SimplCommerce.Module.Payments/Areas/Payments/Controllers/PaymentApiController.cs | api/payments | GET | admin | Admin API |
| Payments | PaymentProviderApiController | src/Modules/SimplCommerce.Module.Payments/Areas/Payments/Controllers/PaymentProviderApiController.cs | api/payments-providers | GET/POST | admin | Admin API |
| PaymentStripe | StripeApiController | src/Modules/SimplCommerce.Module.PaymentStripe/Areas/PaymentStripe/Controllers/StripeApiController.cs | api/stripe | GET/PUT | admin | Admin API |
| PaymentStripe | StripeController | src/Modules/SimplCommerce.Module.PaymentStripe/Areas/PaymentStripe/Controllers/StripeController.cs | — | GET | none | Storefront API |
| PaymentPaypalExpress | PaypalExpressApiController | src/Modules/SimplCommerce.Module.PaymentPaypalExpress/Areas/PaymentPaypalExpress/Controllers/PaypalExpressApiController.cs | api/paypal-express | GET/PUT | admin | Admin API |
| PaymentPaypalExpress | PaypalExpressController | src/Modules/SimplCommerce.Module.PaymentPaypalExpress/Areas/PaymentPaypalExpress/Controllers/PaypalExpressController.cs | — | POST | none | Storefront API |
| PaymentNganLuong | NganLuongController | src/Modules/SimplCommerce.Module.PaymentNganLuong/Areas/PaymentNganLuong/Controllers/NganLuongController.cs | — | GET/POST | [Authorize] | Storefront API |
| PaymentNganLuong | NganLuongApiController | src/Modules/SimplCommerce.Module.PaymentNganLuong/Areas/PaymentNganLuong/Controllers/NganLuongApiController.cs | api/ngan-luong | GET/PUT | admin | Admin API |
| PaymentMomo | MomoPaymentApiController | src/Modules/SimplCommerce.Module.PaymentMomo/Areas/PaymentMomo/Controllers/MomoPaymentApiController.cs | api/momo | GET/PUT | admin | Admin API |
| PaymentMomo | MomoPaymentController | src/Modules/SimplCommerce.Module.PaymentMomo/Areas/PaymentMomo/Controllers/MomoPaymentController.cs | — | POST/GET | [Authorize] | Storefront API |
| PaymentCoD | CoDController | src/Modules/SimplCommerce.Module.PaymentCoD/Areas/PaymentCoD/Controllers/CoDController.cs | — | POST | [Authorize] | Storefront API |
| PaymentCoD | CoDApiController | src/Modules/SimplCommerce.Module.PaymentCoD/Areas/PaymentCoD/Controllers/CoDApiController.cs | api/cod | GET/PUT | admin | Admin API |
| PaymentCashfree | CashfreeController | src/Modules/SimplCommerce.Module.PaymentCashfree/Areas/PaymentCashfree/Controllers/CashfreeController.cs | — | POST | none | Storefront API |
| PaymentCashfree | CashfreeApiController | src/Modules/SimplCommerce.Module.PaymentCashfree/Areas/PaymentCashfree/Controllers/CashfreeApiController.cs | api/cashfree | GET/PUT | admin | Admin API |
| PaymentBraintree | BraintreeController | src/Modules/SimplCommerce.Module.PaymentBraintree/Areas/PaymentBraintree/Controllers/BraintreeController.cs | — | POST | none | Storefront API |
| PaymentBraintree | BraintreeApiController | src/Modules/SimplCommerce.Module.PaymentBraintree/Areas/PaymentBraintree/Controllers/BraintreeApiController.cs | api/braintree | GET/PUT | admin | Admin API |
| Orders | OrderApiController | src/Modules/SimplCommerce.Module.Orders/Areas/Orders/Controllers/OrderApiController.cs | api/orders | GET/POST | admin,vendor | Admin API |
| Orders | OrderController | src/Modules/SimplCommerce.Module.Orders/Areas/Orders/Controllers/OrderController.cs | — | GET | [Authorize] | MVC View |
| Orders | OrderHistoryApiController | src/Modules/SimplCommerce.Module.Orders/Areas/Orders/Controllers/OrderHistoryApiController.cs | — | GET | admin,vendor | Admin API |
| Orders | CheckoutApiController | src/Modules/SimplCommerce.Module.Orders/Areas/Orders/Controllers/CheckoutApiController.cs | — | POST/GET | admin | Admin API |
| Orders | InvoiceApiController | src/Modules/SimplCommerce.Module.Orders/Areas/Orders/Controllers/InvoiceApiController.cs | api/invoices | GET | admin,vendor | Admin API |
| Notifications | NotificationsController | src/Modules/SimplCommerce.Module.Notifications/Areas/Notifications/Controllers/NotificationsController.cs | — | GET/POST | none | MVC View |
| News | NewsItemApiController | src/Modules/SimplCommerce.Module.News/Areas/News/Controllers/NewsItemApiController.cs | api/news-items | POST/GET/PUT/DELETE | admin | Admin API |
| News | NewsItemController | src/Modules/SimplCommerce.Module.News/Areas/News/Controllers/NewsItemController.cs | — | GET | none | Storefront API |
| News | NewsCategoryApiController | src/Modules/SimplCommerce.Module.News/Areas/News/Controllers/NewsCategoryApiController.cs | api/news-categories | GET/POST/PUT/DELETE | admin | Admin API |
| News | NewsCategoryController | src/Modules/SimplCommerce.Module.News/Areas/News/Controllers/NewsCategoryController.cs | — | GET | none | Storefront API |
| Localization | LocalizationController | src/Modules/SimplCommerce.Module.Localization/Areas/Localization/Controllers/LocalizationController.cs | — | POST | none | MVC View |
| Localization | LocalizationApiController | src/Modules/SimplCommerce.Module.Localization/Areas/Localization/Controllers/LocalizationApiController.cs | api/localization | GET/POST | mixed | Admin API |
| Inventory | WarehouseProductApiController | src/Modules/SimplCommerce.Module.Inventory/Areas/Inventory/Controllers/WarehouseProductApiController.cs | api/warehouses | POST | admin,vendor | Admin API |
| Inventory | StockApiController | src/Modules/SimplCommerce.Module.Inventory/Areas/Inventory/Controllers/StockApiController.cs | api/stocks | POST/PUT/GET | admin,vendor | Admin API |
| Inventory | WarehouseApiController | src/Modules/SimplCommerce.Module.Inventory/Areas/Inventory/Controllers/WarehouseApiController.cs | api/warehouses | GET/POST/PUT/DELETE | admin,vendor | Admin API |
| Core | WidgetApiController | src/Modules/SimplCommerce.Module.Core/Areas/Core/Controllers/WidgetApiController.cs | api/widgets | GET | admin | Admin API |
| Core | WidgetInstanceApiController | src/Modules/SimplCommerce.Module.Core/Areas/Core/Controllers/WidgetInstanceApiController.cs | api/widget-instances | GET/DELETE | admin | Admin API |
| Core | WidgetZoneApiController | src/Modules/SimplCommerce.Module.Core/Areas/Core/Controllers/WidgetZoneApiController.cs | api/widget-zones | GET | admin | Admin API |
| Core | StateOrProvinceApiController | src/Modules/SimplCommerce.Module.Core/Areas/Core/Controllers/StateOrProvinceApiController.cs | api/states-provinces | GET/POST/PUT/DELETE | mixed | Admin API |
| Core | ThemeApiController | src/Modules/SimplCommerce.Module.Core/Areas/Core/Controllers/ThemeApiController.cs | api/themes | GET/PUT/DELETE/POST | admin | Admin API |
| Core | UserAddressController | src/Modules/SimplCommerce.Module.Core/Areas/Core/Controllers/UserAddressController.cs | — | GET/POST | [Authorize] | MVC View |
| Core | UserApiController | src/Modules/SimplCommerce.Module.Core/Areas/Core/Controllers/UserApiController.cs | api/users | GET/POST/PUT/DELETE | admin | Admin API |
| Core | HomeAdminController | src/Modules/SimplCommerce.Module.Core/Areas/Core/Controllers/HomeAdminController.cs | — | GET | admin,vendor | MVC View |
| Core | HomeController | src/Modules/SimplCommerce.Module.Core/Areas/Core/Controllers/HomeController.cs | — | GET | none | MVC View |
| Core | ManageController | src/Modules/SimplCommerce.Module.Core/Areas/Core/Controllers/ManageController.cs | — | GET/POST | [Authorize] | MVC View |
| Core | RoleApiController | src/Modules/SimplCommerce.Module.Core/Areas/Core/Controllers/RoleApiController.cs | api/roles | GET | admin | Admin API |
| Core | DistrictApiController | src/Modules/SimplCommerce.Module.Core/Areas/Core/Controllers/DistrictApiController.cs | api/districts | GET | none | Admin API |
| Core | EntityApiController | src/Modules/SimplCommerce.Module.Core/Areas/Core/Controllers/EntityApiController.cs | api/entities | GET | admin | Admin API |
| Core | EntityTypeApiController | src/Modules/SimplCommerce.Module.Core/Areas/Core/Controllers/EntityTypeApiController.cs | api/entity-types | GET | admin | Admin API |
| Core | CustomerGroupApiController | src/Modules/SimplCommerce.Module.Core/Areas/Core/Controllers/CustomerGroupApiController.cs | api/customergroups | POST/GET/PUT/DELETE | admin | Admin API |
| Core | DashboardController | src/Modules/SimplCommerce.Module.Core/Areas/Core/Controllers/DashboardController.cs | — | GET | admin,vendor | MVC View |
| Core | AppSettingApiController | src/Modules/SimplCommerce.Module.Core/Areas/Core/Controllers/AppSettingApiController.cs | api/appsettings | GET/PUT | admin | Admin API |
| Core | CommonApiController | src/Modules/SimplCommerce.Module.Core/Areas/Core/Controllers/CommonApiController.cs | api/common | POST | admin | Admin API |
| Core | CountryApiController | src/Modules/SimplCommerce.Module.Core/Areas/Core/Controllers/CountryApiController.cs | api/countries | GET/POST/PUT/DELETE | mixed | Admin API |
| Core | AccountController | src/Modules/SimplCommerce.Module.Core/Areas/Core/Controllers/AccountController.cs | — | GET/POST | mixed | MVC View |
| Contacts | ContactController | src/Modules/SimplCommerce.Module.Contacts/Areas/Contacts/Controllers/ContactController.cs | — | GET/POST | none | Storefront API |
| Contacts | ContactApiController | src/Modules/SimplCommerce.Module.Contacts/Areas/Contacts/Controllers/ContactApiController.cs | api/contacts | POST/GET/DELETE | admin | Admin API |
| Contacts | ContactAreaApiController | src/Modules/SimplCommerce.Module.Contacts/Areas/Contacts/Controllers/ContactAreaApiController.cs | api/contact-area | GET/POST/PUT/DELETE | admin | Admin API |
| Contacts | ContactAreaTranslationApiController | src/Modules/SimplCommerce.Module.Contacts/Areas/Contacts/Controllers/ContactAreaTranslationApiController.cs | api/contact-area-translations | GET/PUT | admin | Admin API |
| Comments | CommentController | src/Modules/SimplCommerce.Module.Comments/Areas/Comments/Controllers/CommentController.cs | comments | GET/POST | mixed | Storefront API |
| Comments | CommentApiController | src/Modules/SimplCommerce.Module.Comments/Areas/Comments/Controllers/CommentApiController.cs | api/comments | GET/POST | admin | Admin API |
| Cms | PageController | src/Modules/SimplCommerce.Module.Cms/Areas/Cms/Controllers/PageController.cs | — | GET | none | Storefront API |
| Cms | PageTranslationApiController | src/Modules/SimplCommerce.Module.Cms/Areas/Cms/Controllers/PageTranslationApiController.cs | api/page-translations | GET/PUT | admin | Admin API |
| Cms | SpaceBarWidgetApiController | src/Modules/SimplCommerce.Module.Cms/Areas/Cms/Controllers/SpaceBarWidgetApiContorller.cs | api/spacebar-widgets | GET/POST/PUT | admin | Admin API |
| Cms | HtmlWidgetApiController | src/Modules/SimplCommerce.Module.Cms/Areas/Cms/Controllers/HtmlWidgetApiController.cs | api/html-widgets | GET/POST/PUT | admin | Admin API |
| Cms | MenuApiController | src/Modules/SimplCommerce.Module.Cms/Areas/Cms/Controllers/MenuApiController.cs | api/menus | GET/POST/PUT/DELETE | admin | Admin API |
| Cms | PageApiController | src/Modules/SimplCommerce.Module.Cms/Areas/Cms/Controllers/PageApiController.cs | api/pages | POST/GET/PUT/DELETE | admin | Admin API |
| Cms | CarouselWidgetApiController | src/Modules/SimplCommerce.Module.Cms/Areas/Cms/Controllers/CarouselWidgetApiController.cs | api/carousel-widgets | GET/POST/PUT | admin | Admin API |
| Checkouts | CheckoutController | src/Modules/SimplCommerce.Module.Checkouts/Areas/Checkouts/Controllers/CheckoutController.cs | — | GET/POST | [Authorize] | MVC View |
| Catalog | ProductWidgetApiController | src/Modules/SimplCommerce.Module.Catalog/Areas/Catalog/Controllers/ProductWidgetApiController.cs | api/product-widgets | GET/POST/PUT | admin | Admin API |
| Catalog | SimpleProductWidgetApiController | src/Modules/SimplCommerce.Module.Catalog/Areas/Catalog/Controllers/SimpleProductWidgetApiController.cs | api/simple-product-widgets | GET/POST/PUT | admin | Admin API |
| Catalog | ProductOptionApiController | src/Modules/SimplCommerce.Module.Catalog/Areas/Catalog/Controllers/ProductOptionApiController.cs | api/product-options | GET/POST/PUT/DELETE | admin | Admin API |
| Catalog | ProductPriceApiController | src/Modules/SimplCommerce.Module.Catalog/Areas/Catalog/Controllers/ProductPriceApiController.cs | api/product-prices | GET/PUT | admin,vendor | Admin API |
| Catalog | ProductTemplateApiController | src/Modules/SimplCommerce.Module.Catalog/Areas/Catalog/Controllers/ProductTemplateApiController.cs | api/product-templates | GET/POST/PUT/DELETE | admin | Admin API |
| Catalog | ProductTranslationApiController | src/Modules/SimplCommerce.Module.Catalog/Areas/Catalog/Controllers/ProductTranslationApiController.cs | api/product-translations | GET/PUT | admin | Admin API |
| Catalog | ProductAttributeApiController | src/Modules/SimplCommerce.Module.Catalog/Areas/Catalog/Controllers/ProductAttributeApiController.cs | api/product-attributes | GET/POST/PUT/DELETE | admin | Admin API |
| Catalog | ProductAttributeGroupApiController | src/Modules/SimplCommerce.Module.Catalog/Areas/Catalog/Controllers/ProductAttributeGroupApiController.cs | api/product-attribute-groups | GET/POST/PUT/DELETE | admin | Admin API |
| Catalog | ProductCloneApiController | src/Modules/SimplCommerce.Module.Catalog/Areas/Catalog/Controllers/ProductCloneApiController.cs | api/product-clones | POST | admin,vendor | Admin API |
| Catalog | ProductController | src/Modules/SimplCommerce.Module.Catalog/Areas/Catalog/Controllers/ProductController.cs | — | GET | none | Storefront API |
| Catalog | CategoryTranslationApiController | src/Modules/SimplCommerce.Module.Catalog/Areas/Catalog/Controllers/CategoryTranslationApiController.cs | api/category-translations | GET/PUT | admin | Admin API |
| Catalog | CategoryWidgetApiController | src/Modules/SimplCommerce.Module.Catalog/Areas/Catalog/Controllers/CategoryWidgetApiController.cs | api/category-widgets | GET/POST/PUT | admin | Admin API |
| Catalog | ProductApiController | src/Modules/SimplCommerce.Module.Catalog/Areas/Catalog/Controllers/ProductApiController.cs | api/products | POST/GET/PUT/DELETE | mixed | Admin API |
| Catalog | BrandController | src/Modules/SimplCommerce.Module.Catalog/Areas/Catalog/Controllers/BrandController.cs | — | GET | none | Storefront API |
| Catalog | CategoryApiController | src/Modules/SimplCommerce.Module.Catalog/Areas/Catalog/Controllers/CategoryApiController.cs | api/categories | GET/POST/PUT/DELETE | mixed | Admin API |
| Catalog | CategoryController | src/Modules/SimplCommerce.Module.Catalog/Areas/Catalog/Controllers/CategoryController.cs | — | GET | none | Storefront API |
| Catalog | BrandApiController | src/Modules/SimplCommerce.Module.Catalog/Areas/Catalog/Controllers/BrandApiController.cs | api/brands | GET/POST/PUT/DELETE | mixed | Admin API |
| ActivityLog | MostViewedEntityController | src/Modules/SimplCommerce.Module.ActivityLog/Areas/ActivityLog/Controllers/MostViewedEntityController.cs | — | GET | none | Storefront API |

## Ghi chú migration (Phase 3)

- Mỗi Admin API controller → chuyển thành endpoint group Minimal API trong `Endpoints/<X>Endpoints.cs` của module gốc. Route prefix giữ nguyên (e.g. `api/products`).
- Storefront API controllers → chuyển sang `Module.StorefrontApi` endpoint groups (đã có module tên StorefrontApi theo prompt, nhưng không tồn tại — sẽ gộp vào endpoint group của module gốc, expose qua `MapStorefrontEndpoints()`).
- MVC View controllers (AccountController, HomeController, Dashboard, Checkout views, v.v.) → không chuyển thành endpoint — sẽ bị thay thế hoàn toàn bởi Blazor pages ở Phase 4/5.
- `[Authorize(Roles="admin, vendor")]` → cần policy tách biệt: `AdminOrVendor`, `AdminOnly`, `VendorOnly`.
- `[Authorize]` không role → "customer-authenticated" policy.
- Route của `ReviewController`, `ReplyController`, `ProductController` etc. không có class-level Route attr — dùng convention `[Area]/[Controller]/[Action]`; khi migrate sang Minimal API cần tự khai báo route rõ ràng.

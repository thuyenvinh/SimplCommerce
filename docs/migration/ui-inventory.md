# UI INVENTORY — AngularJS Admin + Razor Views

> Nguồn: toàn bộ `.html` dưới `wwwroot/admin/` và `.cshtml` dưới `Areas/*/Views/` + `Views/` tại commit HEAD (2026-04-17).

## Tổng quan

- **AngularJS admin templates (.html):** 98
- **Razor Views (.cshtml):** 181 (137 trong modules + 44 trong WebHost)
- **Modules có UI admin (AngularJS):** 27
- **Modules có Razor Views:** 23 + WebHost

## PART A — AngularJS admin templates (phải viết lại → Blazor)

### Module: ActivityLog — 1 file
- `wwwroot/admin/most-viewed-products.directive.html`

### Module: Catalog — 23 files
- `wwwroot/admin/brand/brand-form.html`
- `wwwroot/admin/brand/brand-list.html`
- `wwwroot/admin/category-widget/category-widget-form.html`
- `wwwroot/admin/category/category-form.html`
- `wwwroot/admin/category/category-list.html`
- `wwwroot/admin/category/category-translation-form.html`
- `wwwroot/admin/product-attribute-group/product-attribute-group-form.html`
- `wwwroot/admin/product-attribute-group/product-attribute-group-list.html`
- `wwwroot/admin/product-attribute/product-attribute-form.html`
- `wwwroot/admin/product-attribute/product-attribute-list.html`
- `wwwroot/admin/product-clone/product-clone-form.html`
- `wwwroot/admin/product-option/product-option-form.html`
- `wwwroot/admin/product-option/product-option-list.html`
- `wwwroot/admin/product-price/product-price-form.html`
- `wwwroot/admin/product-template/product-template-form.html`
- `wwwroot/admin/product-template/product-template-list.html`
- `wwwroot/admin/product-widget/product-widget-form.html`
- `wwwroot/admin/product/product-form.html`
- `wwwroot/admin/product/product-list.html`
- `wwwroot/admin/product/product-option-display-directive.html`
- `wwwroot/admin/product/product-selection-directive.html`
- `wwwroot/admin/product/product-translation-form.html`
- `wwwroot/admin/simple-product-widget/simple-product-widget-form.html`

### Module: Cms — 9 files
- `wwwroot/admin/carousel-widget/carousel-widget-form.html`
- `wwwroot/admin/html-widget/html-widget-form.html`
- `wwwroot/admin/menu/menu-form-create.html`
- `wwwroot/admin/menu/menu-form.html`
- `wwwroot/admin/menu/menu-list.html`
- `wwwroot/admin/page/page-form.html`
- `wwwroot/admin/page/page-list.html`
- `wwwroot/admin/page/page-translation-form.html`
- `wwwroot/admin/spacebar-widget/spacebar-widget-form.html`

### Module: Comments — 1 file
- `wwwroot/admin/comment/comment-list.html`

### Module: Contacts — 5 files
- `wwwroot/admin/contact-area/contact-area-form.html`
- `wwwroot/admin/contact-area/contact-area-list.html`
- `wwwroot/admin/contact-area/contact-area-translation-form.html`
- `wwwroot/admin/contacts/contact-list.html`
- `wwwroot/admin/contacts/contact.html`

### Module: Core — 14 files
- `wwwroot/admin/common/st-date-range.html`
- `wwwroot/admin/configuration/configuration.html`
- `wwwroot/admin/countries/country-form.html`
- `wwwroot/admin/countries/country-list.html`
- `wwwroot/admin/customergroups/customergroup-form.html`
- `wwwroot/admin/customergroups/customergroup-list.html`
- `wwwroot/admin/stateprovince/state-province-form.html`
- `wwwroot/admin/stateprovince/state-province-list.html`
- `wwwroot/admin/themes/online-theme-list.html`
- `wwwroot/admin/themes/theme-details.html`
- `wwwroot/admin/themes/theme-list.html`
- `wwwroot/admin/user/user-form.html`
- `wwwroot/admin/user/user-list.html`
- `wwwroot/admin/widget/widget-instance-list.html`

### Module: Inventory — 5 files
- `wwwroot/admin/stock/stock-form.html`
- `wwwroot/admin/stock/stock-history.html`
- `wwwroot/admin/warehouse/manage-products-form.html`
- `wwwroot/admin/warehouse/warehouse-form.html`
- `wwwroot/admin/warehouse/warehouse-list.html`

### Module: Localization — 1 file
- `wwwroot/admin/localization/localization-form.html`

### Module: News — 4 files
- `wwwroot/admin/news-categories/news-category-form.html`
- `wwwroot/admin/news-categories/news-category-list.html`
- `wwwroot/admin/news-items/news-item-form.html`
- `wwwroot/admin/news-items/news-item-list.html`

### Module: Orders — 4 files
- `wwwroot/admin/order/order-create.html`
- `wwwroot/admin/order/order-detail.html`
- `wwwroot/admin/order/order-list.html`
- `wwwroot/admin/order/order-widget.directive.html`

### Module: PaymentBraintree — 1 file
- `wwwroot/admin/braintree/braintree-config-form.html`

### Module: PaymentCashfree — 1 file
- `wwwroot/admin/cashfree/cashfree-config-form.html`

### Module: PaymentCoD — 1 file
- `wwwroot/admin/config/config-form.html`

### Module: PaymentMomo — 1 file
- `wwwroot/admin/momo/momo-config-form.html`

### Module: PaymentNganLuong — 1 file
- `wwwroot/admin/ngan-luong/ngan-luong-config-form.html`

### Module: PaymentPaypalExpress — 1 file
- `wwwroot/admin/config/config-form.html`

### Module: PaymentStripe — 1 file
- `wwwroot/admin/stripe/stripe-config-form.html`

### Module: Payments — 2 files
- `wwwroot/admin/payment/payment-list-by-order.directive.html`
- `wwwroot/admin/provider/payment-provider-list.html`

### Module: Pricing — 3 files
- `wwwroot/admin/cart-rule-usage/cart-rule-usage-list.html`
- `wwwroot/admin/cart-rule/cart-rule-form.html`
- `wwwroot/admin/cart-rule/cart-rule-list.html`

### Module: ProductRecentlyViewed — 1 file
- `wwwroot/admin/recently-viewed-widget/recently-viewed-widget-form.html`

### Module: Reviews — 4 files
- `wwwroot/admin/review/review-list.html`
- `wwwroot/admin/review/review-reply-list.html`
- `wwwroot/admin/review/review-reply-widget.directive.html`
- `wwwroot/admin/review/review-widget.directive.html`

### Module: Search — 1 file
- `wwwroot/admin/most-search-keywords.directive.html`

### Module: Shipments — 4 files
- `wwwroot/admin/shipment/shipment-details.html`
- `wwwroot/admin/shipment/shipment-form.html`
- `wwwroot/admin/shipment/shipment-list-by-order.directive.html`
- `wwwroot/admin/shipment/shipment-list.html`

### Module: Shipping — 1 file
- `wwwroot/admin/provider/shipping-provider-list.html`

### Module: ShippingTableRate — 1 file
- `wwwroot/admin/tablerate/shipping-tablerate-form.html`

### Module: Tax — 5 files
- `wwwroot/admin/tax-class/tax-class-form.html`
- `wwwroot/admin/tax-class/tax-class-list.html`
- `wwwroot/admin/tax-rate/tax-rate-form.html`
- `wwwroot/admin/tax-rate/tax-rate-import.html`
- `wwwroot/admin/tax-rate/tax-rate-list.html`

### Module: Vendors — 2 files
- `wwwroot/admin/vendors/vendor-form.html`
- `wwwroot/admin/vendors/vendor-list.html`

## PART B — Razor Views (.cshtml)

### Module: Catalog — 12 files
- Areas/Catalog/Views/Brand/BrandDetail.cshtml
- Areas/Catalog/Views/Category/CategoryDetail.cshtml
- Areas/Catalog/Views/Product/ProductDetail.cshtml
- Areas/Catalog/Views/Product/ProductOverview.cshtml
- Areas/Catalog/Views/Shared/Components/CategoryBreadcrumb/Default.cshtml
- Areas/Catalog/Views/Shared/Components/CategoryMenu/Default.cshtml
- Areas/Catalog/Views/Shared/Components/CategoryWidget/Default.cshtml
- Areas/Catalog/Views/Shared/Components/ProductWidget/Default.cshtml
- Areas/Catalog/Views/Shared/Components/SimpleProductWidget/Default.cshtml
- Areas/Catalog/Views/Shared/_ProductThumbnail.cshtml
- Areas/Catalog/Views/_ViewImports.cshtml
- Areas/Catalog/Views/_ViewStart.cshtml

### Module: Checkouts — 5 files
- Areas/Checkouts/Views/Checkout/Error.cshtml
- Areas/Checkouts/Views/Checkout/Shipping.cshtml
- Areas/Checkouts/Views/Checkout/Success.cshtml
- Areas/Checkouts/Views/_ViewImports.cshtml
- Areas/Checkouts/Views/_ViewStart.cshtml

### Module: Cms — 6 files
- Areas/Cms/Views/Page/PageDetail.cshtml
- Areas/Cms/Views/Shared/Components/CarouselWidget/Default.cshtml
- Areas/Cms/Views/Shared/Components/Menu/Default.cshtml
- Areas/Cms/Views/Shared/Components/SpaceBarWidget/Default.cshtml
- Areas/Cms/Views/_ViewImports.cshtml
- Areas/Cms/Views/_ViewStart.cshtml

### Module: Comments — 3 files
### Module: Contacts — 4 files
### Module: Core — 31 files
(Account views: Login, Register, ForgotPassword, ResetPassword, ConfirmEmail, ExternalLogin*, Lockout, SendCode, VerifyCode, AccessDenied; Manage: ChangePassword, Index, ManageLogins, SetPassword, UserInfo, UserSettings; Dashboard HomeTemplate; Home/Index; HomeAdmin/Index; UserAddress Create/Edit/List/_AddressForm; EmailTemplate/AccountRegisteredConfirmationEmailModel; Shared/_WidgetInstances; Shared/Components/DefaultShippingAddress)

### Module: Inventory — 1 file (EmailTemplate)
### Module: News — 6 files
### Module: Notifications — 4 files
### Module: Orders — 7 files (OrderDetails, OrderHistoryList, OrderEmailToCustomer, InvoicePdf, OrderSummary component)
### Module: PaymentBraintree — 3 files
### Module: PaymentCashfree — 3 files
### Module: PaymentCoD — 3 files
### Module: PaymentMomo — 3 files
### Module: PaymentNganLuong — 4 files
### Module: PaymentPaypalExpress — 3 files
### Module: PaymentStripe — 3 files
### Module: Payments — 3 files (checkout Payment.cshtml)
### Module: ProductComparison — 4 files
### Module: ProductRecentlyViewed — 3 files
### Module: Reviews — 8 files
### Module: SampleData — 2 files
### Module: Search — 4 files
### Module: ShoppingCart — 5 files (Cart Index, CartBadge component)
### Module: WishList — 7 files (PrivateList, PublicList, Share, AddToWishListResult, UpdateItemResult)

### WebHost — 44 files
Chủ yếu là themes (`Themes/CozaStore/`, `Themes/SampleTheme/`) bao gồm override các component view của Catalog, Cms, Core, ProductRecentlyViewed, Search, ShoppingCart — plus Layout, LoginPartial, SelectLanguagePartial. Các file notable:
- `Views/Shared/_Layout.cshtml`
- `Views/Shared/_LoginPartial.cshtml`
- `Views/Shared/_SelectLanguagePartial.cshtml`
- `Views/Shared/_AccountMenu.cshtml`
- `Views/Shared/_AnalyticsScript.cshtml`
- `Views/Shared/_CookieConsentPartial.cshtml`
- `Views/Shared/_ValidationScriptsPartial.cshtml`
- `Views/Shared/Error.cshtml`, `404.cshtml`
- `Themes/CozaStore/**` + `Themes/SampleTheme/**`

## PART C — Top 10 màn admin nhiều template nhất

| Rank | Screen | File count | Path |
|---|---|---|---|
| 1 | product (Catalog) | 5 | src/Modules/SimplCommerce.Module.Catalog/wwwroot/admin/product/ |
| 2 | shipment (Shipments) | 4 | src/Modules/SimplCommerce.Module.Shipments/wwwroot/admin/shipment/ |
| 3 | review (Reviews) | 4 | src/Modules/SimplCommerce.Module.Reviews/wwwroot/admin/review/ |
| 4 | order (Orders) | 4 | src/Modules/SimplCommerce.Module.Orders/wwwroot/admin/order/ |
| 5 | warehouse (Inventory) | 3 | src/Modules/SimplCommerce.Module.Inventory/wwwroot/admin/warehouse/ |
| 6 | themes (Core) | 3 | src/Modules/SimplCommerce.Module.Core/wwwroot/admin/themes/ |
| 7 | tax-rate (Tax) | 3 | src/Modules/SimplCommerce.Module.Tax/wwwroot/admin/tax-rate/ |
| 8 | page (Cms) | 3 | src/Modules/SimplCommerce.Module.Cms/wwwroot/admin/page/ |
| 9 | menu (Cms) | 3 | src/Modules/SimplCommerce.Module.Cms/wwwroot/admin/menu/ |
| 10 | contact-area (Contacts) | 3 | src/Modules/SimplCommerce.Module.Contacts/wwwroot/admin/contact-area/ |

## Migration notes

- **AngularJS templates (98):** xóa toàn bộ ở Phase 8, viết lại thành Razor Components trong `SimplCommerce.Admin` ở Phase 5. Module.Catalog và Module.Core chiếm ~38% workload.
- **Razor Views MVC:** chia 2 nhóm:
  - **Email template** (`EmailTemplates/*.cshtml` trong Orders, Inventory, Core): giữ riêng, vẫn cần để render email body. Phase 3 sẽ move sang cơ chế Razor rendering dành cho email (RazorLight / RazorEngine / MailKit MimeMessage builder) trong ApiService.
  - **Storefront/Admin view** (phần còn lại): viết lại sang Blazor component/page trong Storefront (Phase 4) và Admin (Phase 5).
- **Themes:** `CozaStore` + `SampleTheme` trong WebHost → Phase 4 dùng MudBlazor theming thay vì MVC theme system, có thể port style CSS.

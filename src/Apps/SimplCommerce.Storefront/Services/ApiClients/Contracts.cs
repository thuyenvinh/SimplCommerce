namespace SimplCommerce.Storefront.Services.ApiClients;

// Shape mirrors the JSON returned by the ApiService minimal endpoints.
// We re-declare the records here (rather than sharing a contracts assembly) so
// the Storefront UI is free to evolve independently once the API stabilises.

public record ProductListItem(
    long Id, string Name, string Slug, decimal Price, decimal? OldPrice,
    string? ThumbnailUrl, bool IsCallForPricing, bool IsAllowToOrder,
    double? RatingAverage, int ReviewsCount);

public record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize);

public record CategoryItem(long Id, string Name, string Slug, long? ParentId, int DisplayOrder);

public record BrandItem(long Id, string Name, string Slug);

public record ProductDetailResponse(
    long Id, string Name, string Slug, string? Description, string? Specification,
    decimal Price, decimal? OldPrice, int StockQuantity, bool IsCallForPricing,
    bool IsAllowToOrder, string? ThumbnailUrl, IReadOnlyList<string> Images,
    IReadOnlyList<CategoryItem> Categories, BrandItem? Brand,
    double? RatingAverage, int ReviewsCount);

public record SearchResult(long Id, string Name, string Slug, decimal Price, decimal? OldPrice, string? ThumbnailUrl);
public record SearchResponse(IReadOnlyList<SearchResult> Items, int TotalCount, string Query);

public record LoginRequest(string Email, string Password);
public record RegisterRequest(string Email, string Password, string FullName);
public record LoginResponse(string AccessToken, DateTimeOffset ExpiresAt, string[] Roles);
public record MeResponse(long UserId, string Email, string FullName, string[] Roles);

public record AddToCartRequest(long ProductId, int Quantity);
public record ApplyCouponRequest(string CouponCode);

public record AddressDto(long Id, string ContactName, string Phone, string AddressLine1, string City, string ZipCode);

public record OrderListItem(long Id, DateTimeOffset CreatedOn, decimal OrderTotal, int OrderStatus);
public record OrderItemDto(long ProductId, string ProductName, int Quantity, decimal ProductPrice);
public record OrderDetailDto(long Id, DateTimeOffset CreatedOn, decimal SubTotal, decimal OrderTotal,
    int OrderStatus, IReadOnlyList<OrderItemDto> Items);

public record WishListItemDto(long Id, long ProductId, int Quantity, string? Description);
public record AddWishListItemRequest(long ProductId, int Quantity = 1, string? Description = null);

public record CmsPageDto(long Id, string Name, string Slug, string? Body);

public record NewsSummary(long Id, string Name, string Slug, string? ShortContent);
public record NewsDetail(long Id, string Name, string Slug, string? ShortContent, string? FullContent);

// --- Marketplace: public vendor pages (Wave 10) ---
public record StorefrontVendorItem(long Id, string Name, string Slug, string? Description);
public record StorefrontVendorDetail(long Id, string Name, string Slug, string? Description);
public record StorefrontVendorProductItem(
    long Id, string Name, string Slug, decimal Price, decimal? OldPrice,
    string? ThumbnailUrl, bool IsCallForPricing, bool IsAllowToOrder,
    double? RatingAverage, int ReviewsCount);
public record StorefrontVendorProductsPage(int Total, int Page, int PageSize, IReadOnlyList<StorefrontVendorProductItem> Items);

// --- Marketplace: become-a-vendor onboarding (Wave 7) ---
public record VendorApplyRequest(string BusinessName, string Slug, string? Description, string? ContactEmail, string? ContactPhone, string? RegistrationNote);
public record MyVendorApplication(long Id, string BusinessName, string Slug, int Status, DateTimeOffset CreatedOn, DateTimeOffset? DecidedOn, string? DecisionNote, long? CreatedVendorId);

// --- Marketplace: buyer ↔ vendor messaging (Wave 15) ---
public record VendorMessageThread(long CounterpartUserId, string? CounterpartName, long VendorId, string VendorName, DateTimeOffset LastMessageOn, string LastSnippet, int UnreadCount);
public record VendorMessage(long Id, int SenderKind, long FromUserId, string? FromName, long? OrderId, string Body, DateTimeOffset CreatedOn, DateTimeOffset? ReadAt);
public record SendVendorMessageRequest(string Body, long? OrderId);

public record CheckoutStartResponse(Guid CheckoutId);
public record CheckoutSummaryDto(
    Guid Id, decimal SubTotal, decimal Discount, decimal? ShippingAmount,
    decimal? TaxAmount, decimal OrderTotal, string? CouponCode,
    IReadOnlyList<CheckoutLineDto> Items);
public record CheckoutLineDto(long ProductId, string ProductName, int Quantity, decimal ProductPrice);
public record CheckoutAddressRequest(
    string ContactName, string Phone, string AddressLine1, string? AddressLine2,
    string CountryId, long StateOrProvinceId, long? DistrictId,
    string? City, string? ZipCode,
    bool UseShippingAddressAsBillingAddress = true,
    string? ShippingMethod = null, string? OrderNote = null);
public record PaymentMethodOption(string Id, string Name);
public record PlaceOrderRequest(string PaymentMethod, decimal PaymentFeeAmount = 0);
public record PlaceOrderResponse(long OrderId);

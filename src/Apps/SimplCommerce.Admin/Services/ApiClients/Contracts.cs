namespace SimplCommerce.Admin.Services.ApiClients;

public record LoginRequest(string Email, string Password);
public record LoginResponse(string AccessToken, DateTimeOffset ExpiresAt, string[] Roles);
public record MeResponse(long UserId, string Email, string FullName, string[] Roles);

// --- Catalog admin ---
public record BrandItem(long Id, string Name, string Slug, bool IsPublished);
public record BrandInput(string Name, string Slug, bool IsPublished);

public record CategoryItem(long Id, string Name, string Slug, long? ParentId, int DisplayOrder, string? Description);
public record CategoryInput(string Name, string Slug, long? ParentId, int DisplayOrder, string? Description);

public record ProductListItem(long Id, string Name, string Slug, string? Sku, decimal Price, decimal? OldPrice,
    int StockQuantity, bool IsPublished, bool IsAllowToOrder, DateTimeOffset CreatedOn);
public record ProductsPage(int Total, int Page, int PageSize, IReadOnlyList<ProductListItem> Items);

public record ProductInput(
    string Name, string Slug, string? Sku,
    decimal Price, decimal? OldPrice,
    string? ShortDescription, string? Description, string? Specification,
    bool IsPublished, bool IsAllowToOrder, bool IsCallForPricing, bool IsFeatured,
    bool StockTrackingIsEnabled, int StockQuantity,
    long? BrandId);

public record ProductEditDto(
    long Id, string Name, string Slug, string? Sku,
    decimal Price, decimal? OldPrice,
    string? ShortDescription, string? Description, string? Specification,
    bool IsPublished, bool IsAllowToOrder, bool IsCallForPricing, bool IsFeatured,
    bool StockTrackingIsEnabled, int StockQuantity,
    long? BrandId, IReadOnlyList<long> CategoryIds);

// --- Orders admin ---
public record AdminCustomerSummary(long Id, string? FullName, string? Email);
public record AdminOrderListItem(long Id, DateTimeOffset CreatedOn, decimal OrderTotal, int OrderStatus, AdminCustomerSummary Customer);
public record AdminOrdersPage(int Total, int Page, int PageSize, IReadOnlyList<AdminOrderListItem> Items);
public record UpdateOrderStatusRequest(int NewStatus);

public record AdminOrderItemDto(long ProductId, string ProductName, int Quantity, decimal ProductPrice, decimal DiscountAmount);
public record AdminOrderAddressDto(string ContactName, string Phone, string AddressLine1, string? AddressLine2, string? City, string? ZipCode);
public record AdminOrderDetail(
    long Id, DateTimeOffset CreatedOn, DateTimeOffset LatestUpdatedOn,
    int OrderStatus, string? PaymentMethod, decimal SubTotal, decimal DiscountAmount,
    decimal TaxAmount, decimal ShippingAmount, decimal OrderTotal,
    long? CustomerId, string? CustomerEmail, string? CustomerFullName,
    AdminOrderAddressDto? ShippingAddress, AdminOrderAddressDto? BillingAddress,
    IReadOnlyList<AdminOrderItemDto> Items);

// --- Core admin (users/roles) ---
public record AdminUserListItem(long Id, string? Email, string? FullName, DateTimeOffset CreatedOn, bool LockoutEnabled);
public record AdminUsersPage(int Total, int Page, int PageSize, IReadOnlyList<AdminUserListItem> Items);

// --- Reviews admin ---
public record AdminReviewItem(long Id, int Rating, string Title, string Comment, string ReviewerName, int Status, DateTimeOffset CreatedOn, string EntityTypeId, long EntityId);
public record AdminReviewsPage(int Total, int Page, int PageSize, IReadOnlyList<AdminReviewItem> Items);
public record ModerationRequest(int Status);

// --- Inventory admin ---
public record AdminWarehouseItem(long Id, string Name, long? VendorId);
public record AdminStockItem(long Id, long ProductId, long WarehouseId, int Quantity);
public record AdminStockHistoryItem(long Id, long ProductId, string ProductName, long WarehouseId, string WarehouseName, long AdjustedQuantity, string? Note, DateTimeOffset CreatedOn);
public record AdminStockAdjustmentInput(long ProductId, long WarehouseId, long AdjustedQuantity, string? Note);

// --- Activity log admin ---
public record AdminActivityItem(long Id, long ActivityTypeId, long UserId, long EntityId, string? EntityTypeId, DateTimeOffset CreatedOn);
public record AdminActivityPage(int Total, int Page, int PageSize, IReadOnlyList<AdminActivityItem> Items);

// --- Vendors admin ---
public record AdminVendorItem(long Id, string Name, string Slug, string? Description, bool IsActive);
public record AdminVendorDetail(long Id, string Name, string Slug, string? Description, string? Email, bool IsActive, DateTimeOffset CreatedOn);
public record AdminVendorInput(string Name, string Slug, string? Description, string? Email, bool IsActive);

// --- Tax admin ---
public record AdminTaxClassItem(long Id, string Name);
public record AdminTaxClassInput(string Name);
public record AdminTaxRateItem(long Id, long TaxClassId, string CountryId, long? StateOrProvinceId, string? ZipCode, decimal Rate);
public record AdminTaxRateInput(long TaxClassId, string CountryId, long? StateOrProvinceId, string? ZipCode, decimal Rate);

// --- Shipping admin ---
public record AdminShippingProviderItem(string Id, string Name, bool IsEnabled);

// --- Payments admin ---
public record AdminPaymentProviderItem(string Id, string Name, bool IsEnabled);
public record AdminPaymentItem(long Id, long OrderId, string? PaymentMethod, decimal PaymentFee, decimal Amount, int Status, DateTimeOffset CreatedOn);
public record AdminPaymentsPage(int Total, int Page, int PageSize, IReadOnlyList<AdminPaymentItem> Items);

// --- News admin ---
public record AdminNewsItemListItem(long Id, string Name, string Slug, bool IsPublished, DateTimeOffset CreatedOn);
public record AdminNewsItemsPage(int Total, int Page, int PageSize, IReadOnlyList<AdminNewsItemListItem> Items);
public record AdminNewsItemDetail(long Id, string Name, string Slug, string? ShortContent, string? FullContent, bool IsPublished, DateTimeOffset CreatedOn);
public record AdminNewsItemInput(string Name, string Slug, string? ShortContent, string? FullContent, bool IsPublished);
public record AdminNewsCategoryItem(long Id, string Name, string Slug, bool IsPublished);
public record AdminNewsCategoryInput(string Name, string Slug, bool IsPublished);

// --- Core: app settings + customer groups ---
public record AdminAppSettingItem(string Id, string Value, string? Module);
public record AdminAppSettingInput(string Value);
public record AdminCustomerGroupItem(long Id, string Name);
public record AdminCustomerGroupInput(string Name);

// --- Cms admin ---
public record AdminCmsPageListItem(long Id, string Name, string Slug, bool IsPublished, DateTimeOffset CreatedOn);
public record AdminCmsPageDetail(long Id, string Name, string Slug, string? Body, bool IsPublished, DateTimeOffset CreatedOn);
public record AdminCmsPageInput(string Name, string Slug, string? Body, bool IsPublished);

// --- Comments admin ---
public record AdminCommentItem(long Id, string CommenterName, string CommenterEmail, string CommentText, int Status, DateTimeOffset CreatedOn, string EntityTypeId, long EntityId);
public record AdminCommentsPage(int Total, int Page, int PageSize, IReadOnlyList<AdminCommentItem> Items);
public record AdminCommentStatusInput(int Status);

// --- Contacts admin ---
public record AdminContactItem(long Id, string Name, string Email, string Message, int Status, long? ContactAreaId, DateTimeOffset CreatedOn);
public record AdminContactsPage(int Total, int Page, int PageSize, IReadOnlyList<AdminContactItem> Items);
public record AdminContactStatusInput(int Status);

// --- Pricing admin ---
public record AdminCartRuleItem(long Id, string Name, DateTimeOffset? StartOn, DateTimeOffset? EndOn, int? UsageLimitPerCoupon, bool IsActive);
public record AdminCartRuleDetail(long Id, string Name, string? Description, bool IsActive,
    DateTimeOffset? StartOn, DateTimeOffset? EndOn, bool IsCouponRequired,
    string RuleToApply, decimal DiscountAmount, decimal? MaxDiscountAmount,
    int? UsageLimitPerCoupon, int? UsageLimitPerCustomer);
public record AdminCartRuleInput(string Name, string? Description, bool IsActive,
    DateTimeOffset? StartOn, DateTimeOffset? EndOn, bool IsCouponRequired,
    string RuleToApply, decimal DiscountAmount, decimal? MaxDiscountAmount,
    int? UsageLimitPerCoupon, int? UsageLimitPerCustomer);
public record AdminCatalogRuleItem(long Id, string Name, DateTimeOffset? StartOn, DateTimeOffset? EndOn, bool IsActive);
public record AdminCouponItem(long Id, string Code, long CartRuleId);
public record AdminCouponInput(long CartRuleId, string Code);

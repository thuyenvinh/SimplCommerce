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

// --- Orders admin ---
public record AdminCustomerSummary(long Id, string? FullName, string? Email);
public record AdminOrderListItem(long Id, DateTimeOffset CreatedOn, decimal OrderTotal, int OrderStatus, AdminCustomerSummary Customer);
public record AdminOrdersPage(int Total, int Page, int PageSize, IReadOnlyList<AdminOrderListItem> Items);
public record UpdateOrderStatusRequest(int NewStatus);

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

// --- Activity log admin ---
public record AdminActivityItem(long Id, long ActivityTypeId, long UserId, long EntityId, string? EntityTypeId, DateTimeOffset CreatedOn);
public record AdminActivityPage(int Total, int Page, int PageSize, IReadOnlyList<AdminActivityItem> Items);

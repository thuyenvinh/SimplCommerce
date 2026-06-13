using System.Net.Http.Json;

namespace SimplCommerce.Admin.Services.ApiClients;

public interface IAuthApi
{
    Task<HttpResponseMessage> LoginAsync(LoginRequest req, CancellationToken ct = default);
}

public sealed class AuthApi(HttpClient http) : IAuthApi
{
    public Task<HttpResponseMessage> LoginAsync(LoginRequest req, CancellationToken ct) =>
        http.PostAsJsonAsync("/api/auth/login", req, ct);
}

public interface IAdminCatalogApi
{
    Task<IReadOnlyList<BrandItem>?> ListBrandsAsync(CancellationToken ct = default);
    Task<HttpResponseMessage> CreateBrandAsync(BrandInput input, CancellationToken ct = default);
    Task<HttpResponseMessage> UpdateBrandAsync(long id, BrandInput input, CancellationToken ct = default);
    Task<HttpResponseMessage> DeleteBrandAsync(long id, CancellationToken ct = default);

    Task<IReadOnlyList<CategoryItem>?> ListCategoriesAsync(CancellationToken ct = default);
    Task<HttpResponseMessage> CreateCategoryAsync(CategoryInput input, CancellationToken ct = default);
    Task<HttpResponseMessage> UpdateCategoryAsync(long id, CategoryInput input, CancellationToken ct = default);
    Task<HttpResponseMessage> DeleteCategoryAsync(long id, CancellationToken ct = default);

    Task<ProductsPage?> ListProductsAsync(int page = 1, int pageSize = 20, string? search = null, CancellationToken ct = default);
    Task<ProductEditDto?> GetProductAsync(long id, CancellationToken ct = default);
    Task<HttpResponseMessage> CreateProductAsync(ProductInput input, CancellationToken ct = default);
    Task<HttpResponseMessage> UpdateProductAsync(long id, ProductInput input, CancellationToken ct = default);
    Task<HttpResponseMessage> DeleteProductAsync(long id, CancellationToken ct = default);

    // G07: variant CRUD
    Task<IReadOnlyList<ProductVariantDto>?> ListVariantsAsync(long parentId, CancellationToken ct = default);
    Task<HttpResponseMessage> CreateVariantAsync(long parentId, ProductVariantInput input, CancellationToken ct = default);
    Task<HttpResponseMessage> UpdateVariantAsync(long parentId, long variantId, ProductVariantInput input, CancellationToken ct = default);
    Task<HttpResponseMessage> DeleteVariantAsync(long parentId, long variantId, CancellationToken ct = default);
}

public sealed class AdminCatalogApi(HttpClient http) : IAdminCatalogApi
{
    public async Task<IReadOnlyList<BrandItem>?> ListBrandsAsync(CancellationToken ct) =>
        await http.GetFromJsonAsync<List<BrandItem>>("/api/admin/catalog/brands", ct);

    public Task<HttpResponseMessage> CreateBrandAsync(BrandInput input, CancellationToken ct) =>
        http.PostAsJsonAsync("/api/admin/catalog/brands", input, ct);

    public Task<HttpResponseMessage> UpdateBrandAsync(long id, BrandInput input, CancellationToken ct) =>
        http.PutAsJsonAsync($"/api/admin/catalog/brands/{id}", input, ct);

    public Task<HttpResponseMessage> DeleteBrandAsync(long id, CancellationToken ct) =>
        http.DeleteAsync($"/api/admin/catalog/brands/{id}", ct);

    public async Task<IReadOnlyList<CategoryItem>?> ListCategoriesAsync(CancellationToken ct) =>
        await http.GetFromJsonAsync<List<CategoryItem>>("/api/admin/catalog/categories", ct);

    public Task<HttpResponseMessage> CreateCategoryAsync(CategoryInput input, CancellationToken ct) =>
        http.PostAsJsonAsync("/api/admin/catalog/categories", input, ct);

    public Task<HttpResponseMessage> UpdateCategoryAsync(long id, CategoryInput input, CancellationToken ct) =>
        http.PutAsJsonAsync($"/api/admin/catalog/categories/{id}", input, ct);

    public Task<HttpResponseMessage> DeleteCategoryAsync(long id, CancellationToken ct) =>
        http.DeleteAsync($"/api/admin/catalog/categories/{id}", ct);

    public Task<ProductsPage?> ListProductsAsync(int page, int pageSize, string? search, CancellationToken ct)
    {
        var url = $"/api/admin/catalog/products?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(search)) url += $"&search={Uri.EscapeDataString(search)}";
        return http.GetFromJsonAsync<ProductsPage>(url, ct);
    }

    public Task<ProductEditDto?> GetProductAsync(long id, CancellationToken ct)
        => http.GetFromJsonAsync<ProductEditDto>($"/api/admin/catalog/products/{id}", ct);

    public Task<HttpResponseMessage> CreateProductAsync(ProductInput input, CancellationToken ct) =>
        http.PostAsJsonAsync("/api/admin/catalog/products", input, ct);

    public Task<HttpResponseMessage> UpdateProductAsync(long id, ProductInput input, CancellationToken ct) =>
        http.PutAsJsonAsync($"/api/admin/catalog/products/{id}", input, ct);

    public Task<HttpResponseMessage> DeleteProductAsync(long id, CancellationToken ct) =>
        http.DeleteAsync($"/api/admin/catalog/products/{id}", ct);

    public async Task<IReadOnlyList<ProductVariantDto>?> ListVariantsAsync(long parentId, CancellationToken ct) =>
        await http.GetFromJsonAsync<List<ProductVariantDto>>($"/api/admin/catalog/products/{parentId}/variants", ct);

    public Task<HttpResponseMessage> CreateVariantAsync(long parentId, ProductVariantInput input, CancellationToken ct) =>
        http.PostAsJsonAsync($"/api/admin/catalog/products/{parentId}/variants", input, ct);

    public Task<HttpResponseMessage> UpdateVariantAsync(long parentId, long variantId, ProductVariantInput input, CancellationToken ct) =>
        http.PutAsJsonAsync($"/api/admin/catalog/products/{parentId}/variants/{variantId}", input, ct);

    public Task<HttpResponseMessage> DeleteVariantAsync(long parentId, long variantId, CancellationToken ct) =>
        http.DeleteAsync($"/api/admin/catalog/products/{parentId}/variants/{variantId}", ct);
}

public interface IAdminOrdersApi
{
    Task<AdminOrdersPage?> ListAsync(int? status = null, string? customerSearch = null, int page = 1, int pageSize = 20, CancellationToken ct = default);
    Task<AdminOrderDetail?> GetAsync(long id, CancellationToken ct = default);
    Task<HttpResponseMessage> UpdateStatusAsync(long id, UpdateOrderStatusRequest req, CancellationToken ct = default);
    Task<AdminSalesReport?> GetSalesReportAsync(DateTimeOffset? from = null, DateTimeOffset? to = null, CancellationToken ct = default);
}

public sealed class AdminOrdersApi(HttpClient http) : IAdminOrdersApi
{
    public Task<AdminOrdersPage?> ListAsync(int? status, string? customerSearch, int page, int pageSize, CancellationToken ct)
    {
        var url = $"/api/admin/orders?page={page}&pageSize={pageSize}";
        if (status.HasValue) url += $"&status={status}";
        if (!string.IsNullOrWhiteSpace(customerSearch)) url += $"&customerSearch={Uri.EscapeDataString(customerSearch)}";
        return http.GetFromJsonAsync<AdminOrdersPage>(url, ct);
    }

    public Task<AdminOrderDetail?> GetAsync(long id, CancellationToken ct)
        => http.GetFromJsonAsync<AdminOrderDetail>($"/api/admin/orders/{id}", ct);

    public Task<HttpResponseMessage> UpdateStatusAsync(long id, UpdateOrderStatusRequest req, CancellationToken ct) =>
        http.PatchAsJsonAsync($"/api/admin/orders/{id}/status", req, ct);

    public Task<AdminSalesReport?> GetSalesReportAsync(DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct)
    {
        var url = "/api/admin/orders/sales-report";
        var qs = new List<string>();
        if (from.HasValue) qs.Add($"from={Uri.EscapeDataString(from.Value.ToString("o"))}");
        if (to.HasValue) qs.Add($"to={Uri.EscapeDataString(to.Value.ToString("o"))}");
        if (qs.Count > 0) url += "?" + string.Join("&", qs);
        return http.GetFromJsonAsync<AdminSalesReport>(url, ct);
    }
}

public interface IAdminShipmentsApi
{
    Task<IReadOnlyList<AdminShipmentListItem>?> ListAsync(long? orderId = null, CancellationToken ct = default);
    Task<AdminShipmentDetail?> GetAsync(long id, CancellationToken ct = default);
    Task<HttpResponseMessage> CreateAsync(AdminShipmentInput input, CancellationToken ct = default);
    Task<HttpResponseMessage> DeleteAsync(long id, CancellationToken ct = default);
    // G01: ShipmentStatus lifecycle transitions
    Task<HttpResponseMessage> UpdateStatusAsync(long id, AdminUpdateShipmentStatusRequest req, CancellationToken ct = default);
}

public sealed class AdminShipmentsApi(HttpClient http) : IAdminShipmentsApi
{
    public async Task<IReadOnlyList<AdminShipmentListItem>?> ListAsync(long? orderId, CancellationToken ct)
    {
        var url = "/api/admin/shipments/";
        if (orderId.HasValue) url += $"?orderId={orderId}";
        return await http.GetFromJsonAsync<List<AdminShipmentListItem>>(url, ct);
    }

    public Task<AdminShipmentDetail?> GetAsync(long id, CancellationToken ct) =>
        http.GetFromJsonAsync<AdminShipmentDetail>($"/api/admin/shipments/{id}", ct);

    public Task<HttpResponseMessage> CreateAsync(AdminShipmentInput input, CancellationToken ct) =>
        http.PostAsJsonAsync("/api/admin/shipments/", input, ct);

    public Task<HttpResponseMessage> DeleteAsync(long id, CancellationToken ct) =>
        http.DeleteAsync($"/api/admin/shipments/{id}", ct);

    public Task<HttpResponseMessage> UpdateStatusAsync(long id, AdminUpdateShipmentStatusRequest req, CancellationToken ct) =>
        http.PatchAsJsonAsync($"/api/admin/shipments/{id}/status", req, ct);
}

public interface IAdminCoreApi
{
    Task<MeResponse?> GetMeAsync(CancellationToken ct = default);
    Task<AdminUsersPage?> ListUsersAsync(int page = 1, int pageSize = 20, string? search = null, CancellationToken ct = default);
    Task<IReadOnlyList<AdminAppSettingItem>?> ListAppSettingsAsync(string? module = null, CancellationToken ct = default);
    Task<HttpResponseMessage> UpsertAppSettingAsync(string id, AdminAppSettingInput input, CancellationToken ct = default);
    Task<IReadOnlyList<AdminCustomerGroupItem>?> ListCustomerGroupsAsync(CancellationToken ct = default);
    Task<HttpResponseMessage> CreateCustomerGroupAsync(AdminCustomerGroupInput input, CancellationToken ct = default);
    Task<HttpResponseMessage> UpdateCustomerGroupAsync(long id, AdminCustomerGroupInput input, CancellationToken ct = default);
    Task<HttpResponseMessage> DeleteCustomerGroupAsync(long id, CancellationToken ct = default);
}

public sealed class AdminCoreApi(HttpClient http) : IAdminCoreApi
{
    public Task<MeResponse?> GetMeAsync(CancellationToken ct) =>
        http.GetFromJsonAsync<MeResponse>("/api/auth/me", ct);

    public Task<AdminUsersPage?> ListUsersAsync(int page, int pageSize, string? search, CancellationToken ct)
    {
        var url = $"/api/admin/core/users?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(search)) url += $"&search={Uri.EscapeDataString(search)}";
        return http.GetFromJsonAsync<AdminUsersPage>(url, ct);
    }

    public async Task<IReadOnlyList<AdminAppSettingItem>?> ListAppSettingsAsync(string? module, CancellationToken ct)
    {
        var url = "/api/admin/core/app-settings";
        if (!string.IsNullOrWhiteSpace(module)) url += $"?module={Uri.EscapeDataString(module)}";
        return await http.GetFromJsonAsync<List<AdminAppSettingItem>>(url, ct);
    }

    public Task<HttpResponseMessage> UpsertAppSettingAsync(string id, AdminAppSettingInput input, CancellationToken ct) =>
        http.PutAsJsonAsync($"/api/admin/core/app-settings/{Uri.EscapeDataString(id)}", input, ct);

    public async Task<IReadOnlyList<AdminCustomerGroupItem>?> ListCustomerGroupsAsync(CancellationToken ct) =>
        await http.GetFromJsonAsync<List<AdminCustomerGroupItem>>("/api/admin/core/customer-groups", ct);

    public Task<HttpResponseMessage> CreateCustomerGroupAsync(AdminCustomerGroupInput input, CancellationToken ct) =>
        http.PostAsJsonAsync("/api/admin/core/customer-groups", input, ct);

    public Task<HttpResponseMessage> UpdateCustomerGroupAsync(long id, AdminCustomerGroupInput input, CancellationToken ct) =>
        http.PutAsJsonAsync($"/api/admin/core/customer-groups/{id}", input, ct);

    public Task<HttpResponseMessage> DeleteCustomerGroupAsync(long id, CancellationToken ct) =>
        http.DeleteAsync($"/api/admin/core/customer-groups/{id}", ct);
}

public interface IAdminNewsApi
{
    Task<AdminNewsItemsPage?> ListItemsAsync(int page = 1, int pageSize = 20, CancellationToken ct = default);
    Task<AdminNewsItemDetail?> GetItemAsync(long id, CancellationToken ct = default);
    Task<HttpResponseMessage> CreateItemAsync(AdminNewsItemInput input, CancellationToken ct = default);
    Task<HttpResponseMessage> UpdateItemAsync(long id, AdminNewsItemInput input, CancellationToken ct = default);
    Task<HttpResponseMessage> DeleteItemAsync(long id, CancellationToken ct = default);
    Task<IReadOnlyList<AdminNewsCategoryItem>?> ListCategoriesAsync(CancellationToken ct = default);
    Task<HttpResponseMessage> CreateCategoryAsync(AdminNewsCategoryInput input, CancellationToken ct = default);
    Task<HttpResponseMessage> UpdateCategoryAsync(long id, AdminNewsCategoryInput input, CancellationToken ct = default);
    Task<HttpResponseMessage> DeleteCategoryAsync(long id, CancellationToken ct = default);
}

public sealed class AdminNewsApi(HttpClient http) : IAdminNewsApi
{
    public Task<AdminNewsItemsPage?> ListItemsAsync(int page, int pageSize, CancellationToken ct) =>
        http.GetFromJsonAsync<AdminNewsItemsPage>($"/api/admin/news/items?page={page}&pageSize={pageSize}", ct);

    public Task<AdminNewsItemDetail?> GetItemAsync(long id, CancellationToken ct) =>
        http.GetFromJsonAsync<AdminNewsItemDetail>($"/api/admin/news/items/{id}", ct);

    public Task<HttpResponseMessage> CreateItemAsync(AdminNewsItemInput input, CancellationToken ct) =>
        http.PostAsJsonAsync("/api/admin/news/items", input, ct);

    public Task<HttpResponseMessage> UpdateItemAsync(long id, AdminNewsItemInput input, CancellationToken ct) =>
        http.PutAsJsonAsync($"/api/admin/news/items/{id}", input, ct);

    public Task<HttpResponseMessage> DeleteItemAsync(long id, CancellationToken ct) =>
        http.DeleteAsync($"/api/admin/news/items/{id}", ct);

    public async Task<IReadOnlyList<AdminNewsCategoryItem>?> ListCategoriesAsync(CancellationToken ct) =>
        await http.GetFromJsonAsync<List<AdminNewsCategoryItem>>("/api/admin/news/categories", ct);

    public Task<HttpResponseMessage> CreateCategoryAsync(AdminNewsCategoryInput input, CancellationToken ct) =>
        http.PostAsJsonAsync("/api/admin/news/categories", input, ct);

    public Task<HttpResponseMessage> UpdateCategoryAsync(long id, AdminNewsCategoryInput input, CancellationToken ct) =>
        http.PutAsJsonAsync($"/api/admin/news/categories/{id}", input, ct);

    public Task<HttpResponseMessage> DeleteCategoryAsync(long id, CancellationToken ct) =>
        http.DeleteAsync($"/api/admin/news/categories/{id}", ct);
}

public interface IAdminReviewsApi
{
    Task<AdminReviewsPage?> ListAsync(int? status = null, int page = 1, int pageSize = 20, CancellationToken ct = default);
    Task<HttpResponseMessage> ModerateAsync(long id, ModerationRequest req, CancellationToken ct = default);
}

public sealed class AdminReviewsApi(HttpClient http) : IAdminReviewsApi
{
    public Task<AdminReviewsPage?> ListAsync(int? status, int page, int pageSize, CancellationToken ct)
    {
        var url = $"/api/admin/reviews?page={page}&pageSize={pageSize}";
        if (status.HasValue) url += $"&status={status}";
        return http.GetFromJsonAsync<AdminReviewsPage>(url, ct);
    }

    public Task<HttpResponseMessage> ModerateAsync(long id, ModerationRequest req, CancellationToken ct) =>
        http.PatchAsJsonAsync($"/api/admin/reviews/{id}/status", req, ct);
}

public interface IAdminInventoryApi
{
    Task<IReadOnlyList<AdminWarehouseItem>?> ListWarehousesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<AdminStockItem>?> ListStocksAsync(long? warehouseId = null, CancellationToken ct = default);
    Task<IReadOnlyList<AdminStockHistoryItem>?> ListStockHistoryAsync(long? productId = null, long? warehouseId = null, int page = 1, int pageSize = 50, CancellationToken ct = default);
    Task<HttpResponseMessage> AdjustStockAsync(AdminStockAdjustmentInput input, CancellationToken ct = default);
}

public sealed class AdminInventoryApi(HttpClient http) : IAdminInventoryApi
{
    public async Task<IReadOnlyList<AdminWarehouseItem>?> ListWarehousesAsync(CancellationToken ct) =>
        await http.GetFromJsonAsync<List<AdminWarehouseItem>>("/api/admin/inventory/warehouses", ct);

    public async Task<IReadOnlyList<AdminStockItem>?> ListStocksAsync(long? warehouseId, CancellationToken ct)
    {
        var url = "/api/admin/inventory/stocks";
        if (warehouseId.HasValue) url += $"?warehouseId={warehouseId}";
        return await http.GetFromJsonAsync<List<AdminStockItem>>(url, ct);
    }

    public async Task<IReadOnlyList<AdminStockHistoryItem>?> ListStockHistoryAsync(long? productId, long? warehouseId, int page, int pageSize, CancellationToken ct)
    {
        var url = $"/api/admin/inventory/stock-history?page={page}&pageSize={pageSize}";
        if (productId.HasValue) url += $"&productId={productId}";
        if (warehouseId.HasValue) url += $"&warehouseId={warehouseId}";
        return await http.GetFromJsonAsync<List<AdminStockHistoryItem>>(url, ct);
    }

    public Task<HttpResponseMessage> AdjustStockAsync(AdminStockAdjustmentInput input, CancellationToken ct) =>
        http.PostAsJsonAsync("/api/admin/inventory/stock-adjustments", input, ct);
}

public interface IAdminActivityApi
{
    Task<AdminActivityPage?> ListAsync(int page = 1, int pageSize = 50, CancellationToken ct = default);
}

public sealed class AdminActivityApi(HttpClient http) : IAdminActivityApi
{
    public Task<AdminActivityPage?> ListAsync(int page, int pageSize, CancellationToken ct) =>
        http.GetFromJsonAsync<AdminActivityPage>($"/api/admin/activity-log?page={page}&pageSize={pageSize}", ct);
}

public interface IAdminVendorsApi
{
    Task<IReadOnlyList<AdminVendorItem>?> ListAsync(CancellationToken ct = default);
    Task<AdminVendorDetail?> GetAsync(long id, CancellationToken ct = default);
    Task<HttpResponseMessage> CreateAsync(AdminVendorInput input, CancellationToken ct = default);
    Task<HttpResponseMessage> UpdateAsync(long id, AdminVendorInput input, CancellationToken ct = default);
    Task<HttpResponseMessage> DeleteAsync(long id, CancellationToken ct = default);

    // Wave 7: onboarding queue
    Task<AdminVendorApplicationsPage?> ListApplicationsAsync(int? status = null, int page = 1, int pageSize = 20, CancellationToken ct = default);
    Task<HttpResponseMessage> ApproveApplicationAsync(long id, AdminVendorApplicationDecision req, CancellationToken ct = default);
    Task<HttpResponseMessage> RejectApplicationAsync(long id, AdminVendorApplicationDecision req, CancellationToken ct = default);

    // Wave 9: dashboard bootstrap. Returns null on 204 (admin caller, no vendor context).
    Task<AdminVendorSelfResponse?> GetSelfAsync(CancellationToken ct = default);
}

public sealed class AdminVendorsApi(HttpClient http) : IAdminVendorsApi
{
    public async Task<IReadOnlyList<AdminVendorItem>?> ListAsync(CancellationToken ct) =>
        await http.GetFromJsonAsync<List<AdminVendorItem>>("/api/admin/vendors/", ct);

    public Task<AdminVendorDetail?> GetAsync(long id, CancellationToken ct) =>
        http.GetFromJsonAsync<AdminVendorDetail>($"/api/admin/vendors/{id}", ct);

    public Task<HttpResponseMessage> CreateAsync(AdminVendorInput input, CancellationToken ct) =>
        http.PostAsJsonAsync("/api/admin/vendors/", input, ct);

    public Task<HttpResponseMessage> UpdateAsync(long id, AdminVendorInput input, CancellationToken ct) =>
        http.PutAsJsonAsync($"/api/admin/vendors/{id}", input, ct);

    public Task<HttpResponseMessage> DeleteAsync(long id, CancellationToken ct) =>
        http.DeleteAsync($"/api/admin/vendors/{id}", ct);

    public Task<AdminVendorApplicationsPage?> ListApplicationsAsync(int? status, int page, int pageSize, CancellationToken ct)
    {
        var url = $"/api/admin/vendors/applications/?page={page}&pageSize={pageSize}";
        if (status.HasValue) url += $"&status={status}";
        return http.GetFromJsonAsync<AdminVendorApplicationsPage>(url, ct);
    }

    public Task<HttpResponseMessage> ApproveApplicationAsync(long id, AdminVendorApplicationDecision req, CancellationToken ct) =>
        http.PostAsJsonAsync($"/api/admin/vendors/applications/{id}/approve", req, ct);

    public Task<HttpResponseMessage> RejectApplicationAsync(long id, AdminVendorApplicationDecision req, CancellationToken ct) =>
        http.PostAsJsonAsync($"/api/admin/vendors/applications/{id}/reject", req, ct);

    public async Task<AdminVendorSelfResponse?> GetSelfAsync(CancellationToken ct)
    {
        // 204 No Content = admin / no vendor context; treat as null so the
        // dashboard renders its admin view.
        using var resp = await http.GetAsync("/api/admin/vendors/me", ct);
        if (resp.StatusCode == System.Net.HttpStatusCode.NoContent) return null;
        if (!resp.IsSuccessStatusCode) return null;
        return await resp.Content.ReadFromJsonAsync<AdminVendorSelfResponse>(cancellationToken: ct);
    }
}

public interface IAdminTaxApi
{
    Task<IReadOnlyList<AdminTaxClassItem>?> ListClassesAsync(CancellationToken ct = default);
    Task<HttpResponseMessage> CreateClassAsync(AdminTaxClassInput input, CancellationToken ct = default);
    Task<HttpResponseMessage> UpdateClassAsync(long id, AdminTaxClassInput input, CancellationToken ct = default);
    Task<HttpResponseMessage> DeleteClassAsync(long id, CancellationToken ct = default);
    Task<IReadOnlyList<AdminTaxRateItem>?> ListRatesAsync(CancellationToken ct = default);
    Task<HttpResponseMessage> CreateRateAsync(AdminTaxRateInput input, CancellationToken ct = default);
    Task<HttpResponseMessage> UpdateRateAsync(long id, AdminTaxRateInput input, CancellationToken ct = default);
    Task<HttpResponseMessage> DeleteRateAsync(long id, CancellationToken ct = default);
}

public sealed class AdminTaxApi(HttpClient http) : IAdminTaxApi
{
    public async Task<IReadOnlyList<AdminTaxClassItem>?> ListClassesAsync(CancellationToken ct) =>
        await http.GetFromJsonAsync<List<AdminTaxClassItem>>("/api/admin/tax/classes", ct);

    public Task<HttpResponseMessage> CreateClassAsync(AdminTaxClassInput input, CancellationToken ct) =>
        http.PostAsJsonAsync("/api/admin/tax/classes", input, ct);

    public Task<HttpResponseMessage> UpdateClassAsync(long id, AdminTaxClassInput input, CancellationToken ct) =>
        http.PutAsJsonAsync($"/api/admin/tax/classes/{id}", input, ct);

    public Task<HttpResponseMessage> DeleteClassAsync(long id, CancellationToken ct) =>
        http.DeleteAsync($"/api/admin/tax/classes/{id}", ct);

    public async Task<IReadOnlyList<AdminTaxRateItem>?> ListRatesAsync(CancellationToken ct) =>
        await http.GetFromJsonAsync<List<AdminTaxRateItem>>("/api/admin/tax/rates", ct);

    public Task<HttpResponseMessage> CreateRateAsync(AdminTaxRateInput input, CancellationToken ct) =>
        http.PostAsJsonAsync("/api/admin/tax/rates", input, ct);

    public Task<HttpResponseMessage> UpdateRateAsync(long id, AdminTaxRateInput input, CancellationToken ct) =>
        http.PutAsJsonAsync($"/api/admin/tax/rates/{id}", input, ct);

    public Task<HttpResponseMessage> DeleteRateAsync(long id, CancellationToken ct) =>
        http.DeleteAsync($"/api/admin/tax/rates/{id}", ct);
}

public interface IAdminShippingApi
{
    Task<IReadOnlyList<AdminShippingProviderItem>?> ListProvidersAsync(CancellationToken ct = default);
    Task<AdminShippingProviderDetail?> GetProviderAsync(string id, CancellationToken ct = default);
    Task<HttpResponseMessage> UpdateProviderAsync(string id, AdminShippingProviderInput input, CancellationToken ct = default);

    Task<IReadOnlyList<AdminTableRateItem>?> ListTableRatesAsync(CancellationToken ct = default);
    Task<HttpResponseMessage> CreateTableRateAsync(AdminTableRateInput input, CancellationToken ct = default);
    Task<HttpResponseMessage> UpdateTableRateAsync(long id, AdminTableRateInput input, CancellationToken ct = default);
    Task<HttpResponseMessage> DeleteTableRateAsync(long id, CancellationToken ct = default);
}

public sealed class AdminShippingApi(HttpClient http) : IAdminShippingApi
{
    public async Task<IReadOnlyList<AdminShippingProviderItem>?> ListProvidersAsync(CancellationToken ct) =>
        await http.GetFromJsonAsync<List<AdminShippingProviderItem>>("/api/admin/shipping/providers", ct);

    public Task<AdminShippingProviderDetail?> GetProviderAsync(string id, CancellationToken ct) =>
        http.GetFromJsonAsync<AdminShippingProviderDetail>($"/api/admin/shipping/providers/{Uri.EscapeDataString(id)}", ct);

    public Task<HttpResponseMessage> UpdateProviderAsync(string id, AdminShippingProviderInput input, CancellationToken ct) =>
        http.PutAsJsonAsync($"/api/admin/shipping/providers/{Uri.EscapeDataString(id)}", input, ct);

    public async Task<IReadOnlyList<AdminTableRateItem>?> ListTableRatesAsync(CancellationToken ct) =>
        await http.GetFromJsonAsync<List<AdminTableRateItem>>("/api/admin/shipping/table-rates/", ct);

    public Task<HttpResponseMessage> CreateTableRateAsync(AdminTableRateInput input, CancellationToken ct) =>
        http.PostAsJsonAsync("/api/admin/shipping/table-rates/", input, ct);

    public Task<HttpResponseMessage> UpdateTableRateAsync(long id, AdminTableRateInput input, CancellationToken ct) =>
        http.PutAsJsonAsync($"/api/admin/shipping/table-rates/{id}", input, ct);

    public Task<HttpResponseMessage> DeleteTableRateAsync(long id, CancellationToken ct) =>
        http.DeleteAsync($"/api/admin/shipping/table-rates/{id}", ct);
}

public interface IAdminPaymentsApi
{
    Task<IReadOnlyList<AdminPaymentProviderItem>?> ListProvidersAsync(CancellationToken ct = default);
    Task<AdminPaymentProviderDetail?> GetProviderAsync(string id, CancellationToken ct = default);
    Task<HttpResponseMessage> UpdateProviderAsync(string id, AdminPaymentProviderInput input, CancellationToken ct = default);
    Task<AdminPaymentsPage?> ListPaymentsAsync(int page = 1, int pageSize = 20, CancellationToken ct = default);
    // G03: full or partial refund against an order's captured Payment.
    Task<HttpResponseMessage> CreateRefundAsync(AdminRefundRequest req, CancellationToken ct = default);
}

public sealed class AdminPaymentsApi(HttpClient http) : IAdminPaymentsApi
{
    public async Task<IReadOnlyList<AdminPaymentProviderItem>?> ListProvidersAsync(CancellationToken ct) =>
        await http.GetFromJsonAsync<List<AdminPaymentProviderItem>>("/api/admin/payments/providers", ct);

    public Task<AdminPaymentProviderDetail?> GetProviderAsync(string id, CancellationToken ct) =>
        http.GetFromJsonAsync<AdminPaymentProviderDetail>($"/api/admin/payments/providers/{Uri.EscapeDataString(id)}", ct);

    public Task<HttpResponseMessage> UpdateProviderAsync(string id, AdminPaymentProviderInput input, CancellationToken ct) =>
        http.PutAsJsonAsync($"/api/admin/payments/providers/{Uri.EscapeDataString(id)}", input, ct);

    public Task<AdminPaymentsPage?> ListPaymentsAsync(int page, int pageSize, CancellationToken ct) =>
        http.GetFromJsonAsync<AdminPaymentsPage>($"/api/admin/payments/?page={page}&pageSize={pageSize}", ct);

    public Task<HttpResponseMessage> CreateRefundAsync(AdminRefundRequest req, CancellationToken ct) =>
        http.PostAsJsonAsync("/api/admin/payments/refunds", req, ct);
}

public interface IAdminPricingApi
{
    Task<IReadOnlyList<AdminCartRuleItem>?> ListCartRulesAsync(CancellationToken ct = default);
    Task<AdminCartRuleDetail?> GetCartRuleAsync(long id, CancellationToken ct = default);
    Task<HttpResponseMessage> CreateCartRuleAsync(AdminCartRuleInput input, CancellationToken ct = default);
    Task<HttpResponseMessage> UpdateCartRuleAsync(long id, AdminCartRuleInput input, CancellationToken ct = default);
    Task<HttpResponseMessage> DeleteCartRuleAsync(long id, CancellationToken ct = default);

    Task<IReadOnlyList<AdminCatalogRuleItem>?> ListCatalogRulesAsync(CancellationToken ct = default);

    Task<IReadOnlyList<AdminCouponItem>?> ListCouponsAsync(CancellationToken ct = default);
    Task<HttpResponseMessage> CreateCouponAsync(AdminCouponInput input, CancellationToken ct = default);
    Task<HttpResponseMessage> DeleteCouponAsync(long id, CancellationToken ct = default);
}

public sealed class AdminPricingApi(HttpClient http) : IAdminPricingApi
{
    public async Task<IReadOnlyList<AdminCartRuleItem>?> ListCartRulesAsync(CancellationToken ct) =>
        await http.GetFromJsonAsync<List<AdminCartRuleItem>>("/api/admin/pricing/cart-rules", ct);

    public Task<AdminCartRuleDetail?> GetCartRuleAsync(long id, CancellationToken ct) =>
        http.GetFromJsonAsync<AdminCartRuleDetail>($"/api/admin/pricing/cart-rules/{id}", ct);

    public Task<HttpResponseMessage> CreateCartRuleAsync(AdminCartRuleInput input, CancellationToken ct) =>
        http.PostAsJsonAsync("/api/admin/pricing/cart-rules", input, ct);

    public Task<HttpResponseMessage> UpdateCartRuleAsync(long id, AdminCartRuleInput input, CancellationToken ct) =>
        http.PutAsJsonAsync($"/api/admin/pricing/cart-rules/{id}", input, ct);

    public Task<HttpResponseMessage> DeleteCartRuleAsync(long id, CancellationToken ct) =>
        http.DeleteAsync($"/api/admin/pricing/cart-rules/{id}", ct);

    public async Task<IReadOnlyList<AdminCatalogRuleItem>?> ListCatalogRulesAsync(CancellationToken ct) =>
        await http.GetFromJsonAsync<List<AdminCatalogRuleItem>>("/api/admin/pricing/catalog-rules", ct);

    public async Task<IReadOnlyList<AdminCouponItem>?> ListCouponsAsync(CancellationToken ct) =>
        await http.GetFromJsonAsync<List<AdminCouponItem>>("/api/admin/pricing/coupons", ct);

    public Task<HttpResponseMessage> CreateCouponAsync(AdminCouponInput input, CancellationToken ct) =>
        http.PostAsJsonAsync("/api/admin/pricing/coupons", input, ct);

    public Task<HttpResponseMessage> DeleteCouponAsync(long id, CancellationToken ct) =>
        http.DeleteAsync($"/api/admin/pricing/coupons/{id}", ct);
}

public interface IAdminCmsApi
{
    Task<IReadOnlyList<AdminCmsPageListItem>?> ListPagesAsync(CancellationToken ct = default);
    Task<AdminCmsPageDetail?> GetPageAsync(long id, CancellationToken ct = default);
    Task<HttpResponseMessage> CreatePageAsync(AdminCmsPageInput input, CancellationToken ct = default);
    Task<HttpResponseMessage> UpdatePageAsync(long id, AdminCmsPageInput input, CancellationToken ct = default);
    Task<HttpResponseMessage> DeletePageAsync(long id, CancellationToken ct = default);
}

public sealed class AdminCmsApi(HttpClient http) : IAdminCmsApi
{
    public async Task<IReadOnlyList<AdminCmsPageListItem>?> ListPagesAsync(CancellationToken ct) =>
        await http.GetFromJsonAsync<List<AdminCmsPageListItem>>("/api/admin/cms/pages", ct);

    public Task<AdminCmsPageDetail?> GetPageAsync(long id, CancellationToken ct) =>
        http.GetFromJsonAsync<AdminCmsPageDetail>($"/api/admin/cms/pages/{id}", ct);

    public Task<HttpResponseMessage> CreatePageAsync(AdminCmsPageInput input, CancellationToken ct) =>
        http.PostAsJsonAsync("/api/admin/cms/pages", input, ct);

    public Task<HttpResponseMessage> UpdatePageAsync(long id, AdminCmsPageInput input, CancellationToken ct) =>
        http.PutAsJsonAsync($"/api/admin/cms/pages/{id}", input, ct);

    public Task<HttpResponseMessage> DeletePageAsync(long id, CancellationToken ct) =>
        http.DeleteAsync($"/api/admin/cms/pages/{id}", ct);
}

public interface IAdminCommentsApi
{
    Task<AdminCommentsPage?> ListAsync(int? status = null, int page = 1, int pageSize = 20, CancellationToken ct = default);
    Task<HttpResponseMessage> SetStatusAsync(long id, AdminCommentStatusInput input, CancellationToken ct = default);
    Task<HttpResponseMessage> DeleteAsync(long id, CancellationToken ct = default);
}

public sealed class AdminCommentsApi(HttpClient http) : IAdminCommentsApi
{
    public Task<AdminCommentsPage?> ListAsync(int? status, int page, int pageSize, CancellationToken ct)
    {
        var url = $"/api/admin/comments?page={page}&pageSize={pageSize}";
        if (status.HasValue) url += $"&status={status}";
        return http.GetFromJsonAsync<AdminCommentsPage>(url, ct);
    }

    public Task<HttpResponseMessage> SetStatusAsync(long id, AdminCommentStatusInput input, CancellationToken ct) =>
        http.PatchAsJsonAsync($"/api/admin/comments/{id}/status", input, ct);

    public Task<HttpResponseMessage> DeleteAsync(long id, CancellationToken ct) =>
        http.DeleteAsync($"/api/admin/comments/{id}", ct);
}

public interface IAdminContactsApi
{
    Task<AdminContactsPage?> ListAsync(int? status = null, int page = 1, int pageSize = 20, CancellationToken ct = default);
    Task<AdminContactItem?> GetAsync(long id, CancellationToken ct = default);
    Task<HttpResponseMessage> SetStatusAsync(long id, AdminContactStatusInput input, CancellationToken ct = default);
}

public sealed class AdminContactsApi(HttpClient http) : IAdminContactsApi
{
    public Task<AdminContactsPage?> ListAsync(int? status, int page, int pageSize, CancellationToken ct)
    {
        var url = $"/api/admin/contacts?page={page}&pageSize={pageSize}";
        if (status.HasValue) url += $"&status={status}";
        return http.GetFromJsonAsync<AdminContactsPage>(url, ct);
    }

    public Task<AdminContactItem?> GetAsync(long id, CancellationToken ct) =>
        http.GetFromJsonAsync<AdminContactItem>($"/api/admin/contacts/{id}", ct);

    public Task<HttpResponseMessage> SetStatusAsync(long id, AdminContactStatusInput input, CancellationToken ct) =>
        http.PatchAsJsonAsync($"/api/admin/contacts/{id}/status", input, ct);
}

public interface IAdminLocalizationApi
{
    Task<IReadOnlyList<AdminCultureItem>?> ListCulturesAsync(CancellationToken ct = default);
    Task<HttpResponseMessage> CreateCultureAsync(AdminCultureInput input, CancellationToken ct = default);
    Task<HttpResponseMessage> DeleteCultureAsync(string id, CancellationToken ct = default);
    Task<AdminResourcesPage?> ListResourcesAsync(string? cultureId = null, string? search = null, int page = 1, int pageSize = 100, CancellationToken ct = default);
    Task<HttpResponseMessage> UpsertResourceAsync(AdminResourceInput input, CancellationToken ct = default);
    Task<HttpResponseMessage> UpdateResourceAsync(long id, AdminResourceInput input, CancellationToken ct = default);
    Task<HttpResponseMessage> DeleteResourceAsync(long id, CancellationToken ct = default);
}

public sealed class AdminLocalizationApi(HttpClient http) : IAdminLocalizationApi
{
    public async Task<IReadOnlyList<AdminCultureItem>?> ListCulturesAsync(CancellationToken ct) =>
        await http.GetFromJsonAsync<List<AdminCultureItem>>("/api/admin/localization/cultures", ct);

    public Task<HttpResponseMessage> CreateCultureAsync(AdminCultureInput input, CancellationToken ct) =>
        http.PostAsJsonAsync("/api/admin/localization/cultures", input, ct);

    public Task<HttpResponseMessage> DeleteCultureAsync(string id, CancellationToken ct) =>
        http.DeleteAsync($"/api/admin/localization/cultures/{Uri.EscapeDataString(id)}", ct);

    public Task<AdminResourcesPage?> ListResourcesAsync(string? cultureId, string? search, int page, int pageSize, CancellationToken ct)
    {
        var url = $"/api/admin/localization/resources?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(cultureId)) url += $"&cultureId={Uri.EscapeDataString(cultureId)}";
        if (!string.IsNullOrWhiteSpace(search)) url += $"&search={Uri.EscapeDataString(search)}";
        return http.GetFromJsonAsync<AdminResourcesPage>(url, ct);
    }

    public Task<HttpResponseMessage> UpsertResourceAsync(AdminResourceInput input, CancellationToken ct) =>
        http.PostAsJsonAsync("/api/admin/localization/resources", input, ct);

    public Task<HttpResponseMessage> UpdateResourceAsync(long id, AdminResourceInput input, CancellationToken ct) =>
        http.PutAsJsonAsync($"/api/admin/localization/resources/{id}", input, ct);

    public Task<HttpResponseMessage> DeleteResourceAsync(long id, CancellationToken ct) =>
        http.DeleteAsync($"/api/admin/localization/resources/{id}", ct);
}

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

    Task<ProductsPage?> ListProductsAsync(int page = 1, int pageSize = 20, string? search = null, CancellationToken ct = default);
    Task<HttpResponseMessage> DeleteProductAsync(long id, CancellationToken ct = default);
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

    public Task<ProductsPage?> ListProductsAsync(int page, int pageSize, string? search, CancellationToken ct)
    {
        var url = $"/api/admin/catalog/products?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(search)) url += $"&search={Uri.EscapeDataString(search)}";
        return http.GetFromJsonAsync<ProductsPage>(url, ct);
    }

    public Task<HttpResponseMessage> DeleteProductAsync(long id, CancellationToken ct) =>
        http.DeleteAsync($"/api/admin/catalog/products/{id}", ct);
}

public interface IAdminOrdersApi
{
    Task<AdminOrdersPage?> ListAsync(int? status = null, string? customerSearch = null, int page = 1, int pageSize = 20, CancellationToken ct = default);
    Task<HttpResponseMessage> UpdateStatusAsync(long id, UpdateOrderStatusRequest req, CancellationToken ct = default);
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

    public Task<HttpResponseMessage> UpdateStatusAsync(long id, UpdateOrderStatusRequest req, CancellationToken ct) =>
        http.PatchAsJsonAsync($"/api/admin/orders/{id}/status", req, ct);
}

public interface IAdminCoreApi
{
    Task<MeResponse?> GetMeAsync(CancellationToken ct = default);
    Task<AdminUsersPage?> ListUsersAsync(int page = 1, int pageSize = 20, string? search = null, CancellationToken ct = default);
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
    Task<HttpResponseMessage> CreateAsync(AdminVendorInput input, CancellationToken ct = default);
}

public sealed class AdminVendorsApi(HttpClient http) : IAdminVendorsApi
{
    public async Task<IReadOnlyList<AdminVendorItem>?> ListAsync(CancellationToken ct) =>
        await http.GetFromJsonAsync<List<AdminVendorItem>>("/api/admin/vendors/", ct);

    public Task<HttpResponseMessage> CreateAsync(AdminVendorInput input, CancellationToken ct) =>
        http.PostAsJsonAsync("/api/admin/vendors/", input, ct);
}

public interface IAdminTaxApi
{
    Task<IReadOnlyList<AdminTaxClassItem>?> ListClassesAsync(CancellationToken ct = default);
    Task<HttpResponseMessage> CreateClassAsync(AdminTaxClassInput input, CancellationToken ct = default);
    Task<IReadOnlyList<AdminTaxRateItem>?> ListRatesAsync(CancellationToken ct = default);
    Task<HttpResponseMessage> CreateRateAsync(AdminTaxRateInput input, CancellationToken ct = default);
}

public sealed class AdminTaxApi(HttpClient http) : IAdminTaxApi
{
    public async Task<IReadOnlyList<AdminTaxClassItem>?> ListClassesAsync(CancellationToken ct) =>
        await http.GetFromJsonAsync<List<AdminTaxClassItem>>("/api/admin/tax/classes", ct);

    public Task<HttpResponseMessage> CreateClassAsync(AdminTaxClassInput input, CancellationToken ct) =>
        http.PostAsJsonAsync("/api/admin/tax/classes", input, ct);

    public async Task<IReadOnlyList<AdminTaxRateItem>?> ListRatesAsync(CancellationToken ct) =>
        await http.GetFromJsonAsync<List<AdminTaxRateItem>>("/api/admin/tax/rates", ct);

    public Task<HttpResponseMessage> CreateRateAsync(AdminTaxRateInput input, CancellationToken ct) =>
        http.PostAsJsonAsync("/api/admin/tax/rates", input, ct);
}

public interface IAdminShippingApi
{
    Task<IReadOnlyList<AdminShippingProviderItem>?> ListProvidersAsync(CancellationToken ct = default);
}

public sealed class AdminShippingApi(HttpClient http) : IAdminShippingApi
{
    public async Task<IReadOnlyList<AdminShippingProviderItem>?> ListProvidersAsync(CancellationToken ct) =>
        await http.GetFromJsonAsync<List<AdminShippingProviderItem>>("/api/admin/shipping/providers", ct);
}

public interface IAdminPaymentsApi
{
    Task<IReadOnlyList<AdminPaymentProviderItem>?> ListProvidersAsync(CancellationToken ct = default);
    Task<AdminPaymentsPage?> ListPaymentsAsync(int page = 1, int pageSize = 20, CancellationToken ct = default);
}

public sealed class AdminPaymentsApi(HttpClient http) : IAdminPaymentsApi
{
    public async Task<IReadOnlyList<AdminPaymentProviderItem>?> ListProvidersAsync(CancellationToken ct) =>
        await http.GetFromJsonAsync<List<AdminPaymentProviderItem>>("/api/admin/payments/providers", ct);

    public Task<AdminPaymentsPage?> ListPaymentsAsync(int page, int pageSize, CancellationToken ct) =>
        http.GetFromJsonAsync<AdminPaymentsPage>($"/api/admin/payments/?page={page}&pageSize={pageSize}", ct);
}

public interface IAdminPricingApi
{
    Task<IReadOnlyList<AdminCartRuleItem>?> ListCartRulesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<AdminCatalogRuleItem>?> ListCatalogRulesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<AdminCouponItem>?> ListCouponsAsync(CancellationToken ct = default);
}

public sealed class AdminPricingApi(HttpClient http) : IAdminPricingApi
{
    public async Task<IReadOnlyList<AdminCartRuleItem>?> ListCartRulesAsync(CancellationToken ct) =>
        await http.GetFromJsonAsync<List<AdminCartRuleItem>>("/api/admin/pricing/cart-rules", ct);

    public async Task<IReadOnlyList<AdminCatalogRuleItem>?> ListCatalogRulesAsync(CancellationToken ct) =>
        await http.GetFromJsonAsync<List<AdminCatalogRuleItem>>("/api/admin/pricing/catalog-rules", ct);

    public async Task<IReadOnlyList<AdminCouponItem>?> ListCouponsAsync(CancellationToken ct) =>
        await http.GetFromJsonAsync<List<AdminCouponItem>>("/api/admin/pricing/coupons", ct);
}

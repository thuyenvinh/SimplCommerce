using System.Net.Http.Json;

namespace SimplCommerce.Storefront.Services.ApiClients;

public interface ICatalogApi
{
    Task<PagedResult<ProductListItem>?> ListProductsAsync(long? categoryId = null, long? brandId = null, string? search = null, int page = 1, int pageSize = 24, CancellationToken ct = default);
    Task<ProductDetailResponse?> GetProductBySlugAsync(string slug, CancellationToken ct = default);
    Task<IReadOnlyList<CategoryItem>?> ListCategoriesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<BrandItem>?> ListBrandsAsync(CancellationToken ct = default);
}

public sealed class CatalogApi(HttpClient http) : ICatalogApi
{
    public Task<PagedResult<ProductListItem>?> ListProductsAsync(long? categoryId, long? brandId, string? search, int page, int pageSize, CancellationToken ct)
    {
        var url = $"/api/storefront/catalog/products?page={page}&pageSize={pageSize}";
        if (categoryId.HasValue) url += $"&categoryId={categoryId}";
        if (brandId.HasValue) url += $"&brandId={brandId}";
        if (!string.IsNullOrWhiteSpace(search)) url += $"&search={Uri.EscapeDataString(search)}";
        return http.GetFromJsonAsync<PagedResult<ProductListItem>>(url, ct);
    }

    public Task<ProductDetailResponse?> GetProductBySlugAsync(string slug, CancellationToken ct) =>
        http.GetFromJsonAsync<ProductDetailResponse>($"/api/storefront/catalog/products/by-slug/{Uri.EscapeDataString(slug)}", ct);

    public async Task<IReadOnlyList<CategoryItem>?> ListCategoriesAsync(CancellationToken ct) =>
        await http.GetFromJsonAsync<List<CategoryItem>>("/api/storefront/catalog/categories", ct);

    public async Task<IReadOnlyList<BrandItem>?> ListBrandsAsync(CancellationToken ct) =>
        await http.GetFromJsonAsync<List<BrandItem>>("/api/storefront/catalog/brands", ct);
}

public interface ISearchApi
{
    Task<SearchResponse?> SearchAsync(string q, int page = 1, int pageSize = 24, CancellationToken ct = default);
}

public sealed class SearchApi(HttpClient http) : ISearchApi
{
    public Task<SearchResponse?> SearchAsync(string q, int page, int pageSize, CancellationToken ct) =>
        http.GetFromJsonAsync<SearchResponse>($"/api/storefront/search?q={Uri.EscapeDataString(q)}&page={page}&pageSize={pageSize}", ct);
}

public interface ICartApi
{
    Task<HttpResponseMessage> GetAsync(CancellationToken ct = default);
    Task<HttpResponseMessage> AddAsync(AddToCartRequest req, CancellationToken ct = default);
    Task<HttpResponseMessage> ApplyCouponAsync(ApplyCouponRequest req, CancellationToken ct = default);
}

public sealed class CartApi(HttpClient http) : ICartApi
{
    public Task<HttpResponseMessage> GetAsync(CancellationToken ct) =>
        http.GetAsync("/api/storefront/cart/", ct);

    public Task<HttpResponseMessage> AddAsync(AddToCartRequest req, CancellationToken ct) =>
        http.PostAsJsonAsync("/api/storefront/cart/items", req, ct);

    public Task<HttpResponseMessage> ApplyCouponAsync(ApplyCouponRequest req, CancellationToken ct) =>
        http.PostAsJsonAsync("/api/storefront/cart/coupon", req, ct);
}

public interface IAuthApi
{
    Task<HttpResponseMessage> LoginAsync(LoginRequest req, CancellationToken ct = default);
    Task<HttpResponseMessage> RegisterAsync(RegisterRequest req, CancellationToken ct = default);
}

public sealed class AuthApi(HttpClient http) : IAuthApi
{
    public Task<HttpResponseMessage> LoginAsync(LoginRequest req, CancellationToken ct) =>
        http.PostAsJsonAsync("/api/auth/login", req, ct);

    public Task<HttpResponseMessage> RegisterAsync(RegisterRequest req, CancellationToken ct) =>
        http.PostAsJsonAsync("/api/auth/register", req, ct);
}

public interface IAccountApi
{
    Task<MeResponse?> GetMeAsync(CancellationToken ct = default);
    Task<IReadOnlyList<AddressDto>?> GetAddressesAsync(CancellationToken ct = default);
}

public sealed class AccountApi(HttpClient http) : IAccountApi
{
    public Task<MeResponse?> GetMeAsync(CancellationToken ct) =>
        http.GetFromJsonAsync<MeResponse>("/api/auth/me", ct);

    public async Task<IReadOnlyList<AddressDto>?> GetAddressesAsync(CancellationToken ct) =>
        await http.GetFromJsonAsync<List<AddressDto>>("/api/storefront/core/addresses", ct);
}

public interface IOrderApi
{
    Task<IReadOnlyList<OrderListItem>?> ListAsync(int page = 1, int pageSize = 20, CancellationToken ct = default);
    Task<OrderDetailDto?> GetAsync(long id, CancellationToken ct = default);
}

public sealed class OrderApi(HttpClient http) : IOrderApi
{
    public async Task<IReadOnlyList<OrderListItem>?> ListAsync(int page, int pageSize, CancellationToken ct) =>
        await http.GetFromJsonAsync<List<OrderListItem>>($"/api/storefront/orders/?page={page}&pageSize={pageSize}", ct);

    public Task<OrderDetailDto?> GetAsync(long id, CancellationToken ct) =>
        http.GetFromJsonAsync<OrderDetailDto>($"/api/storefront/orders/{id}", ct);
}

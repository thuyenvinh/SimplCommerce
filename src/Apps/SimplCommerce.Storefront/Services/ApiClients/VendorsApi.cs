using System.Net.Http.Json;

namespace SimplCommerce.Storefront.Services.ApiClients;

/// <summary>
/// Wave 19: storefront vendor client. The list/detail/products endpoints are
/// anonymous (Wave 10); apply + my-application are CustomerOnly (Wave 7), so
/// this client is registered with the bearer-forwarding handler.
/// </summary>
public interface IVendorsApi
{
    Task<IReadOnlyList<StorefrontVendorItem>?> ListAsync(CancellationToken ct = default);
    Task<StorefrontVendorDetail?> GetBySlugAsync(string slug, CancellationToken ct = default);
    Task<StorefrontVendorProductsPage?> GetProductsAsync(string slug, int page = 1, int pageSize = 24, CancellationToken ct = default);

    Task<HttpResponseMessage> ApplyAsync(VendorApplyRequest req, CancellationToken ct = default);
    // Returns null when the customer has never applied (204) or on error.
    Task<MyVendorApplication?> GetMyApplicationAsync(CancellationToken ct = default);
}

public sealed class VendorsApi(HttpClient http) : IVendorsApi
{
    public async Task<IReadOnlyList<StorefrontVendorItem>?> ListAsync(CancellationToken ct) =>
        await http.GetFromJsonAsync<List<StorefrontVendorItem>>("/api/storefront/vendors/", ct);

    public async Task<StorefrontVendorDetail?> GetBySlugAsync(string slug, CancellationToken ct)
    {
        using var resp = await http.GetAsync($"/api/storefront/vendors/{Uri.EscapeDataString(slug)}", ct);
        if (!resp.IsSuccessStatusCode) return null;
        return await resp.Content.ReadFromJsonAsync<StorefrontVendorDetail>(cancellationToken: ct);
    }

    public async Task<StorefrontVendorProductsPage?> GetProductsAsync(string slug, int page, int pageSize, CancellationToken ct)
    {
        using var resp = await http.GetAsync($"/api/storefront/vendors/{Uri.EscapeDataString(slug)}/products?page={page}&pageSize={pageSize}", ct);
        if (!resp.IsSuccessStatusCode) return null;
        return await resp.Content.ReadFromJsonAsync<StorefrontVendorProductsPage>(cancellationToken: ct);
    }

    public Task<HttpResponseMessage> ApplyAsync(VendorApplyRequest req, CancellationToken ct) =>
        http.PostAsJsonAsync("/api/storefront/vendors/apply", req, ct);

    public async Task<MyVendorApplication?> GetMyApplicationAsync(CancellationToken ct)
    {
        using var resp = await http.GetAsync("/api/storefront/vendors/my-application", ct);
        if (resp.StatusCode == System.Net.HttpStatusCode.NoContent) return null;
        if (!resp.IsSuccessStatusCode) return null;
        return await resp.Content.ReadFromJsonAsync<MyVendorApplication>(cancellationToken: ct);
    }
}
